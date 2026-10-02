using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.BL.Services;
using Khel_Akhel_Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using static Khel_Akhel_Server.Models.CommonResponse;

namespace Khel_Akhel_Server.Controllers.Customer
{
    [ApiController]
    [Route("api/customer")]
    public class CustomerController : ControllerBase
    {
        private readonly DbHelper _db;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService _audit;

        public CustomerController(
            DbHelper db,
            IUrlEncryptionService encryption,
            IAuditService audit
        )
        {
            _db = db;
            _encryption = encryption;
            _audit = audit;
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

        private bool IsCurrentUserAdmin()
        {
            return User.IsInRole("Admin");
        }

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return Request.Headers["User-Agent"].ToString() ?? "Unknown";
        }
        #endregion

        #region 1. CustomerCreate
        [AllowAnonymous]
        [HttpPost("create")]
        public async Task<IActionResult> CustomerCreate([FromBody] CustomerCreateRequest? request)
        {
            Logs.Info("CustomerCreate API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning("CustomerCreate request rejected | Reason: Null request body");
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

                // Trim inputs (Do NOT trim password!)
                request.FirstName = request.FirstName?.Trim() ?? string.Empty;
                request.LastName = request.LastName?.Trim() ?? string.Empty;
                request.Email = request.Email?.Trim() ?? string.Empty;
                request.MobileNo = request.MobileNo?.Trim() ?? string.Empty;

                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.FirstName))
                    errors.Add("First name is required.");
                else if (!ValidationHelper.IsValidName(request.FirstName))
                    errors.Add("First name must be 2-50 alphabetic characters.");

                if (string.IsNullOrWhiteSpace(request.LastName))
                    errors.Add("Last name is required.");
                else if (!ValidationHelper.IsValidName(request.LastName))
                    errors.Add("Last name must be 2-50 alphabetic characters.");

                if (string.IsNullOrWhiteSpace(request.Email))
                    errors.Add("Email is required.");
                else if (!ValidationHelper.IsValidEmail(request.Email))
                    errors.Add("Invalid email format.");

                if (string.IsNullOrWhiteSpace(request.MobileNo))
                    errors.Add("Mobile number is required.");
                else if (!ValidationHelper.IsValidMobile(request.MobileNo))
                    errors.Add("Invalid mobile number format.");

                if (string.IsNullOrEmpty(request.Password))
                    errors.Add("Password is required.");
                else if (!ValidationHelper.IsValidPassword(request.Password))
                    errors.Add(
                        "Password must be at least 8 characters long and contain uppercase, lowercase, number, and special character."
                    );

                if (errors.Any())
                {
                    sw.Stop();
                    Logs.Warning(
                        $"CustomerCreate validation failed | Errors:{string.Join(", ", errors)}"
                    );
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

                // Check Duplicate Email (Case-insensitive)
                string checkEmailQuery =
                    @"
                    SELECT COUNT(1)
                    FROM drs_customer_mst WITH (NOLOCK)
                    WHERE LOWER(Email) = LOWER(@Email) AND IsDeleted = 0";

                int emailExists = Convert.ToInt32(
                    _db.ExecuteScalar(
                        checkEmailQuery,
                        new[] { new SqlParameter("@Email", request.Email) }
                    ) ?? 0
                );

                if (emailExists > 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"CustomerCreate duplicate email detected | Email:{request.Email}"
                    );
                    return Conflict(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 409,
                            Message = "CUSTOMER_ALREADY_EXISTS",
                            Errors = new List<string>
                            {
                                "Customer with given email already exists.",
                            },
                        }
                    );
                }

                // Check Duplicate Mobile
                string checkMobileQuery =
                    @"
                    SELECT COUNT(1)
                    FROM drs_customer_mst WITH (NOLOCK)
                    WHERE MobileNo = @MobileNo AND IsDeleted = 0";

                int mobileExists = Convert.ToInt32(
                    _db.ExecuteScalar(
                        checkMobileQuery,
                        new[] { new SqlParameter("@MobileNo", request.MobileNo) }
                    ) ?? 0
                );

