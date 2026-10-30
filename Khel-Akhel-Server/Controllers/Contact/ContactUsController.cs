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

namespace Khel_Akhel_Server.Controllers.Contact
{
    [ApiController]
    [Route("api/contact-us")]
    public class ContactUsController : ControllerBase
    {
        private readonly DbHelper _db;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService _audit;

        public ContactUsController(
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

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return Request.Headers["User-Agent"].ToString() ?? "Unknown";
        }
        #endregion

        #region Submit Contact Inquiry API
        [AllowAnonymous]
        [EnableRateLimiting("public-form")]
        [HttpPost]
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitInquiry(
            [FromBody] ContactInquiryCreateRequest? request
        )
        {
            Logs.Info("SubmitInquiry API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning("SubmitInquiry rejected | Reason: Null request body");
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

                // Honeypot check
                if (!string.IsNullOrWhiteSpace(request.Website))
                {
                    sw.Stop();
                    Logs.Warning(
                        $"Honeypot triggered for SubmitInquiry from IP {GetClientIpAddress()}"
                    );
                    return Ok(
                        new ApiResponse
                        {
                            Success = true,
                            StatusCode = 200,
                            Message = "CONTACT_INQUIRY_SUBMITTED",
                            Data = new ContactInquirySubmitResult
                            {
                                EncryptedInquiryId = string.Empty,
                                FullName = request.FullName,
                                Email = request.Email,
                                ConfirmationMessage =
                                    "Your inquiry has been received. Our team will contact you shortly.",
                            },
                        }
                    );
                }

                // Normalization
                request.FullName = request.FullName?.Trim() ?? string.Empty;
                request.MobileNumber = request.MobileNumber?.Trim() ?? string.Empty;
                request.Email = request.Email?.Trim() ?? string.Empty;
                request.CompanyName = string.IsNullOrWhiteSpace(request.CompanyName)
                    ? null
                    : request.CompanyName.Trim();
                request.ServiceDescription = request.ServiceDescription?.Trim() ?? string.Empty;

                var validationErrors = new List<string>();

                // FullName Validation
                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    validationErrors.Add("Full name is required.");
                }
                else if (request.FullName.Length < 2 || request.FullName.Length > 150)
                {
                    validationErrors.Add("Full name must be between 2 and 150 characters.");
                }

                // MobileNumber Validation
                if (string.IsNullOrWhiteSpace(request.MobileNumber))
                {
                    validationErrors.Add("Mobile number is required.");
                }
                else if (!ValidationHelper.IsValidInternationalMobile(request.MobileNumber))
                {
                    validationErrors.Add(
                        "Please enter a valid phone number (between 7 and 20 characters with valid international format)."
                    );
                }

                // Email Validation
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    validationErrors.Add("Email address is required.");
                }
                else if (
                    request.Email.Length > 254
                    || !ValidationHelper.IsValidEmail(request.Email)
                )
                {
                    validationErrors.Add("Please enter a valid email address.");
                }

                // CompanyName Validation (Optional)
                if (request.CompanyName != null && request.CompanyName.Length > 200)
                {
                    validationErrors.Add("Company name cannot exceed 200 characters.");
                }

                // ServiceDescription Validation
                if (string.IsNullOrWhiteSpace(request.ServiceDescription))
                {
                    validationErrors.Add("Service description is required.");
                }
                else if (request.ServiceDescription.Length < 5)
                {
                    validationErrors.Add("Service description must be at least 5 characters long.");
                }
                else if (request.ServiceDescription.Length > 300)
                {
                    validationErrors.Add("Service description cannot exceed 300 characters.");
                }

