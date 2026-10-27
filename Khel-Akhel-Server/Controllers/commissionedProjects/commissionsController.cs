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

namespace Khel_Akhel_Server.Controllers.commissionedProjects
{
    [ApiController]
    [Route("api/commissions")]
    public class commissionsController : ControllerBase
    {
        private readonly DbHelper _db;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService _audit;
        private readonly IWebHostEnvironment _env;
        private readonly ICaptchaService _captchaService;

        private const int MaxAllowedImages = 3;
        private const long MaxImageSizeInBytes = 5 * 1024 * 1024; // 5MB per file
        private const int MaxDescriptionWords = 500;
        private const int MaxDescriptionCharacters = 5000;

        private static readonly HashSet<string> AllowedStatuses = new(
            StringComparer.OrdinalIgnoreCase
        )
        {
            "Submitted",
            "Under Review",
            "Contacted",
            "In Progress",
            "Completed",
            "Rejected",
            "Cancelled",
        };

        public commissionsController(
            DbHelper db,
            IUrlEncryptionService encryption,
            IAuditService audit,
            IWebHostEnvironment env,
            ICaptchaService captchaService
        )
        {
            _db = db;
            _encryption = encryption;
            _audit = audit;
            _env = env;
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

        private static int CountWords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            char[] delimiters = new char[] { ' ', '\r', '\n', '\t' };
            return text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
        }
        #endregion

        #region Get Commission Categories API
        /// <summary>
        /// Public endpoint to fetch all active commission project categories for customer dropdown.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            Logs.Info("GetCommissionCategories API started");
            var sw = Stopwatch.StartNew();

            try
            {
                string query =
                    @"
                    SELECT ID, CategoryName, Description, DisplayOrder, IsActive
                    FROM drs_commission_category_mst WITH (NOLOCK)
                    WHERE IsActive = 1 AND IsDeleted = 0
                    ORDER BY DisplayOrder ASC, CategoryName ASC";

                DataTable dt = await _db.ExecuteQueryAsync(query);

                var categoryList = new List<CommissionCategoryResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    long catId = Convert.ToInt64(row["ID"]);
                    categoryList.Add(
                        new CommissionCategoryResponse
                        {
                            Id = catId,
                            EncryptedCategoryId = _encryption.Encrypt(catId),
                            CategoryName = row["CategoryName"].ToString() ?? string.Empty,
                            Description =
                                row["Description"] != DBNull.Value
                                    ? row["Description"].ToString()
                                    : null,
                            DisplayOrder = Convert.ToInt32(row["DisplayOrder"]),
                            IsActive = Convert.ToBoolean(row["IsActive"]),
                        }
                    );
                }

                sw.Stop();
                Logs.Info(
                    $"GetCommissionCategories completed | Count: {categoryList.Count} | ElapsedMs: {sw.ElapsedMilliseconds}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "COMMISSION_CATEGORIES_FETCHED",
                        Data = categoryList,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception in GetCommissionCategories API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while retrieving categories.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Create Commission Request API
        /// <summary>
        /// Public customer endpoint to submit a customized art/design project commission request with up to 3 sketches/images.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("public-form")]
        [RequestSizeLimit(16 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 16 * 1024 * 1024)]
        [HttpPost("create")]
        public async Task<IActionResult> CreateCommissionRequest(
            [FromForm] CommissionRequestCreateDto? request
        )
        {
            Logs.Info("CreateCommissionRequest API started");
            var sw = Stopwatch.StartNew();

            if (request == null)
            {
                sw.Stop();
                Logs.Warning("CreateCommissionRequest rejected | Reason: Null request body");
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
                    $"Honeypot triggered for CreateCommissionRequest from IP {GetClientIpAddress()}"
                );
                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "COMMISSION_REQUEST_SUBMITTED",
                        Data = new CommissionCreateResponse
                        {
                            EncryptedRequestId = string.Empty,
                            RequestNo = $"CP{DateTime.Now:yyyyMMdd}0000",
                            FullName = request.FullName,
                            Email = request.Email,
                            MobileNo = request.MobileNo,
                            CategoryName = "General",
                            ExpectedTimeline = request.ExpectedTimeline,
                            Status = "Submitted",
                            UploadedImagesCount = 0,
                            CreatedDate = DateTimeFormat.Format(DateTime.Now),
                        },
                    }
                );
            }

