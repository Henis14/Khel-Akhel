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
    [EnableRateLimiting("ContactUsPolicy")]
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
    }
}