                if (validationErrors.Count > 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"SubmitInquiry rejected | Reason: Validation failed: {string.Join("; ", validationErrors)}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = validationErrors,
                        }
                    );
                }

                long customerId = GetAuthenticatedUserId();
                string ipAddress = GetClientIpAddress();
                string userAgent = GetUserAgent();

                string insertSql =
                    @"
                    INSERT INTO drs_contact_inquiry_mst
                    (FullName, MobileNo, Email, CompanyName, ServiceDescription, customer_id, IpAddress, UserAgent, IsActive, IsDeleted, CreatedDate)
                    OUTPUT INSERTED.ID
                    VALUES
                    (@FullName, @MobileNo, @Email, @CompanyName, @ServiceDescription, @CustomerId, @IpAddress, @UserAgent, 1, 0, GETDATE());";

                object? custParam = customerId > 0 ? customerId : DBNull.Value;
                object? compParam = string.IsNullOrWhiteSpace(request.CompanyName)
                    ? DBNull.Value
                    : request.CompanyName;
                object? ipParam = string.IsNullOrWhiteSpace(ipAddress) ? DBNull.Value : ipAddress;
                object? uaParam = string.IsNullOrWhiteSpace(userAgent) ? DBNull.Value : userAgent;

                SqlParameter[] parameters = new[]
                {
                    new SqlParameter("@FullName", request.FullName),
                    new SqlParameter("@MobileNo", request.MobileNumber),
                    new SqlParameter("@Email", request.Email),
                    new SqlParameter("@CompanyName", compParam),
                    new SqlParameter("@ServiceDescription", request.ServiceDescription),
                    new SqlParameter("@CustomerId", custParam),
                    new SqlParameter("@IpAddress", ipParam),
                    new SqlParameter("@UserAgent", uaParam),
                };

                object? idResult = await _db.ExecuteScalarAsync(insertSql, parameters);
                if (idResult == null || idResult == DBNull.Value)
                {
                    sw.Stop();
                    Logs.Error(
                        "SubmitInquiry failed | Database failed to return inserted inquiry ID",
                        new Exception("Null scalar result")
                    );
                    return StatusCode(
                        500,
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 500,
                            Message = "SERVER_ERROR",
                            Errors = new List<string>
                            {
                                "An internal server error occurred while submitting your inquiry.",
                            },
                        }
                    );
                }

                long inquiryId = Convert.ToInt64(idResult);

                // Audit logging
                await _audit.InsertAuditAsync(
                    customerId > 0 ? customerId : null,
                    "ContactUs",
                    "SubmitInquiry",
                    "drs_contact_inquiry_mst",
                    inquiryId,
                    null,
                    System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            InquiryId = inquiryId,
                            FullName = request.FullName,
                            Email = request.Email,
                            CompanyName = request.CompanyName,
                        }
                    ),
                    ipAddress,
                    userAgent
                );

                sw.Stop();
                Logs.Info(
                    $"SubmitInquiry completed | InquiryId: {inquiryId} Elapsed: {sw.ElapsedMilliseconds}ms"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "CONTACT_INQUIRY_SUBMITTED",
                        Data = new ContactInquirySubmitResult
                        {
                            EncryptedInquiryId = _encryption.Encrypt(inquiryId),
                            FullName = request.FullName,
                            Email = request.Email,
                            ConfirmationMessage =
                                "Thank you for contacting Khel-Akhel. Our team will review your inquiry and get in touch with you shortly.",
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in SubmitInquiry API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while processing your inquiry.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Admin Inquiries List API
        [Authorize(Roles = "Admin")]
        [HttpGet("list")]
        [HttpGet("admin/list")]
        [HttpGet]
        public async Task<IActionResult> GetInquiriesList(
            [FromQuery] ContactInquiryFilterRequest? filter
        )
        {
            Logs.Info("GetInquiriesList API started");
            var sw = Stopwatch.StartNew();

            try
            {
                var req = filter ?? new ContactInquiryFilterRequest();
                int pageIndex = req.PageIndex < 1 ? 1 : req.PageIndex;
                int pageSize = req.PageSize < 1 ? 10 : (req.PageSize > 100 ? 100 : req.PageSize);
                int offset = (pageIndex - 1) * pageSize;

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@Offset", offset),
                    new SqlParameter("@PageSize", pageSize),
                };

                string whereClause = "WHERE i.IsDeleted = 0";

                if (req.IsActive.HasValue)
                {
                    whereClause += " AND i.IsActive = @IsActive";
                    parameters.Add(new SqlParameter("@IsActive", req.IsActive.Value));
                }

                if (req.StartDate.HasValue)
                {
                    whereClause += " AND i.CreatedDate >= @StartDate";
                    parameters.Add(new SqlParameter("@StartDate", req.StartDate.Value.Date));
                }

                if (req.EndDate.HasValue)
                {
                    whereClause += " AND i.CreatedDate < @EndDate";
                    parameters.Add(new SqlParameter("@EndDate", req.EndDate.Value.Date.AddDays(1)));
                }

                if (!string.IsNullOrWhiteSpace(req.Search))
                {
                    string term = $"%{req.Search.Trim()}%";
                    whereClause +=
                        @" AND (
                        i.FullName LIKE @Search OR 
                        i.Email LIKE @Search OR 
                        i.MobileNo LIKE @Search OR 
                        i.CompanyName LIKE @Search OR 
                        i.ServiceDescription LIKE @Search
                    )";
                    parameters.Add(new SqlParameter("@Search", term));
                }

                string orderByClause = (req.SortBy?.ToLowerInvariant()) switch
                {
                    "oldest" => "i.CreatedDate ASC",
                    "name_asc" => "i.FullName ASC",
                    "name_desc" => "i.FullName DESC",
                    _ => "i.CreatedDate DESC",
                };

                string countQuery =
                    $"SELECT COUNT(1) FROM drs_contact_inquiry_mst i WITH (NOLOCK) {whereClause};";
                object? countObj = await _db.ExecuteScalarAsync(countQuery, parameters.ToArray());
                int totalRecords =
                    (countObj != null && countObj != DBNull.Value) ? Convert.ToInt32(countObj) : 0;

                string listQuery =
                    $@"
                    SELECT 
                        i.ID, i.FullName, i.MobileNo, i.Email, i.CompanyName,
                        i.ServiceDescription, i.customer_id, i.IpAddress, i.UserAgent,
                        i.IsActive, i.CreatedDate, i.ModifiedDate,
                        c.Account_no AS CustomerAccountNo
                    FROM drs_contact_inquiry_mst i WITH (NOLOCK)
                    LEFT JOIN drs_customer_mst c WITH (NOLOCK) ON c.ID = i.customer_id AND c.IsDeleted = 0
                    {whereClause}
                    ORDER BY {orderByClause}
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                DataTable dt = await _db.ExecuteQueryAsync(listQuery, parameters.ToArray());

                var items = new List<ContactInquiryDetailResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    long id = Convert.ToInt64(row["ID"]);
                    items.Add(
                        new ContactInquiryDetailResponse
                        {
                            EncryptedInquiryId = _encryption.Encrypt(id),
                            FullName = row["FullName"].ToString() ?? string.Empty,
                            MobileNumber = row["MobileNo"].ToString() ?? string.Empty,
                            Email = row["Email"].ToString() ?? string.Empty,
                            CompanyName =
                                row["CompanyName"] != DBNull.Value
                                    ? row["CompanyName"].ToString()
                                    : null,
                            ServiceDescription =
                                row["ServiceDescription"].ToString() ?? string.Empty,
                            CustomerAccountNo =
                                row["CustomerAccountNo"] != DBNull.Value
                                    ? row["CustomerAccountNo"].ToString()
                                    : null,
                            IpAddress =
                                row["IpAddress"] != DBNull.Value
                                    ? row["IpAddress"].ToString()
                                    : null,
                            UserAgent =
                                row["UserAgent"] != DBNull.Value
                                    ? row["UserAgent"].ToString()
                                    : null,
                            IsActive = Convert.ToBoolean(row["IsActive"]),
                            CreatedDate = Convert.ToDateTime(row["CreatedDate"]),
                            ModifiedDate =
                                row["ModifiedDate"] != DBNull.Value
                                    ? Convert.ToDateTime(row["ModifiedDate"])
                                    : null,
                        }
                    );
                }

                sw.Stop();
                Logs.Info(
                    $"GetInquiriesList completed | Total: {totalRecords} Retrieved: {items.Count} Elapsed: {sw.ElapsedMilliseconds}ms"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "CONTACT_INQUIRIES_FETCHED",
                        Data = new ContactInquiryListResponse
                        {
                            Items = items,
                            TotalRecords = totalRecords,
                            PageIndex = pageIndex,
                            PageSize = pageSize,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception in GetInquiriesList API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while retrieving contact inquiries.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Admin Get Inquiry Detail API
        [Authorize(Roles = "Admin")]
        [HttpGet("{encryptedInquiryId}")]
        [HttpGet("admin/{encryptedInquiryId}")]
        public async Task<IActionResult> GetInquiryById([FromRoute] string encryptedInquiryId)
        {
            Logs.Info($"GetInquiryById API started | Id: {encryptedInquiryId}");
            var sw = Stopwatch.StartNew();

            try
            {
                if (
                    string.IsNullOrWhiteSpace(encryptedInquiryId)
                    || !_encryption.TryDecrypt(encryptedInquiryId, out long inquiryId)
                    || inquiryId <= 0
                )
                {
                    sw.Stop();
                    Logs.Warning($"GetInquiryById rejected | Invalid EncryptedId: {encryptedInquiryId}");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string> { "Invalid contact inquiry ID." },
                        }
                    );
                }

                string selectSql =
                    @"
                    SELECT TOP 1
                        i.ID, i.FullName, i.MobileNo, i.Email, i.CompanyName,
                        i.ServiceDescription, i.customer_id, i.IpAddress, i.UserAgent,
                        i.IsActive, i.CreatedDate, i.ModifiedDate,
                        c.Account_no AS CustomerAccountNo
                    FROM drs_contact_inquiry_mst i WITH (NOLOCK)
                    LEFT JOIN drs_customer_mst c WITH (NOLOCK) ON c.ID = i.customer_id AND c.IsDeleted = 0
                    WHERE i.ID = @ID AND i.IsDeleted = 0;";

                DataTable dt = await _db.ExecuteQueryAsync(
                    selectSql,
                    new[] { new SqlParameter("@ID", inquiryId) }
                );

                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning($"GetInquiryById not found | InquiryId: {inquiryId}");
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "INQUIRY_NOT_FOUND",
                            Errors = new List<string> { "Contact inquiry not found." },
                        }
                    );
                }

                DataRow row = dt.Rows[0];
                var detail = new ContactInquiryDetailResponse
                {
                    EncryptedInquiryId = _encryption.Encrypt(inquiryId),
                    FullName = row["FullName"].ToString() ?? string.Empty,
                    MobileNumber = row["MobileNo"].ToString() ?? string.Empty,
                    Email = row["Email"].ToString() ?? string.Empty,
                    CompanyName =
                        row["CompanyName"] != DBNull.Value
                            ? row["CompanyName"].ToString()
                            : null,
                    ServiceDescription =
                        row["ServiceDescription"].ToString() ?? string.Empty,
                    CustomerAccountNo =
                        row["CustomerAccountNo"] != DBNull.Value
                            ? row["CustomerAccountNo"].ToString()
                            : null,
                    IpAddress =
                        row["IpAddress"] != DBNull.Value ? row["IpAddress"].ToString() : null,
                    UserAgent =
                        row["UserAgent"] != DBNull.Value ? row["UserAgent"].ToString() : null,
                    IsActive = Convert.ToBoolean(row["IsActive"]),
                    CreatedDate = Convert.ToDateTime(row["CreatedDate"]),
                    ModifiedDate =
                        row["ModifiedDate"] != DBNull.Value
                            ? Convert.ToDateTime(row["ModifiedDate"])
                            : null,
                };

                sw.Stop();
                Logs.Info($"GetInquiryById completed | InquiryId: {inquiryId} Elapsed: {sw.ElapsedMilliseconds}ms");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "CONTACT_INQUIRY_FETCHED",
                        Data = detail,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception in GetInquiryById API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while retrieving contact inquiry details.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Admin Toggle Status API
        [Authorize(Roles = "Admin")]
        [HttpPatch("status/{encryptedInquiryId}")]
        [HttpPatch("toggle-active/{encryptedInquiryId}")]
        [HttpPut("status/{encryptedInquiryId}")]
        public async Task<IActionResult> ToggleInquiryStatus(
            [FromRoute] string encryptedInquiryId,
            [FromBody] ContactInquiryStatusUpdateRequest? body
        )
        {
            Logs.Info($"ToggleInquiryStatus API started | Id: {encryptedInquiryId}");
            var sw = Stopwatch.StartNew();

            try
            {
                if (
                    string.IsNullOrWhiteSpace(encryptedInquiryId)
                    || !_encryption.TryDecrypt(encryptedInquiryId, out long inquiryId)
                    || inquiryId <= 0
                )
                {
                    sw.Stop();
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string> { "Invalid contact inquiry ID." },
                        }
                    );
                }

                string getStatusSql =
                    "SELECT TOP 1 IsActive FROM drs_contact_inquiry_mst WHERE ID = @ID AND IsDeleted = 0;";
                object? currentActiveObj = await _db.ExecuteScalarAsync(
                    getStatusSql,
                    new[] { new SqlParameter("@ID", inquiryId) }
                );

                if (currentActiveObj == null || currentActiveObj == DBNull.Value)
                {
                    sw.Stop();
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "INQUIRY_NOT_FOUND",
                            Errors = new List<string> { "Contact inquiry not found." },
                        }
                    );
                }

                bool currentActive = Convert.ToBoolean(currentActiveObj);
                bool newStatus = body != null ? body.IsActive : !currentActive;

                string updateSql =
                    @"
                    UPDATE drs_contact_inquiry_mst
                    SET IsActive = @IsActive, ModifiedDate = GETDATE()
                    WHERE ID = @ID AND IsDeleted = 0;";

                await _db.ExecuteNonQueryAsync(
                    updateSql,
                    new[]
                    {
                        new SqlParameter("@IsActive", newStatus),
                        new SqlParameter("@ID", inquiryId),
                    }
                );

                long adminId = GetAuthenticatedUserId();
                await _audit.InsertAuditAsync(
                    adminId > 0 ? adminId : null,
                    "ContactUs",
                    "ToggleInquiryStatus",
                    "drs_contact_inquiry_mst",
                    inquiryId,
                    System.Text.Json.JsonSerializer.Serialize(new { IsActive = currentActive }),
                    System.Text.Json.JsonSerializer.Serialize(new { IsActive = newStatus }),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"ToggleInquiryStatus completed | InquiryId: {inquiryId} NewStatus: {newStatus}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "STATUS_UPDATED",
                        Data = new
                        {
                            EncryptedInquiryId = encryptedInquiryId,
                            IsActive = newStatus,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception in ToggleInquiryStatus API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while updating inquiry status.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Admin Delete Inquiry API
        [Authorize(Roles = "Admin")]
        [HttpDelete("{encryptedInquiryId}")]
        [HttpDelete("admin/{encryptedInquiryId}")]
        public async Task<IActionResult> DeleteInquiry([FromRoute] string encryptedInquiryId)
        {
            Logs.Info($"DeleteInquiry API started | Id: {encryptedInquiryId}");
            var sw = Stopwatch.StartNew();

            try
            {
                if (
                    string.IsNullOrWhiteSpace(encryptedInquiryId)
                    || !_encryption.TryDecrypt(encryptedInquiryId, out long inquiryId)
                    || inquiryId <= 0
                )
                {
                    sw.Stop();
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string> { "Invalid contact inquiry ID." },
                        }
                    );
                }

                string checkSql =
                    "SELECT TOP 1 ID FROM drs_contact_inquiry_mst WHERE ID = @ID AND IsDeleted = 0;";
                object? exists = await _db.ExecuteScalarAsync(
                    checkSql,
                    new[] { new SqlParameter("@ID", inquiryId) }
                );

                if (exists == null || exists == DBNull.Value)
                {
                    sw.Stop();
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "INQUIRY_NOT_FOUND",
                            Errors = new List<string> { "Contact inquiry not found." },
                        }
                    );
                }

                string deleteSql =
                    @"
                    UPDATE drs_contact_inquiry_mst
                    SET IsDeleted = 1, ModifiedDate = GETDATE()
                    WHERE ID = @ID AND IsDeleted = 0;";

                await _db.ExecuteNonQueryAsync(
                    deleteSql,
                    new[] { new SqlParameter("@ID", inquiryId) }
                );

                long adminId = GetAuthenticatedUserId();
                await _audit.InsertAuditAsync(
                    adminId > 0 ? adminId : null,
                    "ContactUs",
                    "DeleteInquiry",
                    "drs_contact_inquiry_mst",
                    inquiryId,
                    null,
                    "Soft-deleted contact inquiry",
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"DeleteInquiry completed | InquiryId: {inquiryId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "INQUIRY_DELETED",
                        Data = true,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception in DeleteInquiry API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while deleting the contact inquiry.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Admin Summary Stats API
        [Authorize(Roles = "Admin")]
        [HttpGet("summary")]
        [HttpGet("stats")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                string sql =
                    @"
                    SELECT 
                        COUNT(1) AS TotalInquiries,
                        ISNULL(SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END), 0) AS ActiveInquiries,
                        ISNULL(SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END), 0) AS InactiveInquiries,
                        ISNULL(SUM(CASE WHEN CAST(CreatedDate AS DATE) = CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END), 0) AS TodayInquiries
                    FROM drs_contact_inquiry_mst WITH (NOLOCK)
                    WHERE IsDeleted = 0;";

                DataTable dt = await _db.ExecuteQueryAsync(sql);
                var summary = new ContactInquirySummaryResponse();
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    summary.TotalInquiries = Convert.ToInt32(row["TotalInquiries"]);
                    summary.ActiveInquiries = Convert.ToInt32(row["ActiveInquiries"]);
                    summary.InactiveInquiries = Convert.ToInt32(row["InactiveInquiries"]);
                    summary.TodayInquiries = Convert.ToInt32(row["TodayInquiries"]);
                }

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUMMARY_FETCHED",
                        Data = summary,
                    }
                );
            }
            catch (Exception ex)
            {
                Logs.Error("Exception in GetSummary API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while retrieving inquiry summary.",
                        },
                    }
                );
            }
        }
        #endregion
    }
}
