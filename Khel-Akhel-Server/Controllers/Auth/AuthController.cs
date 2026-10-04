using System.Data;
using System.Diagnostics;
using System.Security.Claims;
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
    [EnableRateLimiting("authentication")]
    public class AuthController : ControllerBase
    {
        private const string RefreshCookieName = "drs_refresh";

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
        #endregion

        #region CAPTCHA
        [AllowAnonymous]
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

        #region 7.1 PortalLogin
        [AllowAnonymous]
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
                request.Password = request.Password?.Trim() ?? string.Empty;
                request.CaptchaId = request.CaptchaId?.Trim();
                request.Captcha = request.Captcha?.Trim();

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

                // If CAPTCHA token/code is supplied in the request, validate CAPTCHA first before user lookup
                bool captchaSupplied =
                    !string.IsNullOrWhiteSpace(request.CaptchaId)
                    || !string.IsNullOrWhiteSpace(request.Captcha);
                if (captchaSupplied)
                {
                    bool captchaValid = _captchaService.ValidateCaptcha(
                        request.CaptchaId,
                        request.Captcha
                    );
                    if (!captchaValid)
                    {
                        sw.Stop();
                        Logs.Warning(
                            $"PortalLogin rejected | Invalid or expired CAPTCHA | Identifier:{request.UserName}"
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
                }

                // Query customer by Email or MobileNo
                string query =
                    @"
                    SELECT TOP 1 ID, FirstName, LastName, Email, MobileNo, PasswordHash, IsAdmin, IsActive, FailedLoginAttempts, AccountLockedUntil
                    FROM drs_customer_mst WITH (NOLOCK)
                    WHERE (LOWER(Email) = LOWER(@UserName) OR MobileNo = @UserName) AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(
                    query,
                    new[] { new SqlParameter("@UserName", request.UserName) }
                );

                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"PortalLogin failed | User not found | Identifier:{request.UserName}"
                    );
                    return Unauthorized(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 401,
                            Message = "INVALID_CREDENTIALS",
                            Errors = new List<string> { "Invalid credentials." },
                        }
                    );
                }

                DataRow user = dt.Rows[0];
                long customerId = Convert.ToInt64(user["ID"]);
                bool isAdmin = Convert.ToBoolean(user["IsAdmin"]);

                // If user is Admin, CAPTCHA is strictly mandatory
                if (isAdmin && !captchaSupplied)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"PortalLogin rejected | CAPTCHA required for Admin account | Identifier:{request.UserName}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_CAPTCHA",
                            Errors = new List<string> { "CAPTCHA is required for Admin login." },
                        }
                    );
                }

                // Check Account Lock
                if (
                    user["AccountLockedUntil"] != DBNull.Value
                    && Convert.ToDateTime(user["AccountLockedUntil"]) > DateTime.UtcNow
                )
                {
                    sw.Stop();
                    Logs.Warning(
                        $"PortalLogin rejected | Account locked | CustomerId:{customerId}"
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

                // Check Account Active
                if (!Convert.ToBoolean(user["IsActive"]))
                {
                    sw.Stop();
                    Logs.Warning(
                        $"PortalLogin rejected | Disabled account | CustomerId:{customerId}"
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

                // Verify Password
                string storedHash = user["PasswordHash"]?.ToString() ?? "";
                bool validPassword = PasswordHelper.VerifyPassword(request.Password, storedHash);

                if (!validPassword)
                {
                    // Increment failed login counter and lock if threshold reached
                    string lockUpdate =
                        @"
                        UPDATE drs_customer_mst
                        SET FailedLoginAttempts = FailedLoginAttempts + 1,
                            LastFailedLoginDate = GETDATE(),
                            AccountLockedUntil = CASE WHEN FailedLoginAttempts + 1 >= 5 THEN DATEADD(MINUTE, 15, GETUTCDATE()) ELSE AccountLockedUntil END
                        WHERE ID = @ID";

                    _db.ExecuteNonQuery(lockUpdate, new[] { new SqlParameter("@ID", customerId) });

                    // Login History Entry for Failure
                    _db.ExecuteNonQuery(
                        @"
                        INSERT INTO drs_login_history_mst (customer_id, LoginType, DeviceInfo, IPAddress, IsSuccess, FailureReason, CreatedDate)
                        VALUES (@CustomerId, 'PASSWORD', @DeviceInfo, @IPAddress, 0, 'INVALID_PASSWORD', GETDATE())",
                        new[]
                        {
                            new SqlParameter("@CustomerId", customerId),
                            new SqlParameter(
                                "@DeviceInfo",
                                (object?)request.DeviceInfo ?? DBNull.Value
                            ),
                            new SqlParameter("@IPAddress", GetClientIpAddress()),
                        }
                    );

                    sw.Stop();
                    Logs.Warning(
                        $"PortalLogin failed | Invalid password | CustomerId:{customerId}"
                    );

                    return Unauthorized(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 401,
                            Message = "INVALID_CREDENTIALS",
                            Errors = new List<string> { "Invalid credentials." },
                        }
                    );
                }

                // Authentication Success
                string role = isAdmin ? "Admin" : "Customer";
                string email = user["Email"]?.ToString() ?? "";

                string accessToken = JwtHelper.GenerateToken(
                    customerId,
                    email,
                    role,
                    _configuration
                );
                IssuedRefreshToken refreshToken = _refreshTokens.Issue(
                    customerId,
                    GetClientIpAddress(),
                    request.DeviceInfo
                );
                SetRefreshTokenCookie(refreshToken);

                // Update Last Login & Reset Failed Login Attempts
                _db.ExecuteNonQuery(
                    @"
                    UPDATE drs_customer_mst
                    SET LastLogin = GETDATE(), FailedLoginAttempts = 0, AccountLockedUntil = NULL
                    WHERE ID = @ID",
                    new[] { new SqlParameter("@ID", customerId) }
                );

                // Record Login History
                _db.ExecuteNonQuery(
                    @"
                    INSERT INTO drs_login_history_mst (customer_id, LoginType, DeviceInfo, IPAddress, IsSuccess, CreatedDate)
                    VALUES (@CustomerId, 'PASSWORD', @DeviceInfo, @IPAddress, 1, GETDATE())",
                    new[]
                    {
                        new SqlParameter("@CustomerId", customerId),
                        new SqlParameter(
                            "@DeviceInfo",
                            (object?)request.DeviceInfo ?? DBNull.Value
                        ),
                        new SqlParameter("@IPAddress", GetClientIpAddress()),
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

        #region 7.2 RefreshToken
        [AllowAnonymous]
        [HttpPost("refresh")]
        public IActionResult RefreshToken()
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

                var rotation = _refreshTokens.Rotate(
                    rawToken,
                    GetClientIpAddress(),
                    GetUserAgent()
                );
                if (!rotation.Success || rotation.Token == null)
                {
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

        #region 7.3 PortalLogOut
        [AllowAnonymous]
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
                    _refreshTokens.Revoke(refreshToken, GetClientIpAddress());
                }

                if (customerId > 0)
                {
                    _refreshTokens.RevokeAll(customerId, GetClientIpAddress());
                }

                DeleteRefreshTokenCookie();

                if (customerId > 0)
                {
                    _db.ExecuteNonQuery(
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

        #region 7.4 ForgetPassword
        [AllowAnonymous]
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
                if (string.IsNullOrWhiteSpace(request.UserName))
                {
                    sw.Stop();
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = new List<string> { "Email or mobile number is required." },
                        }
                    );
                }

                string checkQuery =
                    @"
                    SELECT TOP 1 ID, Email, MobileNo
                    FROM drs_customer_mst WITH (NOLOCK)
                    WHERE (Email = @UserName OR MobileNo = @UserName) AND IsDeleted = 0 AND IsActive = 1";

                DataTable dt = _db.ExecuteQuery(
                    checkQuery,
                    new[] { new SqlParameter("@UserName", request.UserName) }
                );

                if (dt.Rows.Count > 0)
                {
                    long customerId = Convert.ToInt64(dt.Rows[0]["ID"]);
                    await _audit.InsertAuditAsync(
                        customerId,
                        "Auth",
                        "ForgetPasswordRequest",
                        "drs_customer_mst",
                        customerId,
                        null,
                        null,
                        GetClientIpAddress(),
                        GetUserAgent()
                    );
                }

                sw.Stop();
                Logs.Info($"ForgetPassword processed for identifier");

                // Always return standard success response to avoid account enumeration vulnerabilities
                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PASSWORD_RESET_REQUESTED",
                        Data = null,
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

        #region 7.5 ChangePassword
        [Authorize]
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

                long customerId = GetAuthenticatedUserId();
                if (customerId <= 0)
                {
                    sw.Stop();
                    return Unauthorized(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 401,
                            Message = "UNAUTHORIZED",
                            Errors = new List<string> { "Authentication required." },
                        }
                    );
                }

                var errors = new List<string>();
                if (string.IsNullOrEmpty(request.CurrentPassword))
                    errors.Add("Current password is required.");
                if (string.IsNullOrEmpty(request.NewPassword))
                    errors.Add("New password is required.");
                else if (!ValidationHelper.IsValidPassword(request.NewPassword))
                    errors.Add(
                        "New password must be at least 8 characters long and contain uppercase, lowercase, number, and special character."
                    );

                if (
                    !string.Equals(
                        request.NewPassword,
                        request.ConfirmPassword,
                        StringComparison.Ordinal
                    )
                )
                    errors.Add("New password and confirm password do not match.");

                if (errors.Any())
                {
                    sw.Stop();
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

                // Fetch customer record
                string fetchQuery =
                    @"SELECT TOP 1 PasswordHash FROM drs_customer_mst WITH (NOLOCK) WHERE ID = @ID AND IsDeleted = 0";
                DataTable dt = _db.ExecuteQuery(
                    fetchQuery,
                    new[] { new SqlParameter("@ID", customerId) }
                );

                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "CUSTOMER_NOT_FOUND",
                            Errors = new List<string> { "Customer record not found." },
                        }
                    );
                }

                string currentHash = dt.Rows[0]["PasswordHash"]?.ToString() ?? "";
                if (!PasswordHelper.VerifyPassword(request.CurrentPassword, currentHash))
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ChangePassword failed | Current password invalid | CustomerId:{customerId}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_CREDENTIALS",
                            Errors = new List<string> { "Current password is incorrect." },
                        }
                    );
                }

                string newHash = PasswordHelper.HashPassword(request.NewPassword);

                // Update Password Hash & Revoke Active Refresh Tokens
                _db.ExecuteNonQuery(
                    @"
                    UPDATE drs_customer_mst
                    SET PasswordHash = @PasswordHash, ModifiedDate = GETDATE()
                    WHERE ID = @ID AND IsDeleted = 0",
                    new[]
                    {
                        new SqlParameter("@PasswordHash", newHash),
                        new SqlParameter("@ID", customerId),
                    }
                );

                _refreshTokens.RevokeAll(customerId, GetClientIpAddress());
                DeleteRefreshTokenCookie();

                await _audit.InsertAuditAsync(
                    customerId,
                    "Auth",
                    "ChangePassword",
                    "drs_customer_mst",
                    customerId,
                    null,
                    null,
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"ChangePassword completed | CustomerId:{customerId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PASSWORD_CHANGED",
                        Data = null,
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