                if (mobileExists > 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"CustomerCreate duplicate mobile detected | Mobile:{request.MobileNo}"
                    );
                    return Conflict(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 409,
                            Message = "CUSTOMER_ALREADY_EXISTS",
                            Errors = new List<string>
                            {
                                "Customer with given mobile number already exists.",
                            },
                        }
                    );
                }

                // Hash Password & Insert with Auto-generated Account_no
                string passwordHash = PasswordHelper.HashPassword(request.Password);

                using SqlConnection con = _db.GetOpenConnection();
                using SqlTransaction tx = con.BeginTransaction();

                string accountNo;
                long customerId;

                try
                {
                    string genAccountNoQuery =
                        "SELECT 'DRC' + RIGHT('000000' + CAST(NEXT VALUE FOR dbo.Seq_CustomerAccount AS VARCHAR(6)), 6);";
                    using SqlCommand genCmd = new(genAccountNoQuery, con, tx);
                    accountNo = Convert.ToString(genCmd.ExecuteScalar()) ?? string.Empty;

                    if (!System.Text.RegularExpressions.Regex.IsMatch(accountNo, @"^DRC[0-9]{6}$"))
                    {
                        tx.Rollback();
                        throw new InvalidOperationException(
                            $"Invalid generated Account_no format: {accountNo}"
                        );
                    }

                    string insertQuery =
                        @"
                        INSERT INTO drs_customer_mst
                        (Account_no, FirstName, LastName, Email, MobileNo, PasswordHash, IsAdmin, IsEmailVerified, IsMobileVerified, IsActive, IsDeleted, CreatedDate)
                        OUTPUT INSERTED.ID
                        VALUES
                        (@AccountNo, @FirstName, @LastName, @Email, @MobileNo, @PasswordHash, 0, 0, 0, 1, 0, GETDATE())";

                    using SqlCommand insertCmd = new(insertQuery, con, tx);
                    insertCmd.Parameters.AddWithValue("@AccountNo", accountNo);
                    insertCmd.Parameters.AddWithValue("@FirstName", request.FirstName);
                    insertCmd.Parameters.AddWithValue("@LastName", request.LastName);
                    insertCmd.Parameters.AddWithValue("@Email", request.Email);
                    insertCmd.Parameters.AddWithValue("@MobileNo", request.MobileNo);
                    insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                    customerId = Convert.ToInt64(insertCmd.ExecuteScalar());

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }

                string encryptedId = _encryption.Encrypt(customerId);

                var responseData = new CustomerResponse
                {
                    EncryptedId = encryptedId,
                    AccountNo = accountNo,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    MobileNo = request.MobileNo,
                    IsAdmin = false,
                    IsEmailVerified = false,
                    IsMobileVerified = false,
                    IsActive = true,
                    CreatedDate = DateTimeFormat.Format(DateTime.Now),
                };

                // Audit Entry
                string newValuesJson = System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        CustomerId = customerId,
                        request.FirstName,
                        request.LastName,
                        request.Email,
                        request.MobileNo,
                    }
                );

                await _audit.InsertAuditAsync(
                    customerId,
                    "Customer",
                    "CustomerCreate",
                    "drs_customer_mst",
                    customerId,
                    null,
                    newValuesJson,
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"CustomerCreate completed successfully | CustomerId:{customerId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "CUSTOMER_CREATED",
                        Data = responseData,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in CustomerCreate API", ex);
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

        #region 2. CustomerUpdate [PATCH]
        [Authorize]
        [HttpPatch("update")]
        public async Task<IActionResult> CustomerUpdate([FromBody] CustomerUpdateRequest? request)
        {
            Logs.Info("CustomerUpdate API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning("CustomerUpdate request rejected | Reason: Null request body");
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

                long currentUserId = GetAuthenticatedUserId();
                if (currentUserId <= 0)
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

                // Check Customer Record Exists
                string selectQuery =
                    @"
                    SELECT TOP 1 ID, FirstName, LastName, Email, MobileNo, IsActive
                    FROM drs_customer_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(
                    selectQuery,
                    new[] { new SqlParameter("@ID", currentUserId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"CustomerUpdate failed | Customer not found | CustomerId:{currentUserId}"
                    );
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

                DataRow existingRow = dt.Rows[0];
                string oldFirstName = existingRow["FirstName"].ToString() ?? "";
                string oldLastName = existingRow["LastName"].ToString() ?? "";
                string oldEmail = existingRow["Email"].ToString() ?? "";
                string oldMobileNo = existingRow["MobileNo"].ToString() ?? "";

                var errors = new List<string>();

                string newFirstName =
                    request.FirstName != null ? request.FirstName.Trim() : oldFirstName;
                string newLastName =
                    request.LastName != null ? request.LastName.Trim() : oldLastName;
                string newEmail = request.Email != null ? request.Email.Trim() : oldEmail;
                string newMobileNo =
                    request.MobileNo != null ? request.MobileNo.Trim() : oldMobileNo;

                if (request.FirstName != null && !ValidationHelper.IsValidName(newFirstName))
                    errors.Add("First name must be 2-50 alphabetic characters.");

                if (request.LastName != null && !ValidationHelper.IsValidName(newLastName))
                    errors.Add("Last name must be 2-50 alphabetic characters.");

                if (request.Email != null && !ValidationHelper.IsValidEmail(newEmail))
                    errors.Add("Invalid email format.");

                if (request.MobileNo != null && !ValidationHelper.IsValidMobile(newMobileNo))
                    errors.Add("Invalid mobile number format.");

                if (errors.Any())
                {
                    sw.Stop();
                    Logs.Warning(
                        $"CustomerUpdate validation failed | Errors:{string.Join(", ", errors)}"
                    );
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

                // Uniqueness Check for Email if changed (Case-insensitive)
                if (!string.Equals(oldEmail, newEmail, StringComparison.OrdinalIgnoreCase))
                {
                    string checkEmail =
                        @"
                        SELECT COUNT(1)
                        FROM drs_customer_mst WITH (NOLOCK)
                        WHERE LOWER(Email) = LOWER(@Email) AND ID <> @ID AND IsDeleted = 0";

                    int dupEmail = Convert.ToInt32(
                        _db.ExecuteScalar(
                            checkEmail,
                            new[]
                            {
                                new SqlParameter("@Email", newEmail),
                                new SqlParameter("@ID", currentUserId),
                            }
                        ) ?? 0
                    );

                    if (dupEmail > 0)
                    {
                        sw.Stop();
                        return Conflict(
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 409,
                                Message = "CUSTOMER_ALREADY_EXISTS",
                                Errors = new List<string>
                                {
                                    "Customer with given email already exists.",
                                },
                            }
                        );
                    }
                }

                // Uniqueness Check for Mobile if changed
                if (!string.Equals(oldMobileNo, newMobileNo, StringComparison.OrdinalIgnoreCase))
                {
                    string checkMobile =
                        @"
                        SELECT COUNT(1)
                        FROM drs_customer_mst WITH (NOLOCK)
                        WHERE MobileNo = @MobileNo AND ID <> @ID AND IsDeleted = 0";

                    int dupMobile = Convert.ToInt32(
                        _db.ExecuteScalar(
                            checkMobile,
                            new[]
                            {
                                new SqlParameter("@MobileNo", newMobileNo),
                                new SqlParameter("@ID", currentUserId),
                            }
                        ) ?? 0
                    );

                    if (dupMobile > 0)
                    {
                        sw.Stop();
                        return Conflict(
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 409,
                                Message = "CUSTOMER_ALREADY_EXISTS",
                                Errors = new List<string>
                                {
                                    "Customer with given mobile number already exists.",
                                },
                            }
                        );
                    }
                }

                string updateQuery =
                    @"
                    UPDATE drs_customer_mst
                    SET FirstName = @FirstName,
                        LastName = @LastName,
                        Email = @Email,
                        MobileNo = @MobileNo,
                        ModifiedDate = GETDATE()
                    WHERE ID = @ID AND IsDeleted = 0";

                _db.ExecuteNonQuery(
                    updateQuery,
                    new[]
                    {
                        new SqlParameter("@FirstName", newFirstName),
                        new SqlParameter("@LastName", newLastName),
                        new SqlParameter("@Email", newEmail),
                        new SqlParameter("@MobileNo", newMobileNo),
                        new SqlParameter("@ID", currentUserId),
                    }
                );

                // Audit Update
                string oldValueJson = System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        FirstName = oldFirstName,
                        LastName = oldLastName,
                        Email = oldEmail,
                        MobileNo = oldMobileNo,
                    }
                );

                string newValueJson = System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        FirstName = newFirstName,
                        LastName = newLastName,
                        Email = newEmail,
                        MobileNo = newMobileNo,
                    }
                );

                await _audit.InsertAuditAsync(
                    currentUserId,
                    "Customer",
                    "CustomerUpdate",
                    "drs_customer_mst",
                    currentUserId,
                    oldValueJson,
                    newValueJson,
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"CustomerUpdate completed successfully | CustomerId:{currentUserId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "CUSTOMER_UPDATED",
                        Data = new CustomerResponse
                        {
                            EncryptedId = _encryption.Encrypt(currentUserId),
                            FirstName = newFirstName,
                            LastName = newLastName,
                            Email = newEmail,
                            MobileNo = newMobileNo,
                            IsActive = Convert.ToBoolean(existingRow["IsActive"]),
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in CustomerUpdate API", ex);
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

        #region 3. CustomerSoftDelete [PATCH]
        [Authorize]
        [HttpPatch("delete")]
        public async Task<IActionResult> CustomerSoftDelete([FromQuery] string? encryptedId = null)
        {
            Logs.Info("CustomerSoftDelete API started");
            var sw = Stopwatch.StartNew();

            try
            {
                long currentUserId = GetAuthenticatedUserId();
                long targetCustomerId = currentUserId;

                if (!string.IsNullOrWhiteSpace(encryptedId))
                {
                    if (!_encryption.TryDecrypt(encryptedId, out long decryptedId))
                    {
                        sw.Stop();
                        return BadRequest(
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 400,
                                Message = "INVALID_REQUEST",
                                Errors = new List<string> { "Invalid customer ID parameter." },
                            }
                        );
                    }
                    targetCustomerId = decryptedId;
                }

                // Authorization Check: Customer can only soft delete their own account unless caller is Admin
                if (targetCustomerId != currentUserId && !IsCurrentUserAdmin())
                {
                    sw.Stop();
                    Logs.Warning(
                        $"CustomerSoftDelete forbidden | Caller:{currentUserId} Target:{targetCustomerId}"
                    );
                    return StatusCode(
                        403,
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 403,
                            Message = "FORBIDDEN",
                            Errors = new List<string>
                            {
                                "You are not authorized to delete this customer record.",
                            },
                        }
                    );
                }

                // Check Customer Exists & Non-Deleted
                string checkQuery =
                    @"
                    SELECT TOP 1 ID, FirstName, LastName, Email, MobileNo
                    FROM drs_customer_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(
                    checkQuery,
                    new[] { new SqlParameter("@ID", targetCustomerId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"CustomerSoftDelete failed | Customer not found | CustomerId:{targetCustomerId}"
                    );
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "CUSTOMER_NOT_FOUND",
                            Errors = new List<string>
                            {
                                "Customer record not found or already deleted.",
                            },
                        }
                    );
                }

                // Perform Soft Delete
                string deleteQuery =
                    @"
                    UPDATE drs_customer_mst
                    SET IsDeleted = 1,
                        IsActive = 0,
                        ModifiedDate = GETDATE()
                    WHERE ID = @ID";

                _db.ExecuteNonQuery(
                    deleteQuery,
                    new[] { new SqlParameter("@ID", targetCustomerId) }
                );

                // Audit Entry
                await _audit.InsertAuditAsync(
                    currentUserId,
                    "Customer",
                    "CustomerSoftDelete",
                    "drs_customer_mst",
                    targetCustomerId,
                    System.Text.Json.JsonSerializer.Serialize(new { IsActive = 1, IsDeleted = 0 }),
                    System.Text.Json.JsonSerializer.Serialize(new { IsActive = 0, IsDeleted = 1 }),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info(
                    $"CustomerSoftDelete completed successfully | TargetCustomerId:{targetCustomerId}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "CUSTOMER_DELETED",
                        Data = null,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in CustomerSoftDelete API", ex);
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

        #region 4. GetCustomerById [GET]
        [Authorize]
        [HttpGet("{encryptedId}")]
        public IActionResult GetCustomerById([FromRoute] string encryptedId)
        {
            Logs.Info("GetCustomerById API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (
                    !_encryption.TryDecrypt(encryptedId, out long requestedCustomerId)
                    || requestedCustomerId <= 0
                )
                {
                    sw.Stop();
                    Logs.Warning($"GetCustomerById invalid encrypted ID: {encryptedId}");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string>
                            {
                                "Invalid or tampered encrypted customer ID.",
                            },
                        }
                    );
                }

                long currentUserId = GetAuthenticatedUserId();

                // Ownership Check: Customer can only view their own profile unless role is Admin
                if (requestedCustomerId != currentUserId && !IsCurrentUserAdmin())
                {
                    sw.Stop();
                    Logs.Warning(
                        $"GetCustomerById forbidden access attempt | Caller:{currentUserId} Requested:{requestedCustomerId}"
                    );
                    return StatusCode(
                        403,
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 403,
                            Message = "FORBIDDEN",
                            Errors = new List<string>
                            {
                                "You are not authorized to view this customer profile.",
                            },
                        }
                    );
                }

                string query =
                    @"
                    SELECT TOP 1 ID, Account_no, FirstName, LastName, Email, MobileNo, IsAdmin, IsEmailVerified, IsMobileVerified, IsActive, CreatedDate
                    FROM drs_customer_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(
                    query,
                    new[] { new SqlParameter("@ID", requestedCustomerId) }
                );

                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"GetCustomerById customer not found | CustomerId:{requestedCustomerId}"
                    );
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

                DataRow row = dt.Rows[0];
                var customerResponse = new CustomerResponse
                {
                    EncryptedId = _encryption.Encrypt(Convert.ToInt64(row["ID"])),
                    AccountNo = row["Account_no"].ToString() ?? "",
                    FirstName = row["FirstName"].ToString() ?? "",
                    LastName = row["LastName"].ToString() ?? "",
                    Email = row["Email"].ToString() ?? "",
                    MobileNo = row["MobileNo"].ToString() ?? "",
                    IsAdmin = Convert.ToBoolean(row["IsAdmin"]),
                    IsEmailVerified = Convert.ToBoolean(row["IsEmailVerified"]),
                    IsMobileVerified = Convert.ToBoolean(row["IsMobileVerified"]),
                    IsActive = Convert.ToBoolean(row["IsActive"]),
                    CreatedDate = DateTimeFormat.Format(Convert.ToDateTime(row["CreatedDate"])),
                };

                sw.Stop();
                Logs.Info(
                    $"GetCustomerById completed successfully | CustomerId:{requestedCustomerId}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = customerResponse,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetCustomerById API", ex);
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

        #region 5. GetAllCustomer [GET / POST]
        [Authorize(Roles = "Admin")]
        [HttpGet("list")]
        [HttpPost("list")]
        public IActionResult GetAllCustomer(
            [FromQuery] CustomerListRequest? queryRequest,
            [FromBody] CustomerListRequest? bodyRequest
        )
        {
            Logs.Info("GetAllCustomer API started");
            var sw = Stopwatch.StartNew();

            var request =
                (HttpMethods.IsPost(Request.Method) ? bodyRequest : queryRequest)
                ?? queryRequest
                ?? bodyRequest
                ?? new CustomerListRequest();

            try
            {
                int pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
                int pageSize =
                    request.PageSize < 1 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);
                int offset = (pageIndex - 1) * pageSize;
                string search = request.Search?.Trim() ?? string.Empty;

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@Offset", offset),
                    new SqlParameter("@PageSize", pageSize),
                };

                string whereClause = "WHERE IsDeleted = 0";

                if (!string.IsNullOrWhiteSpace(search))
                {
                    whereClause +=
                        " AND (Account_no LIKE @Search OR FirstName LIKE @Search OR LastName LIKE @Search OR Email LIKE @Search OR MobileNo LIKE @Search)";
                    parameters.Add(new SqlParameter("@Search", $"%{search}%"));
                }

                if (request.IsActive.HasValue)
                {
                    whereClause += " AND IsActive = @IsActive";
                    parameters.Add(new SqlParameter("@IsActive", request.IsActive.Value));
                }

                string countQuery =
                    $"SELECT COUNT(1) FROM drs_customer_mst WITH (NOLOCK) {whereClause}";
                int totalRecords = Convert.ToInt32(
                    _db.ExecuteScalar(countQuery, parameters.ToArray()) ?? 0
                );

                string listQuery =
                    $@"
                    SELECT ID, Account_no, FirstName, LastName, Email, MobileNo, IsAdmin, IsEmailVerified, IsMobileVerified, IsActive, CreatedDate
                    FROM drs_customer_mst WITH (NOLOCK)
                    {whereClause}
                    ORDER BY ID DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                DataTable dt = _db.ExecuteQuery(listQuery, parameters.ToArray());

                var customerList = new List<CustomerResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    customerList.Add(
                        new CustomerResponse
                        {
                            EncryptedId = _encryption.Encrypt(Convert.ToInt64(row["ID"])),
                            AccountNo = row["Account_no"].ToString() ?? "",
                            FirstName = row["FirstName"].ToString() ?? "",
                            LastName = row["LastName"].ToString() ?? "",
                            Email = row["Email"].ToString() ?? "",
                            MobileNo = row["MobileNo"].ToString() ?? "",
                            IsAdmin = Convert.ToBoolean(row["IsAdmin"]),
                            IsEmailVerified = Convert.ToBoolean(row["IsEmailVerified"]),
                            IsMobileVerified = Convert.ToBoolean(row["IsMobileVerified"]),
                            IsActive = Convert.ToBoolean(row["IsActive"]),
                            CreatedDate = DateTimeFormat.Format(
                                Convert.ToDateTime(row["CreatedDate"])
                            ),
                        }
                    );
                }

                var responseData = new CustomerListResponse
                {
                    Customers = customerList,
                    TotalRecords = totalRecords,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                };

                sw.Stop();
                Logs.Info(
                    $"GetAllCustomer completed successfully | PageIndex:{pageIndex} PageSize:{pageSize} TotalRecords:{totalRecords}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = responseData,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetAllCustomer API", ex);
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
