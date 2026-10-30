using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.BL.Services;
using Khel_Akhel_Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using static Khel_Akhel_Server.Models.CommonResponse;

namespace Khel_Akhel_Server.Controllers.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private const string RefreshCookieName = "drs_refresh";
        private static readonly string DummyBcryptHash = BCrypt.Net.BCrypt.HashPassword(
            "KhelAkhelDummyTimingMitigationPassword2026!",
            12
        );

        private static ApiResponse GenericInvalidCredentialsResponse() =>
            new ApiResponse
            {
                Success = false,
                StatusCode = 401,
                Message = "INVALID_CREDENTIALS",
                Errors = new List<string> { "Invalid credentials." },
            };

        private readonly DbHelper _db;
        private readonly IConfiguration _configuration;
        private readonly IUrlEncryptionService _encryption;
        private readonly IRefreshTokenService _refreshTokens;
        private readonly IAuditService _audit;
        private readonly IWebHostEnvironment _environment;
        private readonly ICaptchaService _captchaService;

        public AuthController(
            DbHelper db,
            IConfiguration configuration,
            IUrlEncryptionService encryption,
            IRefreshTokenService refreshTokens,
            IAuditService audit,
            IWebHostEnvironment environment,
            ICaptchaService captchaService
        )
        {
            _db = db;
            _configuration = configuration;
            _encryption = encryption;
            _refreshTokens = refreshTokens;
            _audit = audit;
            _environment = environment;
            _captchaService = captchaService;
        }

        #region Helper Methods
        private long GetAuthenticatedUserId()
        {
            var claim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && long.TryParse(claim.Value, out long userId))
            {
                return userId;
            }
            return 0;
        }

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return Request.Headers["User-Agent"].ToString() ?? "Unknown";
        }

        private void SetRefreshTokenCookie(IssuedRefreshToken token)
        {
            bool isDev = _environment.IsDevelopment();
            bool isHttps = Request.IsHttps;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDev || isHttps,
                SameSite = isDev
                    ? (isHttps ? SameSiteMode.None : SameSiteMode.Lax)
                    : SameSiteMode.Strict,
                Expires = token.ExpiresUtc,
                IsEssential = true,
                Path = "/api/auth",
            };

            Response.Cookies.Append(RefreshCookieName, token.Value, cookieOptions);
        }

        private void DeleteRefreshTokenCookie()
        {
            bool isDev = _environment.IsDevelopment();
            bool isHttps = Request.IsHttps;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDev || isHttps,
                SameSite = isDev
                    ? (isHttps ? SameSiteMode.None : SameSiteMode.Lax)
                    : SameSiteMode.Strict,
                Path = "/api/auth",
            };

            Response.Cookies.Delete(RefreshCookieName, cookieOptions);
        }

        private async Task CheckAccountWideAlert(long customerId, string clientIp)
        {
            try
            {
                string countQuery =
                    @"
                    SELECT COUNT(1)
                    FROM drs_login_history_mst
                    WHERE customer_id = @CustomerId
                      AND IsSuccess = 0
                      AND CreatedDate >= DATEADD(MINUTE, -15, GETDATE())";

                object? failuresObj = await _db.ExecuteScalarAsync(
                    countQuery,
                    new[] { new SqlParameter("@CustomerId", customerId) }
                );
                int failuresCount =
                    failuresObj != null && failuresObj != DBNull.Value
                        ? Convert.ToInt32(failuresObj)
                        : 0;

                if (failuresCount >= 20)
                {
                    string recentAuditQuery =
                        @"
                        SELECT COUNT(1)
                        FROM drs_audit_mst
                        WHERE customer_id = @CustomerId
                          AND Action = 'SuspiciousLoginActivity'
                          AND CreatedDate >= DATEADD(MINUTE, -15, GETDATE())";

                    object? auditObj = await _db.ExecuteScalarAsync(
                        recentAuditQuery,
                        new[] { new SqlParameter("@CustomerId", customerId) }
                    );
                    int auditCount =
                        auditObj != null && auditObj != DBNull.Value
                            ? Convert.ToInt32(auditObj)
                            : 0;

                    if (auditCount == 0)
                    {
                        Logs.Warning(
                            $"Suspicious login activity: Customer ID {customerId} recorded {failuresCount} failed login attempts across IPs in the last 15 minutes."
                        );
                        _ = _audit.InsertAuditAsync(
                            customerId: customerId,
                            moduleName: "Auth",
                            action: "SuspiciousLoginActivity",
                            tableName: "drs_customer_mst",
                            entityId: customerId,
                            oldValue: null,
                            newValue: $"{failuresCount} failed login attempts in last 15 minutes",
                            ipAddress: clientIp,
                            userAgent: GetUserAgent()
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Logs.Warning($"Failed to check account-wide alert: {ex.Message}");
            }
        }
        #endregion

        #region Get Captcha API
        [AllowAnonymous]
        [EnableRateLimiting("auth-session")]
        [HttpGet("captcha")]
        [HttpGet("/api/admin/auth/captcha")]
        public IActionResult GetCaptcha()
        {
            try
            {
                var captchaData = _captchaService.GenerateCaptcha();
                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "CAPTCHA_GENERATED",
                        Data = captchaData,
                    }
                );
            }
            catch (Exception ex)
            {
                Logs.Error("Exception occurred in GetCaptcha API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string> { "An internal server error occurred." },
                    }
                );
            }
        }
        #endregion

        #region Portal Login API
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [HttpPost("login")]
        [HttpPost("/api/admin/auth/login")]
        public async Task<IActionResult> PortalLogin([FromBody] PortalLoginRequest? request)
        {
            Logs.Info("PortalLogin API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning("PortalLogin rejected | Null request body");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string> { "Request body is required." },
                        }
                    );
                }

                request.UserName = request.UserName?.Trim() ?? string.Empty;
                request.Password = request.Password ?? string.Empty;
                request.CaptchaId = request.CaptchaId?.Trim();
                request.Captcha = request.Captcha?.Trim();

                // 1. Validate request shape
                if (
                    string.IsNullOrWhiteSpace(request.UserName)
                    || string.IsNullOrWhiteSpace(request.Password)
                )
                {
                    sw.Stop();
                    Logs.Warning("PortalLogin rejected | Missing credentials");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = new List<string> { "Email/Mobile and Password are required." },
                        }
                    );
                }

                // 2. Validate CAPTCHA is strictly mandatory for all logins (Admin and Customer)
                bool captchaValid = _captchaService.ValidateCaptcha(
                    request.CaptchaId,
                    request.Captcha
                );
                if (!captchaValid)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"PortalLogin rejected | Invalid or expired CAPTCHA | IP:{GetClientIpAddress()}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_CAPTCHA",
                            Errors = new List<string> { "Invalid or expired CAPTCHA." },
                        }
                    );
                }

                string clientIp = GetClientIpAddress();

                // 3. Look up user WITHOUT NOLOCK
                string userLookupQuery =
                    @"
                    SELECT TOP 1 ID, FirstName, LastName, Email, MobileNo, PasswordHash, IsAdmin, IsActive, FailedLoginAttempts, AccountLockedUntil
                    FROM drs_customer_mst
                    WHERE (Email = @UserName OR MobileNo = @UserName) AND IsDeleted = 0";

                DataTable dt = await _db.ExecuteQueryAsync(
                    userLookupQuery,
                    new[] { new SqlParameter("@UserName", request.UserName) }
                );

                bool userExists = dt.Rows.Count > 0;
                DataRow? user = userExists ? dt.Rows[0] : null;
                long? customerId = userExists ? Convert.ToInt64(user!["ID"]) : null;

                // 4. Always run BCrypt verification (constant-time response mitigation)
                string hashToVerify = userExists
                    ? (user!["PasswordHash"]?.ToString() ?? DummyBcryptHash)
                    : DummyBcryptHash;

                bool passwordValid = PasswordHelper.VerifyPassword(
                    request.Password,
                    hashToVerify,
                    customerId
                );

                // 5. Source-scoped throttle: count failed attempts for this customer_id AND this IP in the last 15 min since pair's last success
                if (userExists && customerId.HasValue)
                {
                    string throttleQuery =
                        @"
                        SELECT COUNT(1)
                        FROM drs_login_history_mst
                        WHERE customer_id = @CustomerId
                          AND IPAddress = @IPAddress
                          AND IsSuccess = 0
                          AND CreatedDate >= DATEADD(MINUTE, -15, GETDATE())
                          AND CreatedDate > COALESCE(
                              (SELECT MAX(CreatedDate)
                               FROM drs_login_history_mst
                               WHERE customer_id = @CustomerId
                                 AND IPAddress = @IPAddress
                                 AND IsSuccess = 1),
                              '1900-01-01'
                          )";

                    object? countObj = await _db.ExecuteScalarAsync(
                        throttleQuery,
                        new[]
                        {
                            new SqlParameter("@CustomerId", customerId.Value),
                            new SqlParameter("@IPAddress", clientIp),
                        }
                    );

                    int recentFailuresFromThisIp =
                        countObj != null && countObj != DBNull.Value
                            ? Convert.ToInt32(countObj)
                            : 0;

                    if (recentFailuresFromThisIp >= 5)
                    {
                        // Throttled: record failure row and return generic 401 without revealing password state
                        await _db.ExecuteNonQueryAsync(
                            @"INSERT INTO drs_login_history_mst (customer_id, LoginType, DeviceInfo, IPAddress, IsSuccess, FailureReason, CreatedDate)
                              VALUES (@CustomerId, 'PASSWORD', @DeviceInfo, @IPAddress, 0, 'THROTTLED', GETDATE())",
                            new[]
                            {
                                new SqlParameter("@CustomerId", customerId.Value),
                                new SqlParameter(
                                    "@DeviceInfo",
                                    (object?)request.DeviceInfo ?? DBNull.Value
                                ),
                                new SqlParameter("@IPAddress", clientIp),
                            }
                        );

                        await _db.ExecuteNonQueryAsync(
                            @"UPDATE drs_customer_mst
                              SET FailedLoginAttempts = FailedLoginAttempts + 1,
                                  LastFailedLoginDate = GETDATE()
                              WHERE ID = @ID",
                            new[] { new SqlParameter("@ID", customerId.Value) }
                        );

                        await CheckAccountWideAlert(customerId.Value, clientIp);

                        sw.Stop();
                        Logs.Warning(
                            $"PortalLogin rejected | Source-scoped throttle active | CustomerId:{customerId.Value} | IP:{clientIp}"
                        );
                        return Unauthorized(GenericInvalidCredentialsResponse());
                    }
                }

                // 6. If password is wrong or user does not exist, return the identical 401 response
                if (!userExists || !passwordValid)
                {
                    if (userExists && customerId.HasValue)
                    {
                        await _db.ExecuteNonQueryAsync(
                            @"INSERT INTO drs_login_history_mst (customer_id, LoginType, DeviceInfo, IPAddress, IsSuccess, FailureReason, CreatedDate)
                              VALUES (@CustomerId, 'PASSWORD', @DeviceInfo, @IPAddress, 0, 'INVALID_PASSWORD', GETDATE())",
                            new[]
                            {
                                new SqlParameter("@CustomerId", customerId.Value),
                                new SqlParameter(
                                    "@DeviceInfo",
                                    (object?)request.DeviceInfo ?? DBNull.Value
                                ),
                                new SqlParameter("@IPAddress", clientIp),
                            }
                        );

                        await _db.ExecuteNonQueryAsync(
                            @"UPDATE drs_customer_mst
                              SET FailedLoginAttempts = FailedLoginAttempts + 1,
                                  LastFailedLoginDate = GETDATE()
                              WHERE ID = @ID",
                            new[] { new SqlParameter("@ID", customerId.Value) }
                        );

                        await CheckAccountWideAlert(customerId.Value, clientIp);
                    }

                    sw.Stop();
                    Logs.Warning($"PortalLogin failed | Invalid credentials | IP:{clientIp}");
                    return Unauthorized(GenericInvalidCredentialsResponse());
                }

                // 7. ONLY after correct password:
                // Check Active Status
                if (!Convert.ToBoolean(user!["IsActive"]))
                {
                    sw.Stop();
                    Logs.Warning(
                        $"PortalLogin rejected | Disabled account | CustomerId:{customerId!.Value}"
                    );
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 403,
                            Message = "ACCOUNT_DISABLED",
                            Errors = new List<string> { "Account is disabled." },
                        }
                    );
                }

                // Check Account Lock
                if (user["AccountLockedUntil"] != DBNull.Value)
                {
                    DateTime lockedUntil = Convert.ToDateTime(user["AccountLockedUntil"]);
                    if (lockedUntil > DateTime.Now)
                    {
                        sw.Stop();
                        Logs.Warning(
                            $"PortalLogin rejected | Account locked | CustomerId:{customerId!.Value}"
                        );
                        return StatusCode(
                            StatusCodes.Status423Locked,
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = StatusCodes.Status423Locked,
                                Message = "ACCOUNT_TEMPORARILY_LOCKED",
                                Errors = new List<string>
                                {
                                    "Account is temporarily locked due to failed login attempts.",
                                },
                            }
                        );
                    }
                }

                // 8. Authentication Success
                bool isAdmin = Convert.ToBoolean(user["IsAdmin"]);
                string role = isAdmin ? "Admin" : "Customer";
                string email = user["Email"]?.ToString() ?? "";

                string accessToken = JwtHelper.GenerateToken(
                    customerId!.Value,
                    email,
                    role,
                    _configuration
                );
                IssuedRefreshToken refreshToken = await _refreshTokens.IssueAsync(
                    customerId.Value,
                    clientIp,
                    request.DeviceInfo
                );
                SetRefreshTokenCookie(refreshToken);

                // Update Last Login & Reset Failed Login Attempts
                await _db.ExecuteNonQueryAsync(
                    @"
                    UPDATE drs_customer_mst
                    SET LastLogin = GETDATE(), FailedLoginAttempts = 0, AccountLockedUntil = NULL
                    WHERE ID = @ID",
                    new[] { new SqlParameter("@ID", customerId.Value) }
                );

                // Record Login History Success
                await _db.ExecuteNonQueryAsync(
                    @"
                    INSERT INTO drs_login_history_mst (customer_id, LoginType, DeviceInfo, IPAddress, IsSuccess, CreatedDate)
                    VALUES (@CustomerId, 'PASSWORD', @DeviceInfo, @IPAddress, 1, GETDATE())",
                    new[]
                    {
                        new SqlParameter("@CustomerId", customerId.Value),
                        new SqlParameter(
                            "@DeviceInfo",
                            (object?)request.DeviceInfo ?? DBNull.Value
                        ),
                        new SqlParameter("@IPAddress", clientIp),
                    }
                );

                // Audit Entry
                await _audit.InsertAuditAsync(
                    customerId,
                    "Auth",
                    "PortalLogin",
                    "drs_customer_mst",
                    customerId,
                    null,
                    System.Text.Json.JsonSerializer.Serialize(
                        new { CustomerId = customerId, Role = role }
                    ),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                int expiryMinutes = _configuration.GetValue("Jwt:ExpiryMinutes", 60);
                sw.Stop();
                Logs.Info($"PortalLogin successful | CustomerId:{customerId} Role:{role}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "LOGIN_SUCCESS",
                        Data = new AuthResponseData
                        {
                            Token = accessToken,
                            ExpiresIn = expiryMinutes * 60,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in PortalLogin API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string> { "An internal server error occurred." },
                    }
                );
            }
        }
        #endregion

        #region Refresh Token API
        [AllowAnonymous]
        [EnableRateLimiting("auth-session")]
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            Logs.Info("RefreshToken API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (
                    !Request.Cookies.TryGetValue(RefreshCookieName, out var rawToken)
                    || string.IsNullOrWhiteSpace(rawToken)
                )
                {
                    sw.Stop();
                    Logs.Warning("RefreshToken rejected | Refresh cookie missing");
                    return Unauthorized(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 401,
                            Message = "REFRESH_TOKEN_REQUIRED",
                            Errors = new List<string> { "Refresh token cookie is missing." },
                        }
                    );
                }

                var rotation = await _refreshTokens.RotateAsync(
                    rawToken,
                    GetClientIpAddress(),
                    GetUserAgent()
                );
                if (!rotation.Success || rotation.Token == null)
                {
                    if (rotation.FailureReason == "REFRESH_RACE")
                    {
                        sw.Stop();
                        Logs.Warning(
                            $"RefreshToken race detected | IP:{GetClientIpAddress()} | CustomerId:{rotation.CustomerId}"
                        );
                        return StatusCode(
                            StatusCodes.Status409Conflict,
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 409,
                                Message = "REFRESH_IN_PROGRESS",
                                Errors = new List<string>
                                {
                                    "Token refresh is currently in progress. Please retry.",
                                },
                            }
                        );
                    }

                    DeleteRefreshTokenCookie();
                    sw.Stop();
                    Logs.Warning(
                        $"RefreshToken failed | CustomerId:{rotation.CustomerId} Reason:{rotation.FailureReason}"
                    );
                    return Unauthorized(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 401,
                            Message = "INVALID_REFRESH_TOKEN",
                            Errors = new List<string> { "Invalid or expired refresh token." },
                        }
                    );
                }

                SetRefreshTokenCookie(rotation.Token);
                string newAccessToken = JwtHelper.GenerateToken(
                    rotation.CustomerId,
                    rotation.Email,
                    rotation.Role,
                    _configuration
                );

                int expiryMinutes = _configuration.GetValue("Jwt:ExpiryMinutes", 60);
                sw.Stop();
                Logs.Info($"RefreshToken completed | CustomerId:{rotation.CustomerId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "TOKEN_REFRESHED",
                        Data = new AuthResponseData
                        {
                            Token = newAccessToken,
                            ExpiresIn = expiryMinutes * 60,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in RefreshToken API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string> { "An internal server error occurred." },
                    }
                );
            }
        }
        #endregion

        #region Logout API
        [AllowAnonymous]
        [EnableRateLimiting("auth-session")]
        [HttpPost("logout")]
        public async Task<IActionResult> PortalLogOut()
        {
            Logs.Info("PortalLogOut API started");
            var sw = Stopwatch.StartNew();

            try
            {
                long customerId = GetAuthenticatedUserId();

                if (
                    Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken)
                    && !string.IsNullOrWhiteSpace(refreshToken)
                )
                {
                    await _refreshTokens.RevokeAsync(refreshToken, GetClientIpAddress());
                }

                if (customerId > 0)
                {
                    await _refreshTokens.RevokeAllAsync(customerId, GetClientIpAddress());
                }

                DeleteRefreshTokenCookie();

                if (customerId > 0)
                {
                    await _db.ExecuteNonQueryAsync(
                        @"
                        INSERT INTO drs_login_history_mst (customer_id, LoginType, DeviceInfo, IPAddress, IsSuccess, CreatedDate)
                        VALUES (@CustomerId, 'LOGOUT', @DeviceInfo, @IPAddress, 1, GETDATE())",
                        new[]
                        {
                            new SqlParameter("@CustomerId", customerId),
                            new SqlParameter("@DeviceInfo", GetUserAgent()),
                            new SqlParameter("@IPAddress", GetClientIpAddress()),
                        }
                    );

                    await _audit.InsertAuditAsync(
                        customerId,
                        "Auth",
                        "PortalLogOut",
                        "drs_customer_mst",
                        customerId,
                        null,
                        null,
                        GetClientIpAddress(),
                        GetUserAgent()
                    );
                }

                sw.Stop();
                Logs.Info($"PortalLogOut completed | CustomerId:{customerId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "LOGOUT_SUCCESS",
                        Data = null,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in PortalLogOut API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string> { "An internal server error occurred." },
                    }
                );
            }
        }
        #endregion

        #region Password Self-Service Helpers
        /// <summary>
        /// Shared input checks for Forgot Password and Reset (Change) Password.
        /// </summary>
        private static List<string> ValidatePasswordChangeInput(
            string email,
            string mobileNo,
            string newPassword,
            string confirmPassword
        )
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(email))
                errors.Add("Email address is required.");
            else if (!ValidationHelper.IsValidEmail(email))
                errors.Add("Please enter a valid email address.");

            if (string.IsNullOrWhiteSpace(mobileNo))
                errors.Add("Mobile number is required.");
            else if (!ValidationHelper.IsValidMobile(mobileNo))
                errors.Add("Please enter a valid 10-digit mobile number.");

            if (string.IsNullOrEmpty(newPassword))
                errors.Add("New password is required.");
            else if (!ValidationHelper.IsValidPassword(newPassword))
                errors.Add(
                    "New password must be at least 8 characters long and contain uppercase, lowercase, number, and special character."
                );

            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                errors.Add("New password and confirm password do not match.");

            return errors;
        }

        /// <summary>
        /// Finds an active account whose email AND mobile number both match the database record.
        /// </summary>
        private static ApiResponse ChangePasswordMismatchResponse()
        {
            return new ApiResponse
            {
                Success = false,
                StatusCode = 400,
                Message = "DETAILS_NOT_MATCHED",
                Errors = new List<string>
                {
                    "The email address, mobile number or existing password is incorrect.",
                },
            };
        }

        private async Task<DataRow?> FindAccountByEmailAndMobile(string email, string mobileNo)
        {
            DataTable dt = await _db.ExecuteQueryAsync(
                @"
                SELECT TOP 1 ID, PasswordHash, IsAdmin, AccountLockedUntil
                FROM drs_customer_mst
                WHERE Email = @Email
                  AND MobileNo = @MobileNo
                  AND IsDeleted = 0
                  AND IsActive = 1",
                new[]
                {
                    new SqlParameter("@Email", email),
                    new SqlParameter("@MobileNo", mobileNo),
                }
            );
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        /// <summary>
        /// Saves the new password hash, clears any login lock, signs out all sessions and writes the audit entry.
        /// </summary>
        private async Task SaveNewPasswordAsync(long customerId, string newPassword, string auditAction)
        {
            await _db.ExecuteNonQueryAsync(
                @"
                UPDATE drs_customer_mst
                SET PasswordHash = @PasswordHash,
                    FailedLoginAttempts = 0,
                    AccountLockedUntil = NULL,
                    ModifiedDate = GETDATE()
                WHERE ID = @ID AND IsDeleted = 0",
                new[]
                {
                    new SqlParameter("@PasswordHash", PasswordHelper.HashPassword(newPassword)),
                    new SqlParameter("@ID", customerId),
                }
            );

            await _refreshTokens.RevokeAllAsync(customerId, GetClientIpAddress());
            DeleteRefreshTokenCookie();

            await _audit.InsertAuditAsync(
                customerId: customerId,
                moduleName: "Auth",
                action: auditAction,
                tableName: "drs_customer_mst",
                entityId: customerId,
                oldValue: null,
                newValue: null,
                ipAddress: GetClientIpAddress(),
                userAgent: GetUserAgent()
            );
        }
        #endregion

        #region Forgot Password API
        /// <summary>
        /// Forgot Password: the customer proves who they are with their registered email + mobile number
        /// (both must match the database) and a CAPTCHA, then sets a new password.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequest? request)
        {
            Logs.Info("ForgetPassword API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning("ForgetPassword rejected: Request body is required");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string> { "Request body is required." },
                        }
                    );
                }

                request.Email = request.Email?.Trim() ?? string.Empty;
                request.MobileNo = request.MobileNo?.Trim() ?? string.Empty;
                request.CaptchaId = request.CaptchaId?.Trim();
                request.Captcha = request.Captcha?.Trim();

                var errors = ValidatePasswordChangeInput(
                    request.Email,
                    request.MobileNo,
                    request.NewPassword,
                    request.ConfirmPassword
                );
                if (errors.Any())
                {
                    sw.Stop();
                    Logs.Warning($"ForgetPassword rejected: Validation failed ({string.Join("; ", errors)})");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = errors,
                        }
                    );
                }

                // Captcha is strictly required
                if (!_captchaService.ValidateCaptcha(request.CaptchaId, request.Captcha))
                {
                    sw.Stop();
                    Logs.Warning("ForgetPassword rejected | Invalid or expired CAPTCHA");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_CAPTCHA",
                            Errors = new List<string> { "Invalid or expired CAPTCHA." },
                        }
                    );
                }

                // Email AND mobile must both match the same customer (admin accounts are excluded)
                DataRow? account = await FindAccountByEmailAndMobile(request.Email, request.MobileNo);
                if (account == null || Convert.ToBoolean(account["IsAdmin"]))
                {
                    sw.Stop();
                    Logs.Warning("ForgetPassword rejected | Email and mobile number do not match any customer");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "DETAILS_NOT_MATCHED",
                            Errors = new List<string>
                            {
                                "The email address and mobile number do not match our records.",
                            },
                        }
                    );
                }

                long customerId = Convert.ToInt64(account["ID"]);
                await SaveNewPasswordAsync(customerId, request.NewPassword, "PasswordResetSelfService");

                sw.Stop();
                Logs.Info($"ForgetPassword completed | CustomerId:{customerId} | Elapsed:{sw.ElapsedMilliseconds}ms");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PASSWORD_RESET_SUCCESS",
                        Data = "Your password has been reset. Please sign in with your new password.",
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ForgetPassword API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string> { "An internal server error occurred." },
                    }
                );
            }
        }
        #endregion

        #region Validate Reset Password Token API
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [HttpGet("reset-password/validate")]
        public async Task<IActionResult> ValidateResetToken([FromQuery] string? token)
        {
            Logs.Info("ValidateResetToken API started");
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    Logs.Warning("ValidateResetToken rejected: Token query parameter is missing or empty");
                    return Ok(
                        new ApiResponse
                        {
                            Success = true,
                            StatusCode = 200,
                            Message = "TOKEN_VALIDATED",
                            Data = new { valid = false },
                        }
                    );
                }

                string tokenHash = Convert.ToBase64String(
                    SHA256.HashData(Encoding.UTF8.GetBytes(token))
                );

                string query =
                    @"
                    SELECT COUNT(1)
                    FROM drs_password_reset_request_mst r
                    INNER JOIN drs_customer_mst c ON c.ID = r.customer_id AND c.IsDeleted = 0 AND c.IsActive = 1 AND c.IsAdmin = 0
                    WHERE r.TokenHash = @TokenHash
                      AND r.Status = 'LINK_ISSUED'
                      AND r.UsedUtc IS NULL
                      AND r.TokenExpiresUtc > GETUTCDATE()
                      AND r.IsDeleted = 0";

                object? countObj = await _db.ExecuteScalarAsync(
                    query,
                    new[] { new SqlParameter("@TokenHash", tokenHash) }
                );
                bool isValid =
                    countObj != null && countObj != DBNull.Value && Convert.ToInt32(countObj) > 0;

                Logs.Info($"ValidateResetToken completed successfully | IsValid: {isValid}");
                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "TOKEN_VALIDATED",
                        Data = new { valid = isValid },
                    }
                );
            }
            catch (Exception ex)
            {
                Logs.Error("Exception occurred in ValidateResetToken API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string> { "An internal server error occurred." },
                    }
                );
            }
        }
        #endregion

        #region Reset Password API
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest? request)
        {
            Logs.Info("ResetPassword API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning("ResetPassword rejected: Request body is required");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string> { "Request body is required." },
                        }
                    );
                }

                if (string.IsNullOrWhiteSpace(request.Token))
                {
                    sw.Stop();
                    Logs.Warning("ResetPassword rejected: Reset token is required");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_OR_EXPIRED_RESET_TOKEN",
                            Errors = new List<string> { "Reset token is required." },
                        }
                    );
                }

                if (string.IsNullOrEmpty(request.NewPassword))
                {
                    sw.Stop();
                    Logs.Warning("ResetPassword rejected: New password is required");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = new List<string> { "New password is required." },
                        }
                    );
                }

                if (!ValidationHelper.IsValidPassword(request.NewPassword))
                {
                    sw.Stop();
                    Logs.Warning("ResetPassword rejected: Password complexity requirements not met");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = new List<string>
                            {
                                "New password must be at least 8 characters long and contain uppercase, lowercase, number, and special character.",
                            },
                        }
                    );
                }

                if (
                    !string.Equals(
                        request.NewPassword,
                        request.ConfirmPassword,
                        StringComparison.Ordinal
                    )
                )
                {
                    sw.Stop();
                    Logs.Warning("ResetPassword rejected: Passwords do not match");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = new List<string>
                            {
                                "New password and confirm password do not match.",
                            },
                        }
                    );
                }

                string clientIp = GetClientIpAddress();
                string tokenHash = Convert.ToBase64String(
                    SHA256.HashData(Encoding.UTF8.GetBytes(request.Token))
                );

                using var con = await _db.GetOpenConnectionAsync();
                using var tx = con.BeginTransaction(IsolationLevel.ReadCommitted);

                try
                {
                    // Atomic token consumption
                    string consumeQuery =
                        @"
                        UPDATE drs_password_reset_request_mst
                        SET UsedUtc = GETUTCDATE(),
                            Status = 'COMPLETED',
                            ModifiedDate = GETDATE()
                        OUTPUT inserted.customer_id
                        WHERE TokenHash = @TokenHash
                          AND Status = 'LINK_ISSUED'
                          AND UsedUtc IS NULL
                          AND TokenExpiresUtc > GETUTCDATE()
                          AND IsDeleted = 0;";

                    using var consumeCmd = new SqlCommand(consumeQuery, con, tx);
                    consumeCmd.Parameters.AddWithValue("@TokenHash", tokenHash);
                    object? claimResult = await consumeCmd.ExecuteScalarAsync();

                    if (claimResult == null || claimResult == DBNull.Value)
                    {
                        await tx.RollbackAsync();
                        sw.Stop();
                        Logs.Warning(
                            "ResetPassword failed | Invalid, expired or already used reset token"
                        );
                        return BadRequest(
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 400,
                                Message = "INVALID_OR_EXPIRED_RESET_TOKEN",
                                Errors = new List<string>
                                {
                                    "The password reset link has expired, is invalid, or was already used.",
                                },
                            }
                        );
                    }

                    long customerId = Convert.ToInt64(claimResult);

                    // Verify customer state: not deleted, active, and not an admin
                    string checkCustQuery =
                        @"
                        SELECT ID, IsAdmin, IsActive, IsDeleted
                        FROM drs_customer_mst
                        WHERE ID = @CustomerId";

                    using var checkCustCmd = new SqlCommand(checkCustQuery, con, tx);
                    checkCustCmd.Parameters.AddWithValue("@CustomerId", customerId);

                    using var reader = await checkCustCmd.ExecuteReaderAsync();
                    if (!reader.Read())
                    {
                        await reader.CloseAsync();
                        await tx.RollbackAsync();
                        return BadRequest(
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 400,
                                Message = "INVALID_OR_EXPIRED_RESET_TOKEN",
                                Errors = new List<string> { "Account not found." },
                            }
                        );
                    }

                    bool isAdmin = Convert.ToBoolean(reader["IsAdmin"]);
                    bool isActive = Convert.ToBoolean(reader["IsActive"]);
                    bool isDeleted = Convert.ToBoolean(reader["IsDeleted"]);
                    await reader.CloseAsync();

                    if (isAdmin || !isActive || isDeleted)
                    {
                        await tx.RollbackAsync();
                        sw.Stop();
                        Logs.Warning(
                            $"ResetPassword rejected | Ineligible account | CustomerId:{customerId} Admin:{isAdmin} Active:{isActive}"
                        );
                        return BadRequest(
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 400,
                                Message = "INVALID_OR_EXPIRED_RESET_TOKEN",
                                Errors = new List<string>
                                {
                                    "Password reset is not permitted for this account.",
                                },
                            }
                        );
                    }

                    // Update PasswordHash (cost 12), reset failed login attempts, clear lock
                    string newHash = PasswordHelper.HashPassword(request.NewPassword);
                    string updatePassQuery =
                        @"
                        UPDATE drs_customer_mst
                        SET PasswordHash = @PasswordHash,
                            FailedLoginAttempts = 0,
                            AccountLockedUntil = NULL,
                            ModifiedDate = GETDATE()
                        WHERE ID = @CustomerId";

                    using var updatePassCmd = new SqlCommand(updatePassQuery, con, tx);
                    updatePassCmd.Parameters.AddWithValue("@PasswordHash", newHash);
                    updatePassCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    await updatePassCmd.ExecuteNonQueryAsync();

                    // Revoke all refresh tokens
                    string revokeTokensQuery =
                        @"
                        UPDATE drs_refresh_token_mst
                        SET RevokedUtc = GETUTCDATE(),
                            RevokedByIp = @Ip,
                            IsActive = 0,
                            ModifiedDate = GETDATE()
                        WHERE customer_id = @CustomerId AND RevokedUtc IS NULL";

                    using var revokeCmd = new SqlCommand(revokeTokensQuery, con, tx);
                    revokeCmd.Parameters.AddWithValue("@Ip", clientIp);
                    revokeCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    await revokeCmd.ExecuteNonQueryAsync();

                    await tx.CommitAsync();

                    // Audit entry
                    await _audit.InsertAuditAsync(
                        customerId: customerId,
                        moduleName: "Auth",
                        action: "PasswordReset",
                        tableName: "drs_customer_mst",
                        entityId: customerId,
                        oldValue: null,
                        newValue: "Password reset completed via token",
                        ipAddress: clientIp,
                        userAgent: GetUserAgent()
                    );

                    sw.Stop();
                    Logs.Info($"ResetPassword completed successfully | CustomerId:{customerId}");

                    return Ok(
                        new ApiResponse
                        {
                            Success = true,
                            StatusCode = 200,
                            Message = "PASSWORD_RESET_SUCCESS",
                            Data =
                                "Your password has been reset successfully. Please log in with your new password.",
                        }
                    );
                }
                catch
                {
                    try
                    {
                        await tx.RollbackAsync();
                    }
                    catch { }
                    throw;
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ResetPassword API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string> { "An internal server error occurred." },
                    }
                );
            }
        }
        #endregion

        #region Change Password API
        /// <summary>
        /// Reset Password (change an existing password): email + mobile number must match the database
        /// record and the existing password must be correct. Works signed in or signed out.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [HttpPatch("change-password")]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest? request)
        {
            Logs.Info("ChangePassword API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning("ChangePassword rejected: Request body cannot be null");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "REQUEST_BODY_REQUIRED",
                            Errors = new List<string> { "Request body cannot be null." },
                        }
                    );
                }

                request.Email = request.Email?.Trim() ?? string.Empty;
                request.MobileNo = request.MobileNo?.Trim() ?? string.Empty;

                var errors = ValidatePasswordChangeInput(
                    request.Email,
                    request.MobileNo,
                    request.NewPassword,
                    request.ConfirmPassword
                );
                if (string.IsNullOrEmpty(request.CurrentPassword))
                    errors.Insert(0, "Existing password is required.");
                else if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
                    errors.Add("New password must be different from your existing password.");

                if (errors.Any())
                {
                    sw.Stop();
                    Logs.Warning($"ChangePassword rejected: Validation failed ({string.Join("; ", errors)})");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = errors,
                        }
                    );
                }

                string clientIp = GetClientIpAddress();
                DataRow? account = await FindAccountByEmailAndMobile(request.Email, request.MobileNo);
                if (account == null)
                {
                    sw.Stop();
                    Logs.Warning($"ChangePassword rejected | Details not matched | IP:{clientIp}");
                    return BadRequest(ChangePasswordMismatchResponse());
                }

                long customerId = Convert.ToInt64(account["ID"]);

                if (
                    account["AccountLockedUntil"] != DBNull.Value
                    && Convert.ToDateTime(account["AccountLockedUntil"]) > DateTime.Now
                )
                {
                    sw.Stop();
                    Logs.Warning($"ChangePassword rejected | Account locked | CustomerId:{customerId}");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "ACCOUNT_LOCKED",
                            Errors = new List<string>
                            {
                                "This account is temporarily locked. Please try again later or use Forgot Password.",
                            },
                        }
                    );
                }

                object? recentFailuresObj = await _db.ExecuteScalarAsync(
                    @"
                    SELECT COUNT(1)
                    FROM drs_login_history_mst
                    WHERE customer_id = @CustomerId
                      AND IPAddress = @IPAddress
                      AND LoginType = 'CHANGE_PASSWORD'
                      AND IsSuccess = 0
                      AND CreatedDate >= DATEADD(MINUTE, -15, GETDATE())",
                    new[]
                    {
                        new SqlParameter("@CustomerId", customerId),
                        new SqlParameter("@IPAddress", clientIp),
                    }
                );
                int recentFailures =
                    recentFailuresObj != null && recentFailuresObj != DBNull.Value
                        ? Convert.ToInt32(recentFailuresObj)
                        : 0;

                string currentHash = account["PasswordHash"]?.ToString() ?? "";
                bool passwordValid =
                    recentFailures < 5
                    && PasswordHelper.VerifyPassword(request.CurrentPassword, currentHash);

                if (!passwordValid)
                {
                    await _db.ExecuteNonQueryAsync(
                        @"INSERT INTO drs_login_history_mst (customer_id, LoginType, DeviceInfo, IPAddress, IsSuccess, FailureReason, CreatedDate)
                          VALUES (@CustomerId, 'CHANGE_PASSWORD', NULL, @IPAddress, 0, @FailureReason, GETDATE())",
                        new[]
                        {
                            new SqlParameter("@CustomerId", customerId),
                            new SqlParameter("@IPAddress", clientIp),
                            new SqlParameter(
                                "@FailureReason",
                                recentFailures >= 5 ? "THROTTLED" : "INVALID_PASSWORD"
                            ),
                        }
                    );

                    await _db.ExecuteNonQueryAsync(
                        @"UPDATE drs_customer_mst
                          SET FailedLoginAttempts = FailedLoginAttempts + 1,
                              LastFailedLoginDate = GETDATE()
                          WHERE ID = @ID",
                        new[] { new SqlParameter("@ID", customerId) }
                    );

                    await CheckAccountWideAlert(customerId, clientIp);

                    sw.Stop();
                    Logs.Warning(
                        $"ChangePassword failed | {(recentFailures >= 5 ? "Throttled" : "Existing password invalid")} | CustomerId:{customerId} | IP:{clientIp}"
                    );
                    return BadRequest(ChangePasswordMismatchResponse());
                }

                await SaveNewPasswordAsync(customerId, request.NewPassword, "ChangePassword");

                sw.Stop();
                Logs.Info($"ChangePassword completed | CustomerId:{customerId} | Elapsed:{sw.ElapsedMilliseconds}ms");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PASSWORD_CHANGED",
                        Data = "Your password has been changed. Please sign in with your new password.",
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ChangePassword API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string> { "An internal server error occurred." },
                    }
                );
            }
        }
        #endregion
    }
}
