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
    [Route("api/admin/password-reset")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("auth-session")]
    public class PasswordResetAdminController : ControllerBase
    {
        private readonly DbHelper _db;
        private readonly IConfiguration _configuration;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService _audit;

        public PasswordResetAdminController(
            DbHelper db,
            IConfiguration configuration,
            IUrlEncryptionService encryption,
            IAuditService audit
        )
        {
            _db = db;
            _configuration = configuration;
            _encryption = encryption;
            _audit = audit;
        }

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

        #region List Password Reset Requests
        [HttpGet("requests")]
        public IActionResult GetRequests(
            [FromQuery] string status = "PENDING",
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null
        )
        {
            Logs.Info("PasswordResetAdmin.GetRequests API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (pageIndex < 1) pageIndex = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                // 1. Auto-mark expired tokens
                _db.ExecuteNonQuery(@"
                    UPDATE drs_password_reset_request_mst
                    SET Status = 'EXPIRED', ModifiedDate = GETDATE()
                    WHERE Status = 'LINK_ISSUED'
                      AND TokenExpiresUtc <= GETUTCDATE()
                      AND UsedUtc IS NULL
                      AND IsDeleted = 0");

                var parameters = new List<SqlParameter>();
                string whereClause = "r.IsDeleted = 0";

                if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "ALL", StringComparison.OrdinalIgnoreCase))
                {
                    whereClause += " AND r.Status = @Status";
                    parameters.Add(new SqlParameter("@Status", status.ToUpperInvariant()));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    whereClause += @" AND (
                        c.FirstName LIKE @Search OR
                        c.LastName LIKE @Search OR
                        c.Email LIKE @Search OR
                        c.MobileNo LIKE @Search OR
                        c.Account_no LIKE @Search
                    )";
                    parameters.Add(new SqlParameter("@Search", $"%{search.Trim()}%"));
                }

                // Count total matching records
                string countQuery = $@"
                    SELECT COUNT(1)
                    FROM drs_password_reset_request_mst r
                    INNER JOIN drs_customer_mst c ON c.ID = r.customer_id
                    WHERE {whereClause}";

                object? countObj = _db.ExecuteScalar(countQuery, parameters.ToArray());
                int totalRecords = countObj != null && countObj != DBNull.Value ? Convert.ToInt32(countObj) : 0;

                // Query pending count badge
                object? pendingCountObj = _db.ExecuteScalar(
                    "SELECT COUNT(1) FROM drs_password_reset_request_mst WHERE Status = 'PENDING' AND IsDeleted = 0",
                    null
                );
                int pendingCount = pendingCountObj != null && pendingCountObj != DBNull.Value ? Convert.ToInt32(pendingCountObj) : 0;

                // Paged query
                int offset = (pageIndex - 1) * pageSize;
                parameters.Add(new SqlParameter("@Offset", offset));
                parameters.Add(new SqlParameter("@PageSize", pageSize));

                string pagedQuery = $@"
                    SELECT 
                        r.ID,
                        r.customer_id,
                        r.Source,
                        r.Status,
                        r.RequestedIp,
                        r.UserAgent,
                        r.TokenExpiresUtc,
                        r.IssuedUtc,
                        r.AdminNotes,
                        r.CreatedDate,
                        c.Account_no,
                        c.FirstName,
                        c.LastName,
                        c.Email,
                        c.MobileNo,
                        a.FirstName AS AdminFirstName,
                        a.LastName AS AdminLastName
                    FROM drs_password_reset_request_mst r
                    INNER JOIN drs_customer_mst c ON c.ID = r.customer_id
                    LEFT JOIN drs_customer_mst a ON a.ID = r.IssuedByAdminId
                    WHERE {whereClause}
                    ORDER BY r.CreatedDate DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                DataTable dt = _db.ExecuteQuery(pagedQuery, parameters.ToArray());

                var items = new List<PasswordResetRequestAdminDto>();
                foreach (DataRow row in dt.Rows)
                {
                    long reqId = Convert.ToInt64(row["ID"]);
                    long custId = Convert.ToInt64(row["customer_id"]);

                    string adminName = string.Empty;
                    if (row["AdminFirstName"] != DBNull.Value)
                    {
                        adminName = $"{row["AdminFirstName"]} {row["AdminLastName"]}".Trim();
                    }

                    items.Add(new PasswordResetRequestAdminDto
                    {
                        RequestId = _encryption.Encrypt(reqId),
                        CustomerId = _encryption.Encrypt(custId),
                        AccountNo = row["Account_no"]?.ToString() ?? string.Empty,
                        FullName = $"{row["FirstName"]} {row["LastName"]}".Trim(),
                        Email = row["Email"]?.ToString() ?? string.Empty,
                        MobileNo = row["MobileNo"]?.ToString() ?? string.Empty,
                        Status = row["Status"]?.ToString() ?? string.Empty,
                        Source = row["Source"]?.ToString() ?? string.Empty,
                        RequestedDate = DateTimeFormat.Format(Convert.ToDateTime(row["CreatedDate"])),
                        RequestedIp = row["RequestedIp"] != DBNull.Value ? row["RequestedIp"]?.ToString() : null,
                        IssuedByAdminName = !string.IsNullOrEmpty(adminName) ? adminName : null,
                        IssuedDate = row["IssuedUtc"] != DBNull.Value ? DateTimeFormat.Format(Convert.ToDateTime(row["IssuedUtc"])) : null,
                        TokenExpiresDate = row["TokenExpiresUtc"] != DBNull.Value ? DateTimeFormat.Format(Convert.ToDateTime(row["TokenExpiresUtc"])) : null,
                        AdminNotes = row["AdminNotes"] != DBNull.Value ? row["AdminNotes"]?.ToString() : null
                    });
                }

                sw.Stop();
                Logs.Info($"PasswordResetAdmin.GetRequests completed successfully | TotalRecords:{totalRecords} PageIndex:{pageIndex} Status:{status}");
                return Ok(new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "PASSWORD_RESET_REQUESTS_FETCHED",
                    Data = new
                    {
                        TotalRecords = totalRecords,
                        PageIndex = pageIndex,
                        PageSize = pageSize,
                        TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                        PendingCount = pendingCount,
                        Requests = items
                    }
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetRequests API", ex);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "SERVER_ERROR",
                    Errors = new List<string> { "An internal server error occurred." }
                });
            }
        }
        #endregion

        #region Issue Reset Link For Request
        [HttpPost("requests/{encryptedRequestId}/issue-link")]
        public async Task<IActionResult> IssueLinkForRequest(
            [FromRoute] string encryptedRequestId,
            [FromBody] IssueResetLinkRequestDto? request
        )
        {
            Logs.Info("PasswordResetAdmin.IssueLinkForRequest API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.AdminNotes))
                {
                    sw.Stop();
                    Logs.Warning("PasswordResetAdmin.IssueLinkForRequest rejected: Verification notes are required");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "VALIDATION_FAILED",
                        Errors = new List<string> { "Verification notes are required. Please document how identity was verified." }
                    });
                }

                if (!_encryption.TryDecrypt(encryptedRequestId, out long requestId))
                {
                    sw.Stop();
                    Logs.Warning($"PasswordResetAdmin.IssueLinkForRequest rejected: Failed to decrypt RequestId '{encryptedRequestId}'");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_REQUEST_ID",
                        Errors = new List<string> { "Invalid request identifier." }
                    });
                }

                long adminId = GetAuthenticatedUserId();

                // Lookup request and customer details
                string query = @"
                    SELECT r.ID, r.customer_id, r.Status, c.IsAdmin, c.IsActive, c.IsDeleted, c.Email
                    FROM drs_password_reset_request_mst r
                    INNER JOIN drs_customer_mst c ON c.ID = r.customer_id
                    WHERE r.ID = @ID AND r.IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(query, new[] { new SqlParameter("@ID", requestId) });
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning($"PasswordResetAdmin.IssueLinkForRequest rejected: RequestId {requestId} not found");
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "REQUEST_NOT_FOUND",
                        Errors = new List<string> { "Password reset request was not found." }
                    });
                }

                DataRow row = dt.Rows[0];
                long targetCustomerId = Convert.ToInt64(row["customer_id"]);
                bool isTargetAdmin = Convert.ToBoolean(row["IsAdmin"]);
                string currentStatus = row["Status"]?.ToString() ?? "";

                // Hard rule: Reset links cannot be issued for administrative accounts
                if (isTargetAdmin)
                {
                    sw.Stop();
                    Logs.Warning($"IssueLink rejected: Attempt to issue reset link for Admin account ID {targetCustomerId} by Admin ID {adminId}");
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Success = false,
                        StatusCode = 403,
                        Message = "RESET_NOT_ALLOWED_FOR_ADMIN",
                        Errors = new List<string> { "Password reset links cannot be issued for administrative accounts." }
                    });
                }

                if (currentStatus != "PENDING" && currentStatus != "LINK_ISSUED" && currentStatus != "EXPIRED")
                {
                    sw.Stop();
                    Logs.Warning($"PasswordResetAdmin.IssueLinkForRequest rejected: Invalid status '{currentStatus}' for RequestId {requestId}");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_REQUEST_STATUS",
                        Errors = new List<string> { $"Cannot issue reset link for a request with status '{currentStatus}'." }
                    });
                }

                // Generate 32 random bytes, Base64Url encoded
                byte[] randomBytes = RandomNumberGenerator.GetBytes(32);
                string rawToken = Convert.ToBase64String(randomBytes)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('=');

                string tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
                int linkHours = _configuration.GetValue<int>("Security:PasswordResetLinkHours", 24);
                DateTime expiresUtc = DateTime.UtcNow.AddHours(linkHours);

                using var con = await _db.GetOpenConnectionAsync();
                using var tx = con.BeginTransaction(IsolationLevel.ReadCommitted);

                try
                {
                    // Invalidate other outstanding tokens for this customer
                    string invalidateOthers = @"
                        UPDATE drs_password_reset_request_mst
                        SET Status = 'EXPIRED', TokenHash = NULL, ModifiedDate = GETDATE()
                        WHERE customer_id = @CustomerId AND Status = 'LINK_ISSUED' AND ID <> @RequestId AND IsDeleted = 0";

                    using var invCmd = new SqlCommand(invalidateOthers, con, tx);
                    invCmd.Parameters.AddWithValue("@CustomerId", targetCustomerId);
                    invCmd.Parameters.AddWithValue("@RequestId", requestId);
                    await invCmd.ExecuteNonQueryAsync();

                    // Update the request row
                    string updateRow = @"
                        UPDATE drs_password_reset_request_mst
                        SET Status = 'LINK_ISSUED',
                            TokenHash = @TokenHash,
                            TokenExpiresUtc = @ExpiresUtc,
                            IssuedByAdminId = @AdminId,
                            IssuedUtc = GETUTCDATE(),
                            AdminNotes = @AdminNotes,
                            ModifiedDate = GETDATE()
                        WHERE ID = @RequestId";

                    using var updCmd = new SqlCommand(updateRow, con, tx);
                    updCmd.Parameters.AddWithValue("@TokenHash", tokenHash);
                    updCmd.Parameters.AddWithValue("@ExpiresUtc", expiresUtc);
                    updCmd.Parameters.AddWithValue("@AdminId", adminId > 0 ? (object)adminId : DBNull.Value);
                    updCmd.Parameters.AddWithValue("@AdminNotes", request.AdminNotes.Trim());
                    updCmd.Parameters.AddWithValue("@RequestId", requestId);
                    await updCmd.ExecuteNonQueryAsync();

                    await tx.CommitAsync();

                    // Audit: record admin ID and target customer ID only. Never log the raw token!
                    await _audit.InsertAuditAsync(
                        customerId: adminId > 0 ? adminId : targetCustomerId,
                        moduleName: "Auth",
                        action: "PasswordResetLinkIssued",
                        tableName: "drs_password_reset_request_mst",
                        entityId: requestId,
                        oldValue: null,
                        newValue: $"Link issued for customer ID {targetCustomerId}",
                        ipAddress: GetClientIpAddress(),
                        userAgent: GetUserAgent()
                    );

                    string frontendBaseUrl = _configuration["App:FrontendBaseUrl"] ?? "http://localhost:4200";
                    string resetUrl = $"{frontendBaseUrl.TrimEnd('/')}/reset-password?token={rawToken}";

                    sw.Stop();
                    Logs.Info($"PasswordResetLinkIssued for RequestId:{requestId} TargetCustomerId:{targetCustomerId}");

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PASSWORD_RESET_LINK_ISSUED",
                        Data = new IssueResetLinkResponseDto
                        {
                            ResetUrl = resetUrl,
                            ExpiresUtc = expiresUtc.ToString("o")
                        }
                    });
                }
                catch
                {
                    try { await tx.RollbackAsync(); } catch { }
                    throw;
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in IssueLinkForRequest API", ex);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "SERVER_ERROR",
                    Errors = new List<string> { "An internal server error occurred." }
                });
            }
        }
        #endregion

        #region Reject Request
        [HttpPost("requests/{encryptedRequestId}/reject")]
        public async Task<IActionResult> RejectRequest(
            [FromRoute] string encryptedRequestId,
            [FromBody] RejectResetRequestDto? request
        )
        {
            Logs.Info("PasswordResetAdmin.RejectRequest API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.AdminNotes))
                {
                    sw.Stop();
                    Logs.Warning("PasswordResetAdmin.RejectRequest rejected: Rejection notes are required");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "VALIDATION_FAILED",
                        Errors = new List<string> { "Rejection notes are required." }
                    });
                }

                if (!_encryption.TryDecrypt(encryptedRequestId, out long requestId))
                {
                    sw.Stop();
                    Logs.Warning($"PasswordResetAdmin.RejectRequest rejected: Failed to decrypt RequestId '{encryptedRequestId}'");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_REQUEST_ID",
                        Errors = new List<string> { "Invalid request identifier." }
                    });
                }

                long adminId = GetAuthenticatedUserId();

                string query = @"
                    SELECT r.ID, r.customer_id, r.Status, c.IsAdmin
                    FROM drs_password_reset_request_mst r
                    INNER JOIN drs_customer_mst c ON c.ID = r.customer_id
                    WHERE r.ID = @ID AND r.IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(query, new[] { new SqlParameter("@ID", requestId) });
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning($"PasswordResetAdmin.RejectRequest rejected: RequestId {requestId} not found");
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "REQUEST_NOT_FOUND",
                        Errors = new List<string> { "Password reset request was not found." }
                    });
                }

                long customerId = Convert.ToInt64(dt.Rows[0]["customer_id"]);

                _db.ExecuteNonQuery(@"
                    UPDATE drs_password_reset_request_mst
                    SET Status = 'REJECTED',
                        TokenHash = NULL,
                        AdminNotes = @AdminNotes,
                        ModifiedDate = GETDATE()
                    WHERE ID = @RequestId",
                    new[]
                    {
                        new SqlParameter("@AdminNotes", request.AdminNotes.Trim()),
                        new SqlParameter("@RequestId", requestId)
                    }
                );

                await _audit.InsertAuditAsync(
                    customerId: adminId > 0 ? adminId : customerId,
                    moduleName: "Auth",
                    action: "PasswordResetRequestRejected",
                    tableName: "drs_password_reset_request_mst",
                    entityId: requestId,
                    oldValue: null,
                    newValue: $"Rejected by Admin ID {adminId}. Reason: {request.AdminNotes.Trim()}",
                    ipAddress: GetClientIpAddress(),
                    userAgent: GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"PasswordResetRequestRejected for RequestId:{requestId}");

                return Ok(new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "PASSWORD_RESET_REQUEST_REJECTED",
                    Data = "Password reset request has been rejected."
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in RejectRequest API", ex);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "SERVER_ERROR",
                    Errors = new List<string> { "An internal server error occurred." }
                });
            }
        }
        #endregion

        #region Admin-Initiated Issue Link For Customer
        [HttpPost("customers/{encryptedCustomerId}/issue-link")]
        public async Task<IActionResult> IssueLinkForCustomer(
            [FromRoute] string encryptedCustomerId,
            [FromBody] IssueResetLinkRequestDto? request
        )
        {
            Logs.Info("PasswordResetAdmin.IssueLinkForCustomer API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.AdminNotes))
                {
                    sw.Stop();
                    Logs.Warning("PasswordResetAdmin.IssueLinkForCustomer rejected: Verification notes are required");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "VALIDATION_FAILED",
                        Errors = new List<string> { "Verification notes are required. Please document how identity was verified." }
                    });
                }

                if (!_encryption.TryDecrypt(encryptedCustomerId, out long targetCustomerId))
                {
                    sw.Stop();
                    Logs.Warning($"PasswordResetAdmin.IssueLinkForCustomer rejected: Failed to decrypt CustomerId '{encryptedCustomerId}'");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_CUSTOMER_ID",
                        Errors = new List<string> { "Invalid customer identifier." }
                    });
                }

                long adminId = GetAuthenticatedUserId();

                string custQuery = @"
                    SELECT ID, IsAdmin, IsActive, IsDeleted, Email
                    FROM drs_customer_mst
                    WHERE ID = @ID AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(custQuery, new[] { new SqlParameter("@ID", targetCustomerId) });
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning($"PasswordResetAdmin.IssueLinkForCustomer rejected: CustomerId {targetCustomerId} not found");
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "CUSTOMER_NOT_FOUND",
                        Errors = new List<string> { "Customer was not found." }
                    });
                }

                DataRow row = dt.Rows[0];
                bool isTargetAdmin = Convert.ToBoolean(row["IsAdmin"]);
                bool isActive = Convert.ToBoolean(row["IsActive"]);

                if (isTargetAdmin)
                {
                    sw.Stop();
                    Logs.Warning($"IssueLink rejected: Attempt to issue reset link for Admin account ID {targetCustomerId}");
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Success = false,
                        StatusCode = 403,
                        Message = "RESET_NOT_ALLOWED_FOR_ADMIN",
                        Errors = new List<string> { "Password reset links cannot be issued for administrative accounts." }
                    });
                }

                if (!isActive)
                {
                    sw.Stop();
                    Logs.Warning($"PasswordResetAdmin.IssueLinkForCustomer rejected: Account disabled for CustomerId {targetCustomerId}");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "ACCOUNT_DISABLED",
                        Errors = new List<string> { "Cannot issue reset link for a disabled customer account." }
                    });
                }

                // Generate 32 random bytes, Base64Url encoded
                byte[] randomBytes = RandomNumberGenerator.GetBytes(32);
                string rawToken = Convert.ToBase64String(randomBytes)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('=');

                string tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
                int linkHours = _configuration.GetValue<int>("Security:PasswordResetLinkHours", 24);
                DateTime expiresUtc = DateTime.UtcNow.AddHours(linkHours);

                using var con = await _db.GetOpenConnectionAsync();
                using var tx = con.BeginTransaction(IsolationLevel.ReadCommitted);

                try
                {
                    // Invalidate other outstanding tokens
                    string invalidateOthers = @"
                        UPDATE drs_password_reset_request_mst
                        SET Status = 'EXPIRED', TokenHash = NULL, ModifiedDate = GETDATE()
                        WHERE customer_id = @CustomerId AND Status = 'LINK_ISSUED' AND IsDeleted = 0";

                    using var invCmd = new SqlCommand(invalidateOthers, con, tx);
                    invCmd.Parameters.AddWithValue("@CustomerId", targetCustomerId);
                    await invCmd.ExecuteNonQueryAsync();

                    // Insert new ADMIN_INITIATED request row
                    string insertRow = @"
                        INSERT INTO drs_password_reset_request_mst
                        (customer_id, Source, Status, TokenHash, TokenExpiresUtc, IssuedByAdminId, IssuedUtc, AdminNotes, IsActive, IsDeleted, CreatedDate)
                        OUTPUT inserted.ID
                        VALUES
                        (@CustomerId, 'ADMIN_INITIATED', 'LINK_ISSUED', @TokenHash, @ExpiresUtc, @AdminId, GETUTCDATE(), @AdminNotes, 1, 0, GETDATE())";

                    using var insCmd = new SqlCommand(insertRow, con, tx);
                    insCmd.Parameters.AddWithValue("@CustomerId", targetCustomerId);
                    insCmd.Parameters.AddWithValue("@TokenHash", tokenHash);
                    insCmd.Parameters.AddWithValue("@ExpiresUtc", expiresUtc);
                    insCmd.Parameters.AddWithValue("@AdminId", adminId > 0 ? (object)adminId : DBNull.Value);
                    insCmd.Parameters.AddWithValue("@AdminNotes", request.AdminNotes.Trim());

                    object? newIdObj = await insCmd.ExecuteScalarAsync();
                    long newRequestId = Convert.ToInt64(newIdObj);

                    await tx.CommitAsync();

                    // Audit
                    await _audit.InsertAuditAsync(
                        customerId: adminId > 0 ? adminId : targetCustomerId,
                        moduleName: "Auth",
                        action: "PasswordResetLinkIssued",
                        tableName: "drs_password_reset_request_mst",
                        entityId: newRequestId,
                        oldValue: null,
                        newValue: $"Admin-initiated link issued for customer ID {targetCustomerId}",
                        ipAddress: GetClientIpAddress(),
                        userAgent: GetUserAgent()
                    );

                    string frontendBaseUrl = _configuration["App:FrontendBaseUrl"] ?? "http://localhost:4200";
                    string resetUrl = $"{frontendBaseUrl.TrimEnd('/')}/reset-password?token={rawToken}";

                    sw.Stop();
                    Logs.Info($"PasswordResetLinkIssued (Admin Initiated) for CustomerId:{targetCustomerId}");

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PASSWORD_RESET_LINK_ISSUED",
                        Data = new IssueResetLinkResponseDto
                        {
                            ResetUrl = resetUrl,
                            ExpiresUtc = expiresUtc.ToString("o")
                        }
                    });
                }
                catch
                {
                    try { await tx.RollbackAsync(); } catch { }
                    throw;
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in IssueLinkForCustomer API", ex);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "SERVER_ERROR",
                    Errors = new List<string> { "An internal server error occurred." }
                });
            }
        }
        #endregion
    }
}