            // Captcha validation (mandatory for all commission requests)
            if (
                string.IsNullOrWhiteSpace(request.CaptchaId)
                || string.IsNullOrWhiteSpace(request.Captcha)
                || !_captchaService.ValidateCaptcha(request.CaptchaId, request.Captcha)
            )
            {
                sw.Stop();
                Logs.Warning(
                    $"CreateCommissionRequest rejected | Reason: Invalid or missing CAPTCHA from IP {GetClientIpAddress()}"
                );
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_CAPTCHA",
                        Errors = new List<string>
                        {
                            "Invalid or expired CAPTCHA. Please solve the security verification again.",
                        },
                    }
                );
            }

            // Normalization
            request.FullName = request.FullName?.Trim() ?? string.Empty;
            request.MobileNo = request.MobileNo?.Trim() ?? string.Empty;
            request.Email = request.Email?.Trim() ?? string.Empty;
            request.ExpectedTimeline = request.ExpectedTimeline?.Trim() ?? string.Empty;
            request.ProjectLocation = request.ProjectLocation?.Trim() ?? string.Empty;
            request.ProjectDescription = request.ProjectDescription?.Trim() ?? string.Empty;
            request.Keywords = request.Keywords?.Trim();
            request.OtherCategoryDescription = request.OtherCategoryDescription?.Trim();

            var validationErrors = new List<string>();

            // FullName validation
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                validationErrors.Add("Full name is required.");
            }
            else if (request.FullName.Length < 2 || request.FullName.Length > 150)
            {
                validationErrors.Add("Full name must be between 2 and 150 characters.");
            }

            // MobileNo validation
            if (string.IsNullOrWhiteSpace(request.MobileNo))
            {
                validationErrors.Add("Mobile number is required.");
            }
            else if (!ValidationHelper.IsValidMobile(request.MobileNo))
            {
                validationErrors.Add(
                    "Invalid mobile number. Please enter a valid 10-digit mobile number."
                );
            }

            // Email validation
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                validationErrors.Add("Email address is required.");
            }
            else if (!ValidationHelper.IsValidEmail(request.Email))
            {
                validationErrors.Add("Invalid email address format.");
            }

            // Category validation
            long categoryId = request.CategoryId;
            if (categoryId <= 0 && !string.IsNullOrWhiteSpace(request.EncryptedCategoryId))
            {
                _encryption.TryDecrypt(request.EncryptedCategoryId, out categoryId);
            }

            string categoryName = string.Empty;
            if (categoryId <= 0)
            {
                validationErrors.Add("Please select a project category.");
            }
            else
            {
                string checkCatSql =
                    @"
                    SELECT TOP 1 CategoryName 
                    FROM drs_commission_category_mst WITH (NOLOCK) 
                    WHERE ID = @CategoryId AND IsActive = 1 AND IsDeleted = 0";

                object? catNameObj = await _db.ExecuteScalarAsync(
                    checkCatSql,
                    new[] { new SqlParameter("@CategoryId", categoryId) }
                );
                if (catNameObj == null || catNameObj == DBNull.Value)
                {
                    validationErrors.Add(
                        "Selected project category is invalid, inactive, or not found."
                    );
                }
                else
                {
                    categoryName = catNameObj.ToString() ?? string.Empty;
                }
            }

            // Conditional validation for "Others" category
            if (string.Equals(categoryName, "Others", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(request.OtherCategoryDescription))
                {
                    validationErrors.Add(
                        "Please provide a description for the 'Others' project category."
                    );
                }
                else if (request.OtherCategoryDescription.Length > 255)
                {
                    validationErrors.Add(
                        "Other category description cannot exceed 255 characters."
                    );
                }
            }

            // ExpectedTimeline date validation
            DateTime parsedTimeline = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(request.ExpectedTimeline))
            {
                validationErrors.Add("Expected project timeline is required.");
            }
            else if (!DateTime.TryParse(request.ExpectedTimeline, out parsedTimeline))
            {
                validationErrors.Add(
                    "Expected timeline must be a valid date format (e.g. YYYY-MM-DD)."
                );
            }
            else if (parsedTimeline.Date < DateTime.Today)
            {
                validationErrors.Add("Expected timeline date cannot be in the past.");
            }

            // ProjectLocation validation
            if (string.IsNullOrWhiteSpace(request.ProjectLocation))
            {
                validationErrors.Add("Project location is required.");
            }
            else if (request.ProjectLocation.Length < 2 || request.ProjectLocation.Length > 255)
            {
                validationErrors.Add("Project location must be between 2 and 255 characters.");
            }

            // ProjectDescription word count & length validation
            int wordCount = CountWords(request.ProjectDescription);
            if (string.IsNullOrWhiteSpace(request.ProjectDescription))
            {
                validationErrors.Add("Project description is required.");
            }
            else if (wordCount > MaxDescriptionWords)
            {
                validationErrors.Add(
                    $"Project description exceeds maximum limit of {MaxDescriptionWords} words (currently {wordCount} words)."
                );
            }
            else if (request.ProjectDescription.Length > MaxDescriptionCharacters)
            {
                validationErrors.Add(
                    $"Project description cannot exceed {MaxDescriptionCharacters} characters."
                );
            }

            // Keywords validation
            if (!string.IsNullOrWhiteSpace(request.Keywords) && request.Keywords.Length > 500)
            {
                validationErrors.Add("Keywords cannot exceed 500 characters.");
            }

            // Images validation
            var uploadedFiles = request.Images ?? new List<IFormFile>();
            if (uploadedFiles.Count > MaxAllowedImages)
            {
                validationErrors.Add(
                    $"A maximum of {MaxAllowedImages} reference images or sketches are allowed."
                );
            }

            for (int i = 0; i < uploadedFiles.Count; i++)
            {
                var file = uploadedFiles[i];
                var (isValid, errorMessage) = await FileValidationHelper.IsValidImageAsync(
                    file,
                    MaxImageSizeInBytes
                );
                if (!isValid)
                {
                    validationErrors.Add($"Image #{i + 1} ('{file.FileName}'): {errorMessage}");
                }
            }

            if (validationErrors.Count > 0)
            {
                sw.Stop();
                Logs.Warning(
                    $"CreateCommissionRequest validation failed | Errors: {string.Join("; ", validationErrors)}"
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

            // Prepare Upload Directory
            string uploadsRoot = Path.Combine(
                _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads",
                "commissions"
            );
            if (!Directory.Exists(uploadsRoot))
            {
                Directory.CreateDirectory(uploadsRoot);
            }

            var createdPhysicalPaths = new List<string>();
            var savedImages = new List<CommissionImageDto>();

            using SqlConnection conn = await _db.GetOpenConnectionAsync();
            using SqlTransaction tran = conn.BeginTransaction();

            try
            {
                // Generate Unique Request Number via Sequence
                string seqSql = "SELECT NEXT VALUE FOR [dbo].[Seq_CommissionRequestNo];";
                using SqlCommand seqCmd = new SqlCommand(seqSql, conn, tran);
                long seqVal = Convert.ToInt64(await seqCmd.ExecuteScalarAsync());
                string requestNo = $"CP{DateTime.Now:yyyyMMdd}{seqVal:D4}";

                long customerId = GetAuthenticatedUserId();

                // Insert Commission Request
                string insertRequestSql =
                    @"
                    INSERT INTO drs_commission_request_mst (
                        RequestNo, FullName, MobileNo, Email, category_id, OtherCategoryDescription,
                        ExpectedTimeline, ProjectLocation, ProjectDescription, Keywords, Status,
                        AdminNotes, customer_id, IpAddress, UserAgent, IsActive, IsDeleted, CreatedDate
                    )
                    OUTPUT INSERTED.ID
                    VALUES (
                        @RequestNo, @FullName, @MobileNo, @Email, @CategoryId, @OtherCategoryDescription,
                        @ExpectedTimeline, @ProjectLocation, @ProjectDescription, @Keywords, 'Submitted',
                        NULL, @CustomerId, @IpAddress, @UserAgent, 1, 0, GETDATE()
                    );";

                using SqlCommand insertReqCmd = new SqlCommand(insertRequestSql, conn, tran);
                insertReqCmd.Parameters.AddWithValue("@RequestNo", requestNo);
                insertReqCmd.Parameters.AddWithValue("@FullName", request.FullName);
                insertReqCmd.Parameters.AddWithValue("@MobileNo", request.MobileNo);
                insertReqCmd.Parameters.AddWithValue("@Email", request.Email.ToLowerInvariant());
                insertReqCmd.Parameters.AddWithValue("@CategoryId", categoryId);
                insertReqCmd.Parameters.AddWithValue(
                    "@OtherCategoryDescription",
                    (object?)request.OtherCategoryDescription ?? DBNull.Value
                );
                insertReqCmd.Parameters.Add(
                    new SqlParameter("@ExpectedTimeline", SqlDbType.Date)
                    {
                        Value = parsedTimeline.Date,
                    }
                );
                insertReqCmd.Parameters.AddWithValue("@ProjectLocation", request.ProjectLocation);
                insertReqCmd.Parameters.AddWithValue(
                    "@ProjectDescription",
                    request.ProjectDescription
                );
                insertReqCmd.Parameters.AddWithValue(
                    "@Keywords",
                    (object?)request.Keywords ?? DBNull.Value
                );
                insertReqCmd.Parameters.AddWithValue(
                    "@CustomerId",
                    customerId > 0 ? (object)customerId : DBNull.Value
                );
                insertReqCmd.Parameters.AddWithValue(
                    "@IpAddress",
                    (object)GetClientIpAddress() ?? DBNull.Value
                );
                insertReqCmd.Parameters.AddWithValue(
                    "@UserAgent",
                    (object)GetUserAgent() ?? DBNull.Value
                );

                long newRequestId = Convert.ToInt64(await insertReqCmd.ExecuteScalarAsync());

                // Save Images & Image Metadata
                for (int i = 0; i < uploadedFiles.Count; i++)
                {
                    var file = uploadedFiles[i];
                    string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    string safeFileName = $"comm_{newRequestId}_{Guid.NewGuid():N}{extension}";
                    string physicalFilePath = Path.Combine(uploadsRoot, safeFileName);
                    string relativeDbPath = $"/uploads/commissions/{safeFileName}";

                    using (var fileStream = new FileStream(physicalFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    createdPhysicalPaths.Add(physicalFilePath);

                    string insertImgSql =
                        @"
                        INSERT INTO drs_commission_request_image_mst (
                            commission_request_id, OriginalFileName, StoredFileName, StoragePath,
                            ContentType, FileSize, DisplayOrder, IsActive, IsDeleted, CreatedDate
                        )
                        OUTPUT INSERTED.ID
                        VALUES (
                            @RequestId, @OriginalFileName, @StoredFileName, @StoragePath,
                            @ContentType, @FileSize, @DisplayOrder, 1, 0, GETDATE()
                        );";

                    using SqlCommand insertImgCmd = new SqlCommand(insertImgSql, conn, tran);
                    insertImgCmd.Parameters.AddWithValue("@RequestId", newRequestId);
                    insertImgCmd.Parameters.AddWithValue(
                        "@OriginalFileName",
                        Path.GetFileName(file.FileName)
                    );
                    insertImgCmd.Parameters.AddWithValue("@StoredFileName", safeFileName);
                    insertImgCmd.Parameters.AddWithValue("@StoragePath", relativeDbPath);
                    insertImgCmd.Parameters.AddWithValue(
                        "@ContentType",
                        file.ContentType ?? "image/jpeg"
                    );
                    insertImgCmd.Parameters.AddWithValue("@FileSize", file.Length);
                    insertImgCmd.Parameters.AddWithValue("@DisplayOrder", i + 1);

                    long newImageId = Convert.ToInt64(await insertImgCmd.ExecuteScalarAsync());

                    savedImages.Add(
                        new CommissionImageDto
                        {
                            EncryptedImageId = _encryption.Encrypt(newImageId),
                            OriginalFileName = Path.GetFileName(file.FileName),
                            StoredFileName = safeFileName,
                            ImageUrl = relativeDbPath,
                            FileSize = file.Length,
                            ContentType = file.ContentType ?? "image/jpeg",
                            DisplayOrder = i + 1,
                        }
                    );
                }

                await tran.CommitAsync();

                // Audit Log
                await _audit.InsertAuditAsync(
                    customerId,
                    "CommissionProject",
                    "SubmitRequest",
                    "drs_commission_request_mst",
                    newRequestId,
                    null,
                    $"New commission project request submitted. RequestNo: {requestNo}, Customer: {request.FullName}",
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info(
                    $"CreateCommissionRequest succeeded | RequestId: {newRequestId} | RequestNo: {requestNo} | Images: {savedImages.Count} | ElapsedMs: {sw.ElapsedMilliseconds}"
                );

                var responsePayload = new CommissionCreateResponse
                {
                    EncryptedRequestId = _encryption.Encrypt(newRequestId),
                    RequestNo = requestNo,
                    FullName = request.FullName,
                    Email = request.Email,
                    MobileNo = request.MobileNo,
                    CategoryName = categoryName,
                    ExpectedTimeline = parsedTimeline.ToString("yyyy-MM-dd"),
                    Status = "Submitted",
                    UploadedImagesCount = savedImages.Count,
                    CreatedDate = DateTimeFormat.Format(DateTime.Now),
                };

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "COMMISSION_REQUEST_SUBMITTED",
                        Data = responsePayload,
                    }
                );
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();

                // Compensating Cleanup of physical files created on disk
                foreach (var filePath in createdPhysicalPaths)
                {
                    try
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                    catch (Exception fileEx)
                    {
                        Logs.Error(
                            $"Failed to cleanup orphaned commission file on disk: {filePath}",
                            fileEx
                        );
                    }
                }

                sw.Stop();
                Logs.Error("Exception occurred during CreateCommissionRequest", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while processing your commission request.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Get Commission List API
        /// <summary>
        /// Admin endpoint to list, search, filter, and paginate commissioned project requests.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("list")]
        public async Task<IActionResult> GetCommissionList(
            [FromQuery] CommissionListFilterRequest? filter
        )
        {
            Logs.Info("GetCommissionList API started");
            var sw = Stopwatch.StartNew();

            try
            {
                var req = filter ?? new CommissionListFilterRequest();
                int pageIndex = req.PageIndex < 1 ? 1 : req.PageIndex;
                int pageSize = req.PageSize < 1 ? 10 : (req.PageSize > 100 ? 100 : req.PageSize);
                int offset = (pageIndex - 1) * pageSize;

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@Offset", offset),
                    new SqlParameter("@PageSize", pageSize),
                };

                string whereClause = "WHERE r.IsDeleted = 0";

                // Filter by Category
                long filterCategoryId = req.CategoryId ?? 0;
                if (filterCategoryId <= 0 && !string.IsNullOrWhiteSpace(req.EncryptedCategoryId))
                {
                    _encryption.TryDecrypt(req.EncryptedCategoryId, out filterCategoryId);
                }
                if (filterCategoryId > 0)
                {
                    whereClause += " AND r.category_id = @CategoryId";
                    parameters.Add(new SqlParameter("@CategoryId", filterCategoryId));
                }

                // Filter by Status
                if (
                    !string.IsNullOrWhiteSpace(req.Status)
                    && AllowedStatuses.Contains(req.Status.Trim())
                )
                {
                    whereClause += " AND r.Status = @Status";
                    parameters.Add(new SqlParameter("@Status", req.Status.Trim()));
                }

                // Filter by Date Range
                if (req.StartDate.HasValue)
                {
                    whereClause += " AND r.CreatedDate >= @StartDate";
                    parameters.Add(new SqlParameter("@StartDate", req.StartDate.Value.Date));
                }
                if (req.EndDate.HasValue)
                {
                    whereClause += " AND r.CreatedDate < @EndDate";
                    parameters.Add(new SqlParameter("@EndDate", req.EndDate.Value.Date.AddDays(1)));
                }

                // Search by Keyword / Name / Mobile / Email / RequestNo / Location
                if (!string.IsNullOrWhiteSpace(req.Search))
                {
                    string term = $"%{req.Search.Trim()}%";
                    whereClause +=
                        @" AND (
                        r.RequestNo LIKE @Search OR 
                        r.FullName LIKE @Search OR 
                        r.Email LIKE @Search OR 
                        r.MobileNo LIKE @Search OR 
                        r.ProjectLocation LIKE @Search OR 
                        r.Keywords LIKE @Search
                    )";
                    parameters.Add(new SqlParameter("@Search", term));
                }

                // Sort Order Whitelisting
                string orderByClause = (req.SortBy?.ToLowerInvariant()) switch
                {
                    "oldest" => "r.CreatedDate ASC",
                    "timeline_asc" => "r.ExpectedTimeline ASC",
                    "timeline_desc" => "r.ExpectedTimeline DESC",
                    "status" => "r.Status ASC, r.CreatedDate DESC",
                    _ => "r.CreatedDate DESC",
                };

                // Total Count Query
                string countQuery =
                    $"SELECT COUNT(1) FROM drs_commission_request_mst r WITH (NOLOCK) {whereClause};";
                object? countObj = await _db.ExecuteScalarAsync(countQuery, parameters.ToArray());
                int totalRecords =
                    (countObj != null && countObj != DBNull.Value) ? Convert.ToInt32(countObj) : 0;

                // Data Query with image count and first thumbnail
                string listQuery =
                    $@"
                    SELECT 
                        r.ID, r.RequestNo, r.FullName, r.MobileNo, r.Email, r.category_id,
                        c.CategoryName, r.OtherCategoryDescription, r.ExpectedTimeline,
                        r.ProjectLocation, r.Status, r.CreatedDate,
                        ISNULL((
                            SELECT COUNT(1) 
                            FROM drs_commission_request_image_mst img WITH (NOLOCK) 
                            WHERE img.commission_request_id = r.ID AND img.IsDeleted = 0
                        ), 0) AS ImageCount,
                        (
                            SELECT TOP 1 img.StoragePath 
                            FROM drs_commission_request_image_mst img WITH (NOLOCK) 
                            WHERE img.commission_request_id = r.ID AND img.IsDeleted = 0 
                            ORDER BY img.DisplayOrder ASC
                        ) AS ThumbnailUrl
                    FROM drs_commission_request_mst r WITH (NOLOCK)
                    LEFT JOIN drs_commission_category_mst c WITH (NOLOCK) ON c.ID = r.category_id AND c.IsDeleted = 0
                    {whereClause}
                    ORDER BY {orderByClause}
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                DataTable dt = await _db.ExecuteQueryAsync(listQuery, parameters.ToArray());

                var items = new List<CommissionRequestListItem>();
                foreach (DataRow row in dt.Rows)
                {
                    long reqId = Convert.ToInt64(row["ID"]);
                    long catId = Convert.ToInt64(row["category_id"]);
                    DateTime timeline = Convert.ToDateTime(row["ExpectedTimeline"]);

                    items.Add(
                        new CommissionRequestListItem
                        {
                            EncryptedRequestId = _encryption.Encrypt(reqId),
                            RequestNo = row["RequestNo"].ToString() ?? string.Empty,
                            FullName = row["FullName"].ToString() ?? string.Empty,
                            MobileNo = row["MobileNo"].ToString() ?? string.Empty,
                            Email = row["Email"].ToString() ?? string.Empty,
                            CategoryId = catId,
                            EncryptedCategoryId = _encryption.Encrypt(catId),
                            CategoryName =
                                row["CategoryName"] != DBNull.Value
                                    ? row["CategoryName"].ToString() ?? string.Empty
                                    : string.Empty,
                            OtherCategoryDescription =
                                row["OtherCategoryDescription"] != DBNull.Value
                                    ? row["OtherCategoryDescription"].ToString()
                                    : null,
                            ExpectedTimeline = timeline.ToString("yyyy-MM-dd"),
                            ProjectLocation = row["ProjectLocation"].ToString() ?? string.Empty,
                            Status = row["Status"].ToString() ?? string.Empty,
                            ImageCount = Convert.ToInt32(row["ImageCount"]),
                            ThumbnailUrl =
                                row["ThumbnailUrl"] != DBNull.Value
                                    ? row["ThumbnailUrl"].ToString()
                                    : null,
                            CreatedDate = DateTimeFormat.Format(
                                Convert.ToDateTime(row["CreatedDate"])
                            ),
                        }
                    );
                }

                var responsePayload = new CommissionListResponse
                {
                    Items = items,
                    TotalRecords = totalRecords,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                };

                sw.Stop();
                Logs.Info(
                    $"GetCommissionList completed | Count: {items.Count} | Total: {totalRecords} | ElapsedMs: {sw.ElapsedMilliseconds}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = responsePayload,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception in GetCommissionList API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while retrieving commission requests.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Get Commission Detail API
        /// <summary>
        /// Admin endpoint to fetch full details of a specific commission request including reference sketches/images.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("{encryptedId}")]
        public async Task<IActionResult> GetCommissionDetail([FromRoute] string encryptedId)
        {
            Logs.Info("GetCommissionDetail API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (
                    string.IsNullOrWhiteSpace(encryptedId)
                    || !_encryption.TryDecrypt(encryptedId, out long requestId)
                    || requestId <= 0
                )
                {
                    sw.Stop();
                    Logs.Warning(
                        $"GetCommissionDetail rejected | Invalid EncryptedId: {encryptedId}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string>
                            {
                                "Invalid or tampered commission request ID.",
                            },
                        }
                    );
                }

                string selectQuery =
                    @"
                    SELECT TOP 1
                        r.ID, r.RequestNo, r.FullName, r.MobileNo, r.Email, r.category_id,
                        c.CategoryName, r.OtherCategoryDescription, r.ExpectedTimeline,
                        r.ProjectLocation, r.ProjectDescription, r.Keywords, r.Status,
                        r.AdminNotes, r.CreatedDate, r.ModifiedDate
                    FROM drs_commission_request_mst r WITH (NOLOCK)
                    LEFT JOIN drs_commission_category_mst c WITH (NOLOCK) ON c.ID = r.category_id AND c.IsDeleted = 0
                    WHERE r.ID = @ID AND r.IsDeleted = 0;";

                DataTable dtReq = await _db.ExecuteQueryAsync(
                    selectQuery,
                    new[] { new SqlParameter("@ID", requestId) }
                );
                if (dtReq.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"GetCommissionDetail failed | Request not found. RequestId: {requestId}"
                    );
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "COMMISSION_REQUEST_NOT_FOUND",
                            Errors = new List<string>
                            {
                                "Commission request not found or has been removed.",
                            },
                        }
                    );
                }

                DataRow row = dtReq.Rows[0];
                long catId = Convert.ToInt64(row["category_id"]);
                string description = row["ProjectDescription"].ToString() ?? string.Empty;
                DateTime timeline = Convert.ToDateTime(row["ExpectedTimeline"]);

                // Fetch Attached Images
                string imagesQuery =
                    @"
                    SELECT ID, OriginalFileName, StoredFileName, StoragePath, ContentType, FileSize, DisplayOrder
                    FROM drs_commission_request_image_mst WITH (NOLOCK)
                    WHERE commission_request_id = @RequestId AND IsDeleted = 0
                    ORDER BY DisplayOrder ASC;";

                DataTable dtImages = await _db.ExecuteQueryAsync(
                    imagesQuery,
                    new[] { new SqlParameter("@RequestId", requestId) }
                );

                var imageList = new List<CommissionImageDto>();
                foreach (DataRow imgRow in dtImages.Rows)
                {
                    long imgId = Convert.ToInt64(imgRow["ID"]);
                    imageList.Add(
                        new CommissionImageDto
                        {
                            EncryptedImageId = _encryption.Encrypt(imgId),
                            OriginalFileName =
                                imgRow["OriginalFileName"].ToString() ?? string.Empty,
                            StoredFileName = imgRow["StoredFileName"].ToString() ?? string.Empty,
                            ImageUrl = imgRow["StoragePath"].ToString() ?? string.Empty,
                            ContentType = imgRow["ContentType"].ToString() ?? string.Empty,
                            FileSize = Convert.ToInt64(imgRow["FileSize"]),
                            DisplayOrder = Convert.ToInt32(imgRow["DisplayOrder"]),
                        }
                    );
                }

                var responsePayload = new CommissionRequestDetailResponse
                {
                    EncryptedRequestId = _encryption.Encrypt(requestId),
                    RequestNo = row["RequestNo"].ToString() ?? string.Empty,
                    FullName = row["FullName"].ToString() ?? string.Empty,
                    MobileNo = row["MobileNo"].ToString() ?? string.Empty,
                    Email = row["Email"].ToString() ?? string.Empty,
                    CategoryId = catId,
                    EncryptedCategoryId = _encryption.Encrypt(catId),
                    CategoryName =
                        row["CategoryName"] != DBNull.Value
                            ? row["CategoryName"].ToString() ?? string.Empty
                            : string.Empty,
                    OtherCategoryDescription =
                        row["OtherCategoryDescription"] != DBNull.Value
                            ? row["OtherCategoryDescription"].ToString()
                            : null,
                    ExpectedTimeline = timeline.ToString("yyyy-MM-dd"),
                    ProjectLocation = row["ProjectLocation"].ToString() ?? string.Empty,
                    ProjectDescription = description,
                    WordCount = CountWords(description),
                    Keywords = row["Keywords"] != DBNull.Value ? row["Keywords"].ToString() : null,
                    Status = row["Status"].ToString() ?? string.Empty,
                    AdminNotes =
                        row["AdminNotes"] != DBNull.Value ? row["AdminNotes"].ToString() : null,
                    Images = imageList,
                    CreatedDate = DateTimeFormat.Format(Convert.ToDateTime(row["CreatedDate"])),
                    ModifiedDate =
                        row["ModifiedDate"] != DBNull.Value
                            ? DateTimeFormat.Format(Convert.ToDateTime(row["ModifiedDate"]))
                            : null,
                };

                sw.Stop();
                Logs.Info(
                    $"GetCommissionDetail completed | RequestId: {requestId} | ElapsedMs: {sw.ElapsedMilliseconds}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = responsePayload,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception in GetCommissionDetail API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while retrieving request details.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Update Commission Status API
        /// <summary>
        /// Admin endpoint to update the review/communication status and notes for a commissioned project request.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("update-status")]
        public async Task<IActionResult> UpdateCommissionStatus(
            [FromBody] CommissionStatusUpdateRequest? request
        )
        {
            Logs.Info("UpdateCommissionStatus API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning("UpdateCommissionStatus rejected | Reason: Null request body");
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

                if (
                    string.IsNullOrWhiteSpace(request.EncryptedRequestId)
                    || !_encryption.TryDecrypt(request.EncryptedRequestId, out long requestId)
                    || requestId <= 0
                )
                {
                    sw.Stop();
                    Logs.Warning(
                        $"UpdateCommissionStatus rejected | Invalid encrypted request ID: {request.EncryptedRequestId}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string>
                            {
                                "Invalid or tampered commission request ID.",
                            },
                        }
                    );
                }

                request.Status = request.Status?.Trim() ?? string.Empty;
                request.AdminNotes = request.AdminNotes?.Trim();

                if (
                    string.IsNullOrWhiteSpace(request.Status)
                    || !AllowedStatuses.Contains(request.Status)
                )
                {
                    sw.Stop();
                    Logs.Warning(
                        $"UpdateCommissionStatus rejected | Invalid status '{request.Status}' | RequestId:{requestId}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_STATUS",
                            Errors = new List<string>
                            {
                                $"Invalid status '{request.Status}'. Permitted statuses: {string.Join(", ", AllowedStatuses)}",
                            },
                        }
                    );
                }

                if (
                    !string.IsNullOrWhiteSpace(request.AdminNotes)
                    && request.AdminNotes.Length > 2000
                )
                {
                    sw.Stop();
                    Logs.Warning(
                        $"UpdateCommissionStatus rejected | Admin notes exceed 2000 characters | RequestId:{requestId}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_ADMIN_NOTES",
                            Errors = new List<string>
                            {
                                "Admin notes cannot exceed 2000 characters.",
                            },
                        }
                    );
                }

                // Verify Request Exists
                string checkSql =
                    @"
                    SELECT TOP 1 ID, RequestNo, Status, AdminNotes 
                    FROM drs_commission_request_mst WITH (NOLOCK) 
                    WHERE ID = @ID AND IsDeleted = 0;";

                DataTable dt = await _db.ExecuteQueryAsync(
                    checkSql,
                    new[] { new SqlParameter("@ID", requestId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"UpdateCommissionStatus rejected | Commission request not found | RequestId:{requestId}"
                    );
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "COMMISSION_REQUEST_NOT_FOUND",
                            Errors = new List<string>
                            {
                                "Commission request not found or has been deleted.",
                            },
                        }
                    );
                }

                DataRow existingRow = dt.Rows[0];
                string oldStatus = existingRow["Status"].ToString() ?? string.Empty;
                string? oldNotes =
                    existingRow["AdminNotes"] != DBNull.Value
                        ? existingRow["AdminNotes"].ToString()
                        : null;
                string requestNo = existingRow["RequestNo"].ToString() ?? string.Empty;

                // Update Database
                string updateSql =
                    @"
                    UPDATE drs_commission_request_mst
                    SET Status = @Status,
                        AdminNotes = @AdminNotes,
                        ModifiedDate = GETDATE()
                    WHERE ID = @ID AND IsDeleted = 0;";

                _db.ExecuteNonQuery(
                    updateSql,
                    new[]
                    {
                        new SqlParameter("@Status", request.Status),
                        new SqlParameter(
                            "@AdminNotes",
                            (object?)request.AdminNotes ?? DBNull.Value
                        ),
                        new SqlParameter("@ID", requestId),
                    }
                );

                // Audit Log
                long adminUserId = GetAuthenticatedUserId();
                await _audit.InsertAuditAsync(
                    adminUserId,
                    "CommissionProject",
                    "UpdateStatus",
                    "drs_commission_request_mst",
                    requestId,
                    System.Text.Json.JsonSerializer.Serialize(
                        new { Status = oldStatus, AdminNotes = oldNotes }
                    ),
                    System.Text.Json.JsonSerializer.Serialize(
                        new { Status = request.Status, AdminNotes = request.AdminNotes }
                    ),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info(
                    $"UpdateCommissionStatus completed | RequestNo: {requestNo} | Status: {oldStatus} -> {request.Status} | ElapsedMs: {sw.ElapsedMilliseconds}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "COMMISSION_STATUS_UPDATED",
                        Data = new
                        {
                            EncryptedRequestId = request.EncryptedRequestId,
                            RequestNo = requestNo,
                            Status = request.Status,
                            AdminNotes = request.AdminNotes,
                            ModifiedDate = DateTimeFormat.Format(DateTime.Now),
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception in UpdateCommissionStatus API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while updating status.",
                        },
                    }
                );
            }
        }
        #endregion
    }
}
