using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.BL.Services;
using Khel_Akhel_Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using static Khel_Akhel_Server.Models.CommonResponse;

namespace Khel_Akhel_Server.Controllers.Projects
{
    [InvalidatesPublicCache(PublicCacheRegions.Projects)]
    [ApiController]
    [Route("api/projects")]
    public class ProjectsController : ControllerBase
    {
        private readonly DbHelper _db;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService _audit;
        private readonly IWebHostEnvironment _env;

        private const int MaxAllowedImages = 15;
        private const long MaxImageSizeInBytes = 5 * 1024 * 1024; // 5MB per file

        private static readonly HashSet<string> AllowedLayouts = new(
            StringComparer.OrdinalIgnoreCase
        )
        {
            "grid-3",
            "grid-2",
            "editorial",
            "featured",
            "full-width",
            "masonry",
            "asymmetric",
            "grid-4",
            "bento",
            "split",
            "staggered",
            "carousel",
        };

        public ProjectsController(
            DbHelper db,
            IUrlEncryptionService encryption,
            IAuditService audit,
            IWebHostEnvironment env
        )
        {
            _db = db;
            _encryption = encryption;
            _audit = audit;
            _env = env;
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

        private static bool IsValidUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true;
            return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uriResult)
                && (
                    uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps
                );
        }

        private string GetUploadsDirectory()
        {
            string uploadsRoot = Path.Combine(
                _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads",
                "projects"
            );
            if (!Directory.Exists(uploadsRoot))
            {
                Directory.CreateDirectory(uploadsRoot);
            }
            return uploadsRoot;
        }

        private ProjectImageDto MapProjectImage(DataRow row)
        {
            long imageId = Convert.ToInt64(row["ID"]);
            long projectId = Convert.ToInt64(row["project_id"]);
            string path = row["ImagePath"]?.ToString() ?? string.Empty;

            return new ProjectImageDto
            {
                EncryptedImageId = _encryption.Encrypt(imageId),
                EncryptedProjectId = _encryption.Encrypt(projectId),
                OriginalFileName = row["OriginalFileName"]?.ToString() ?? string.Empty,
                StoredFileName = row["StoredFileName"]?.ToString() ?? string.Empty,
                ImageUrl = path,
                ImagePath = path,
                FileSize = row["FileSize"] != DBNull.Value ? Convert.ToInt64(row["FileSize"]) : 0,
                ContentType = row["ContentType"]?.ToString() ?? string.Empty,
                DisplayOrder =
                    row["DisplayOrder"] != DBNull.Value ? Convert.ToInt32(row["DisplayOrder"]) : 0,
                IsActive = Convert.ToBoolean(row["IsActive"]),
                CreatedDate = DateTimeFormat.Format(row["CreatedDate"] as DateTime?),
            };
        }

        private ProjectDetailResponse MapProjectDetail(DataRow row, List<ProjectImageDto> images)
        {
            long projectId = Convert.ToInt64(row["ID"]);

            return new ProjectDetailResponse
            {
                EncryptedProjectId = _encryption.Encrypt(projectId),
                Heading = row["Heading"]?.ToString() ?? string.Empty,
                Subheading =
                    row["Subheading"] == DBNull.Value ? null : row["Subheading"]?.ToString(),
                ContentType =
                    row["ContentType"] == DBNull.Value ? null : row["ContentType"]?.ToString(),
                ProjectOrEventTitle =
                    row["ProjectOrEventTitle"] == DBNull.Value
                        ? null
                        : row["ProjectOrEventTitle"]?.ToString(),
                DisplayYear =
                    row["DisplayYear"] == DBNull.Value ? null : row["DisplayYear"]?.ToString(),
                ShortDescription =
                    row["ShortDescription"] == DBNull.Value
                        ? null
                        : row["ShortDescription"]?.ToString(),
                DetailedDescription =
                    row["DetailedDescription"] == DBNull.Value
                        ? null
                        : row["DetailedDescription"]?.ToString(),
                Credits = row["Credits"] == DBNull.Value ? null : row["Credits"]?.ToString(),
                Location = row["Location"] == DBNull.Value ? null : row["Location"]?.ToString(),
                ExternalUrl =
                    row["ExternalUrl"] == DBNull.Value ? null : row["ExternalUrl"]?.ToString(),
                DisplayOrder =
                    row["DisplayOrder"] != DBNull.Value ? Convert.ToInt32(row["DisplayOrder"]) : 0,
                LayoutType =
                    row.Table.Columns.Contains("LayoutType")
                    && row["LayoutType"] != DBNull.Value
                    && !string.IsNullOrWhiteSpace(row["LayoutType"]?.ToString())
                        ? row["LayoutType"].ToString()!
                        : "grid-3",
                IsPublished = Convert.ToBoolean(row["IsPublished"]),
                IsActive = Convert.ToBoolean(row["IsActive"]),
                ImageCount = images.Count,
                Images = images,
                CreatedDate = DateTimeFormat.Format(row["CreatedDate"] as DateTime?),
                ModifiedDate = DateTimeFormat.Format(row["ModifiedDate"] as DateTime?),
            };
        }

        private ProjectPublicResponse MapProjectPublic(DataRow row, List<ProjectImageDto> images)
        {
            long projectId = Convert.ToInt64(row["ID"]);

            return new ProjectPublicResponse
            {
                EncryptedProjectId = _encryption.Encrypt(projectId),
                Heading = row["Heading"]?.ToString() ?? string.Empty,
                Subheading =
                    row["Subheading"] == DBNull.Value ? null : row["Subheading"]?.ToString(),
                ContentType =
                    row["ContentType"] == DBNull.Value ? null : row["ContentType"]?.ToString(),
                ProjectOrEventTitle =
                    row["ProjectOrEventTitle"] == DBNull.Value
                        ? null
                        : row["ProjectOrEventTitle"]?.ToString(),
                DisplayYear =
                    row["DisplayYear"] == DBNull.Value ? null : row["DisplayYear"]?.ToString(),
                ShortDescription =
                    row["ShortDescription"] == DBNull.Value
                        ? null
                        : row["ShortDescription"]?.ToString(),
                DetailedDescription =
                    row["DetailedDescription"] == DBNull.Value
                        ? null
                        : row["DetailedDescription"]?.ToString(),
                Credits = row["Credits"] == DBNull.Value ? null : row["Credits"]?.ToString(),
                Location = row["Location"] == DBNull.Value ? null : row["Location"]?.ToString(),
                ExternalUrl =
                    row["ExternalUrl"] == DBNull.Value ? null : row["ExternalUrl"]?.ToString(),
                DisplayOrder =
                    row["DisplayOrder"] != DBNull.Value ? Convert.ToInt32(row["DisplayOrder"]) : 0,
                LayoutType =
                    row.Table.Columns.Contains("LayoutType")
                    && row["LayoutType"] != DBNull.Value
                    && !string.IsNullOrWhiteSpace(row["LayoutType"]?.ToString())
                        ? row["LayoutType"].ToString()!
                        : "grid-3",
                Images = images,
            };
        }

        private async Task<List<ProjectImageDto>> GetImagesForProjectAsync(
            long projectId,
            bool activeOnly = true
        )
        {
            string activeCondition = activeOnly ? " AND IsActive = 1" : string.Empty;
            string query =
                $@"
                SELECT ID, project_id, OriginalFileName, StoredFileName, ImagePath, ContentType, FileSize, DisplayOrder, IsActive, CreatedDate
                FROM drs_project_image_mst WITH (NOLOCK)
                WHERE project_id = @ProjectId AND IsDeleted = 0{activeCondition}
                ORDER BY DisplayOrder ASC, ID ASC;";

            DataTable dt = await _db.ExecuteQueryAsync(
                query,
                new[] { new SqlParameter("@ProjectId", projectId) }
            );
            var list = new List<ProjectImageDto>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(MapProjectImage(row));
            }
            return list;
        }

        private async Task<Dictionary<long, List<ProjectImageDto>>> GetImagesForProjectBatchAsync(
            IEnumerable<long> projectIds,
            bool activeOnly = true
        )
        {
            var dict = new Dictionary<long, List<ProjectImageDto>>();
            var idsList = projectIds.Distinct().ToList();
            if (idsList.Count == 0)
                return dict;

            foreach (var id in idsList)
            {
                dict[id] = new List<ProjectImageDto>();
            }

            string idParams = string.Join(",", idsList);
            string activeCondition = activeOnly ? " AND IsActive = 1" : string.Empty;
            string query =
                $@"
                SELECT ID, project_id, OriginalFileName, StoredFileName, ImagePath, ContentType, FileSize, DisplayOrder, IsActive, CreatedDate
                FROM drs_project_image_mst WITH (NOLOCK)
                WHERE project_id IN ({idParams}) AND IsDeleted = 0{activeCondition}
                ORDER BY DisplayOrder ASC, ID ASC;";

            DataTable dt = await _db.ExecuteQueryAsync(query);
            foreach (DataRow row in dt.Rows)
            {
                long projectId = Convert.ToInt64(row["project_id"]);
                if (dict.TryGetValue(projectId, out var imgList))
                {
                    imgList.Add(MapProjectImage(row));
                }
            }

            return dict;
        }
        #endregion

        #region Create Project API
        /// <summary>
        /// Admin endpoint to create a new dynamic Project section/content entry, optionally including up to 15 images.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] ProjectCreateRequest? request)
        {
            Logs.Info("Project Create API started");
            var sw = Stopwatch.StartNew();

            if (request == null)
            {
                sw.Stop();
                Logs.Warning("Project Create rejected | Reason: Null request body");
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
            request.Heading = request.Heading?.Trim() ?? string.Empty;
            request.Subheading = string.IsNullOrWhiteSpace(request.Subheading)
                ? null
                : request.Subheading.Trim();
            request.ContentType = string.IsNullOrWhiteSpace(request.ContentType)
                ? null
                : request.ContentType.Trim();
            request.ProjectOrEventTitle = string.IsNullOrWhiteSpace(request.ProjectOrEventTitle)
                ? null
                : request.ProjectOrEventTitle.Trim();
            request.DisplayYear = string.IsNullOrWhiteSpace(request.DisplayYear)
                ? null
                : request.DisplayYear.Trim();
            request.ShortDescription = string.IsNullOrWhiteSpace(request.ShortDescription)
                ? null
                : request.ShortDescription.Trim();
            request.DetailedDescription = string.IsNullOrWhiteSpace(request.DetailedDescription)
                ? null
                : request.DetailedDescription.Trim();
            request.Credits = string.IsNullOrWhiteSpace(request.Credits)
                ? null
                : request.Credits.Trim();
            request.Location = string.IsNullOrWhiteSpace(request.Location)
                ? null
                : request.Location.Trim();
            request.ExternalUrl = string.IsNullOrWhiteSpace(request.ExternalUrl)
                ? null
                : request.ExternalUrl.Trim();

            var validationErrors = new List<string>();

            // Field validations
            if (string.IsNullOrWhiteSpace(request.Heading))
            {
                validationErrors.Add("Heading is required.");
            }
            else if (request.Heading.Length < 2 || request.Heading.Length > 200)
            {
                validationErrors.Add("Heading must be between 2 and 200 characters.");
            }

            if (request.Subheading != null && request.Subheading.Length > 250)
            {
                validationErrors.Add("Subheading cannot exceed 250 characters.");
            }

            if (request.ContentType != null && request.ContentType.Length > 50)
            {
                validationErrors.Add("Content type cannot exceed 50 characters.");
            }

            if (request.ProjectOrEventTitle != null && request.ProjectOrEventTitle.Length > 200)
            {
                validationErrors.Add("Project or event title cannot exceed 200 characters.");
            }

            if (request.DisplayYear != null && request.DisplayYear.Length > 50)
            {
                validationErrors.Add("Display year cannot exceed 50 characters.");
            }

            if (request.ShortDescription != null && request.ShortDescription.Length > 1000)
            {
                validationErrors.Add("Short description cannot exceed 1000 characters.");
            }

            if (request.Credits != null && request.Credits.Length > 1000)
            {
                validationErrors.Add("Credits cannot exceed 1000 characters.");
            }

            if (request.Location != null && request.Location.Length > 200)
            {
                validationErrors.Add("Location cannot exceed 200 characters.");
            }

            if (request.ExternalUrl != null)
            {
                if (request.ExternalUrl.Length > 500)
                {
                    validationErrors.Add("External URL cannot exceed 500 characters.");
                }
                else if (!IsValidUrl(request.ExternalUrl))
                {
                    validationErrors.Add("External URL must be a valid HTTP or HTTPS URL.");
                }
            }

            if (request.DisplayOrder < 0)
            {
                validationErrors.Add("Display order cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(request.LayoutType))
            {
                request.LayoutType = "grid-3";
            }
            else
            {
                request.LayoutType = request.LayoutType.Trim().ToLowerInvariant();
                if (!AllowedLayouts.Contains(request.LayoutType))
                {
                    validationErrors.Add(
                        $"Invalid layout type '{request.LayoutType}'. Allowed values: {string.Join(", ", AllowedLayouts)}"
                    );
                }
            }

            // Image validations (Max 15 images limit)
            var uploadedFiles = request.Images ?? new List<IFormFile>();
            if (uploadedFiles.Count > MaxAllowedImages)
            {
                validationErrors.Add(
                    $"A maximum of {MaxAllowedImages} images are allowed per Project content section."
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
                    $"Project Create validation failed | Errors: {string.Join("; ", validationErrors)}"
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

            string uploadsDir = GetUploadsDirectory();
            var createdPhysicalPaths = new List<string>();

            using SqlConnection conn = await _db.GetOpenConnectionAsync();
            using SqlTransaction tran = conn.BeginTransaction();

            try
            {
                long adminUserId = GetAuthenticatedUserId();

                const string insertSql =
                    @"
                    INSERT INTO drs_project_mst (
                        Heading, Subheading, ContentType, ProjectOrEventTitle, DisplayYear,
                        ShortDescription, DetailedDescription, Credits, Location, ExternalUrl,
                        DisplayOrder, LayoutType, IsPublished, customer_id, IsActive, IsDeleted, CreatedDate
                    )
                    OUTPUT INSERTED.ID
                    VALUES (
                        @Heading, @Subheading, @ContentType, @ProjectOrEventTitle, @DisplayYear,
                        @ShortDescription, @DetailedDescription, @Credits, @Location, @ExternalUrl,
                        @DisplayOrder, @LayoutType, @IsPublished, @CustomerId, @IsActive, 0, GETDATE()
                    );";

                using SqlCommand insertCmd = new SqlCommand(insertSql, conn, tran);
                insertCmd.Parameters.AddWithValue("@Heading", request.Heading);
                insertCmd.Parameters.AddWithValue(
                    "@Subheading",
                    (object?)request.Subheading ?? DBNull.Value
                );
                insertCmd.Parameters.AddWithValue(
                    "@ContentType",
                    (object?)request.ContentType ?? DBNull.Value
                );
                insertCmd.Parameters.AddWithValue(
                    "@ProjectOrEventTitle",
                    (object?)request.ProjectOrEventTitle ?? DBNull.Value
                );
                insertCmd.Parameters.AddWithValue(
                    "@DisplayYear",
                    (object?)request.DisplayYear ?? DBNull.Value
                );
                insertCmd.Parameters.AddWithValue(
                    "@ShortDescription",
                    (object?)request.ShortDescription ?? DBNull.Value
                );
                insertCmd.Parameters.AddWithValue(
                    "@DetailedDescription",
                    (object?)request.DetailedDescription ?? DBNull.Value
                );
                insertCmd.Parameters.AddWithValue(
                    "@Credits",
                    (object?)request.Credits ?? DBNull.Value
                );
                insertCmd.Parameters.AddWithValue(
                    "@Location",
                    (object?)request.Location ?? DBNull.Value
                );
                insertCmd.Parameters.AddWithValue(
                    "@ExternalUrl",
                    (object?)request.ExternalUrl ?? DBNull.Value
                );
                insertCmd.Parameters.AddWithValue("@DisplayOrder", request.DisplayOrder);
                insertCmd.Parameters.AddWithValue("@LayoutType", request.LayoutType ?? "grid-3");
                insertCmd.Parameters.AddWithValue("@IsPublished", request.IsPublished);
                insertCmd.Parameters.AddWithValue(
                    "@CustomerId",
                    adminUserId > 0 ? (object)adminUserId : DBNull.Value
                );
                insertCmd.Parameters.AddWithValue("@IsActive", request.IsActive);

                long newprojectId = Convert.ToInt64(await insertCmd.ExecuteScalarAsync());

                // Save uploaded images
                var savedImages = new List<ProjectImageDto>();
                for (int i = 0; i < uploadedFiles.Count; i++)
                {
                    var file = uploadedFiles[i];
                    string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    string safeFileName = $"{newprojectId}_{Guid.NewGuid()}{extension}";
                    string physicalPath = Path.Combine(uploadsDir, safeFileName);
                    string relativePath = $"/uploads/projects/{safeFileName}";

                    using (var stream = new FileStream(physicalPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    createdPhysicalPaths.Add(physicalPath);

                    const string insertImgSql =
                        @"
                        INSERT INTO drs_project_image_mst (
                            project_id, OriginalFileName, StoredFileName, ImagePath,
                            ContentType, FileSize, DisplayOrder, IsActive, IsDeleted, CreatedDate
                        )
                        OUTPUT INSERTED.ID
                        VALUES (
                            @ProjectId, @OriginalFileName, @StoredFileName, @ImagePath,
                            @ContentType, @FileSize, @DisplayOrder, 1, 0, GETDATE()
                        );";

                    using SqlCommand insertImgCmd = new SqlCommand(insertImgSql, conn, tran);
                    insertImgCmd.Parameters.AddWithValue("@ProjectId", newprojectId);
                    insertImgCmd.Parameters.AddWithValue("@OriginalFileName", file.FileName);
                    insertImgCmd.Parameters.AddWithValue("@StoredFileName", safeFileName);
                    insertImgCmd.Parameters.AddWithValue("@ImagePath", relativePath);
                    insertImgCmd.Parameters.AddWithValue("@ContentType", file.ContentType);
                    insertImgCmd.Parameters.AddWithValue("@FileSize", file.Length);
                    insertImgCmd.Parameters.AddWithValue("@DisplayOrder", i + 1);

                    long newImageId = Convert.ToInt64(await insertImgCmd.ExecuteScalarAsync());

                    savedImages.Add(
                        new ProjectImageDto
                        {
                            EncryptedImageId = _encryption.Encrypt(newImageId),
                            EncryptedProjectId = _encryption.Encrypt(newprojectId),
                            OriginalFileName = file.FileName,
                            StoredFileName = safeFileName,
                            ImageUrl = relativePath,
                            ImagePath = relativePath,
                            FileSize = file.Length,
                            ContentType = file.ContentType,
                            DisplayOrder = i + 1,
                            IsActive = true,
                            CreatedDate = DateTimeFormat.Format(DateTime.Now),
                        }
                    );
                }

                await tran.CommitAsync();

                await _audit.InsertAuditAsync(
                    adminUserId,
                    "Projects",
                    "Create",
                    "drs_project_mst",
                    newprojectId,
                    null,
                    JsonSerializer.Serialize(
                        new
                        {
                            request.Heading,
                            request.ContentType,
                            LayoutType = request.LayoutType ?? "grid-3",
                            ImagesCount = savedImages.Count,
                        }
                    ),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info(
                    $"Project Create completed successfully | projectId: {newprojectId} | Elapsed: {sw.ElapsedMilliseconds}ms"
                );

                var responseData = new ProjectDetailResponse
                {
                    EncryptedProjectId = _encryption.Encrypt(newprojectId),
                    Heading = request.Heading,
                    Subheading = request.Subheading,
                    ContentType = request.ContentType,
                    ProjectOrEventTitle = request.ProjectOrEventTitle,
                    DisplayYear = request.DisplayYear,
                    ShortDescription = request.ShortDescription,
                    DetailedDescription = request.DetailedDescription,
                    Credits = request.Credits,
                    Location = request.Location,
                    ExternalUrl = request.ExternalUrl,
                    DisplayOrder = request.DisplayOrder,
                    LayoutType = request.LayoutType ?? "grid-3",
                    IsPublished = request.IsPublished,
                    IsActive = request.IsActive,
                    ImageCount = savedImages.Count,
                    Images = savedImages,
                    CreatedDate = DateTimeFormat.Format(DateTime.Now),
                    ModifiedDate = null,
                };

                return StatusCode(
                    201,
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 201,
                        Message = "PROJECT_CREATED",
                        Data = responseData,
                    }
                );
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();

                // Clean up orphaned physical files
                foreach (var path in createdPhysicalPaths)
                {
                    try
                    {
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);
                    }
                    catch (Exception cleanEx)
                    {
                        Logs.Error(
                            $"Failed to clean up physical file '{path}' after rollback",
                            cleanEx
                        );
                    }
                }

                sw.Stop();
                Logs.Error("Project Create API failed with exception", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while creating Project content.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Update Project API
        /// <summary>
        /// Admin endpoint to update an existing Project content entry, manage images (add new, remove existing, reorder), and enforce 15-image limit.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("update")]
        [HttpPatch("update")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update([FromForm] ProjectUpdateRequest? request)
        {
            Logs.Info("Project Update API started");
            var sw = Stopwatch.StartNew();

            if (request == null)
            {
                sw.Stop();
                Logs.Warning("Project Update rejected | Reason: Null request body");
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
                !_encryption.TryDecrypt(request.EncryptedProjectId, out long projectId)
                || projectId <= 0
            )
            {
                sw.Stop();
                Logs.Warning(
                    $"Project Update rejected | Reason: Invalid encryptedProjectId: {request.EncryptedProjectId}"
                );
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_PROJECT_ID",
                        Errors = new List<string> { "Invalid or tampered Project ID." },
                    }
                );
            }

            // Normalization
            request.Heading = request.Heading?.Trim() ?? string.Empty;
            request.Subheading = string.IsNullOrWhiteSpace(request.Subheading)
                ? null
                : request.Subheading.Trim();
            request.ContentType = string.IsNullOrWhiteSpace(request.ContentType)
                ? null
                : request.ContentType.Trim();
            request.ProjectOrEventTitle = string.IsNullOrWhiteSpace(request.ProjectOrEventTitle)
                ? null
                : request.ProjectOrEventTitle.Trim();
            request.DisplayYear = string.IsNullOrWhiteSpace(request.DisplayYear)
                ? null
                : request.DisplayYear.Trim();
            request.ShortDescription = string.IsNullOrWhiteSpace(request.ShortDescription)
                ? null
                : request.ShortDescription.Trim();
            request.DetailedDescription = string.IsNullOrWhiteSpace(request.DetailedDescription)
                ? null
                : request.DetailedDescription.Trim();
            request.Credits = string.IsNullOrWhiteSpace(request.Credits)
                ? null
                : request.Credits.Trim();
            request.Location = string.IsNullOrWhiteSpace(request.Location)
                ? null
                : request.Location.Trim();
            request.ExternalUrl = string.IsNullOrWhiteSpace(request.ExternalUrl)
                ? null
                : request.ExternalUrl.Trim();

            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Heading))
            {
                validationErrors.Add("Heading is required.");
            }
            else if (request.Heading.Length < 2 || request.Heading.Length > 200)
            {
                validationErrors.Add("Heading must be between 2 and 200 characters.");
            }

            if (request.Subheading != null && request.Subheading.Length > 250)
            {
                validationErrors.Add("Subheading cannot exceed 250 characters.");
            }

            if (request.ContentType != null && request.ContentType.Length > 50)
            {
                validationErrors.Add("Content type cannot exceed 50 characters.");
            }

            if (request.ProjectOrEventTitle != null && request.ProjectOrEventTitle.Length > 200)
            {
                validationErrors.Add("Project or event title cannot exceed 200 characters.");
            }

            if (request.DisplayYear != null && request.DisplayYear.Length > 50)
            {
                validationErrors.Add("Display year cannot exceed 50 characters.");
            }

            if (request.ShortDescription != null && request.ShortDescription.Length > 1000)
            {
                validationErrors.Add("Short description cannot exceed 1000 characters.");
            }

            if (request.Credits != null && request.Credits.Length > 1000)
            {
                validationErrors.Add("Credits cannot exceed 1000 characters.");
            }

            if (request.Location != null && request.Location.Length > 200)
            {
                validationErrors.Add("Location cannot exceed 200 characters.");
            }

            if (request.ExternalUrl != null)
            {
                if (request.ExternalUrl.Length > 500)
                {
                    validationErrors.Add("External URL cannot exceed 500 characters.");
                }
                else if (!IsValidUrl(request.ExternalUrl))
                {
                    validationErrors.Add("External URL must be a valid HTTP or HTTPS URL.");
                }
            }

            if (request.DisplayOrder < 0)
            {
                validationErrors.Add("Display order cannot be negative.");
            }

            // Verify existence of record
            const string checkSql =
                "SELECT * FROM drs_project_mst WITH (NOLOCK) WHERE ID = @ID AND IsDeleted = 0;";
            DataTable existingDt = await _db.ExecuteQueryAsync(
                checkSql,
                new[] { new SqlParameter("@ID", projectId) }
            );
            if (existingDt.Rows.Count == 0)
            {
                sw.Stop();
                Logs.Warning($"Project Update failed | Record not found. projectId: {projectId}");
                return NotFound(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "PROJECT_NOT_FOUND",
                        Errors = new List<string>
                        {
                            "Project content record not found or has been deleted.",
                        },
                    }
                );
            }

            DataRow existingRow = existingDt.Rows[0];

            string layoutType =
                existingRow.Table.Columns.Contains("LayoutType")
                && existingRow["LayoutType"] != DBNull.Value
                && !string.IsNullOrWhiteSpace(existingRow["LayoutType"]?.ToString())
                    ? existingRow["LayoutType"].ToString()!
                    : "grid-3";

            if (!string.IsNullOrWhiteSpace(request.LayoutType))
            {
                string cleanedLayout = request.LayoutType.Trim().ToLowerInvariant();
                if (!AllowedLayouts.Contains(cleanedLayout))
                {
                    validationErrors.Add(
                        $"Invalid layout type '{cleanedLayout}'. Allowed values: {string.Join(", ", AllowedLayouts)}"
                    );
                }
                else
                {
                    layoutType = cleanedLayout;
                }
            }

            // Validate image operations and enforce 15-image limit
            var newFiles = request.NewImages ?? new List<IFormFile>();
            for (int i = 0; i < newFiles.Count; i++)
            {
                var file = newFiles[i];
                var (isValid, errorMessage) = await FileValidationHelper.IsValidImageAsync(
                    file,
                    MaxImageSizeInBytes
                );
                if (!isValid)
                {
                    validationErrors.Add($"New image #{i + 1} ('{file.FileName}'): {errorMessage}");
                }
            }

            // Decode image IDs to remove
            var removeIds = new List<long>();
            if (request.RemoveImageIds != null)
            {
                foreach (var encId in request.RemoveImageIds)
                {
                    if (_encryption.TryDecrypt(encId, out long rId) && rId > 0)
                    {
                        removeIds.Add(rId);
                    }
                }
            }

            // Query currently active images
            const string existingImgsSql =
                @"
                SELECT ID, ImagePath, DisplayOrder
                FROM drs_project_image_mst WITH (NOLOCK)
                WHERE project_id = @ProjectId AND IsDeleted = 0;";
            DataTable existingImgsDt = await _db.ExecuteQueryAsync(
                existingImgsSql,
                new[] { new SqlParameter("@ProjectId", projectId) }
            );

            int currentImageCount = existingImgsDt.Rows.Count;
            int removedValidCount = existingImgsDt
                .AsEnumerable()
                .Count(r => removeIds.Contains(Convert.ToInt64(r["ID"])));
            int projectedTotalImages = (currentImageCount - removedValidCount) + newFiles.Count;

            if (projectedTotalImages > MaxAllowedImages)
            {
                validationErrors.Add(
                    $"Total images for this section cannot exceed {MaxAllowedImages}. Current active: {currentImageCount}, removing: {removedValidCount}, new: {newFiles.Count}, resulting total: {projectedTotalImages}."
                );
            }

            if (validationErrors.Count > 0)
            {
                sw.Stop();
                Logs.Warning(
                    $"Project Update validation failed | Errors: {string.Join("; ", validationErrors)}"
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

            string uploadsDir = GetUploadsDirectory();
            var createdPhysicalPaths = new List<string>();

            using SqlConnection conn = await _db.GetOpenConnectionAsync();
            using SqlTransaction tran = conn.BeginTransaction();

            try
            {
                // Preserve existing flags if not supplied in request
                bool isPublished =
                    request.IsPublished ?? Convert.ToBoolean(existingRow["IsPublished"]);
                bool isActive = request.IsActive ?? Convert.ToBoolean(existingRow["IsActive"]);

                const string updateSql =
                    @"
                    UPDATE drs_project_mst
                    SET Heading = @Heading,
                        Subheading = @Subheading,
                        ContentType = @ContentType,
                        ProjectOrEventTitle = @ProjectOrEventTitle,
                        DisplayYear = @DisplayYear,
                        ShortDescription = @ShortDescription,
                        DetailedDescription = @DetailedDescription,
                        Credits = @Credits,
                        Location = @Location,
                        ExternalUrl = @ExternalUrl,
                        DisplayOrder = @DisplayOrder,
                        LayoutType = @LayoutType,
                        IsPublished = @IsPublished,
                        IsActive = @IsActive,
                        ModifiedDate = GETDATE()
                    WHERE ID = @ID AND IsDeleted = 0;";

                using SqlCommand updateCmd = new SqlCommand(updateSql, conn, tran);
                updateCmd.Parameters.AddWithValue("@Heading", request.Heading);
                updateCmd.Parameters.AddWithValue(
                    "@Subheading",
                    (object?)request.Subheading ?? DBNull.Value
                );
                updateCmd.Parameters.AddWithValue(
                    "@ContentType",
                    (object?)request.ContentType ?? DBNull.Value
                );
                updateCmd.Parameters.AddWithValue(
                    "@ProjectOrEventTitle",
                    (object?)request.ProjectOrEventTitle ?? DBNull.Value
                );
                updateCmd.Parameters.AddWithValue(
                    "@DisplayYear",
                    (object?)request.DisplayYear ?? DBNull.Value
                );
                updateCmd.Parameters.AddWithValue(
                    "@ShortDescription",
                    (object?)request.ShortDescription ?? DBNull.Value
                );
                updateCmd.Parameters.AddWithValue(
                    "@DetailedDescription",
                    (object?)request.DetailedDescription ?? DBNull.Value
                );
                updateCmd.Parameters.AddWithValue(
                    "@Credits",
                    (object?)request.Credits ?? DBNull.Value
                );
                updateCmd.Parameters.AddWithValue(
                    "@Location",
                    (object?)request.Location ?? DBNull.Value
                );
                updateCmd.Parameters.AddWithValue(
                    "@ExternalUrl",
                    (object?)request.ExternalUrl ?? DBNull.Value
                );
                updateCmd.Parameters.AddWithValue("@DisplayOrder", request.DisplayOrder);
                updateCmd.Parameters.AddWithValue("@LayoutType", layoutType);
                updateCmd.Parameters.AddWithValue("@IsPublished", isPublished);
                updateCmd.Parameters.AddWithValue("@IsActive", isActive);
                updateCmd.Parameters.AddWithValue("@ID", projectId);

                await updateCmd.ExecuteNonQueryAsync();

                // Process removals
                if (removeIds.Count > 0)
                {
                    foreach (var imgId in removeIds)
                    {
                        const string delImgSql =
                            @"
                            UPDATE drs_project_image_mst
                            SET IsDeleted = 1, IsActive = 0, ModifiedDate = GETDATE()
                            WHERE ID = @ImageId AND project_id = @ProjectId AND IsDeleted = 0;";

                        using SqlCommand delImgCmd = new SqlCommand(delImgSql, conn, tran);
                        delImgCmd.Parameters.AddWithValue("@ImageId", imgId);
                        delImgCmd.Parameters.AddWithValue("@ProjectId", projectId);
                        await delImgCmd.ExecuteNonQueryAsync();
                    }
                }

                // Determine current maximum display order
                const string maxOrderSql =
                    @"
                    SELECT ISNULL(MAX(DisplayOrder), 0)
                    FROM drs_project_image_mst WITH (NOLOCK)
                    WHERE project_id = @ProjectId AND IsDeleted = 0;";
                using SqlCommand maxOrderCmd = new SqlCommand(maxOrderSql, conn, tran);
                maxOrderCmd.Parameters.AddWithValue("@ProjectId", projectId);
                int currentMaxDisplayOrder = Convert.ToInt32(
                    await maxOrderCmd.ExecuteScalarAsync()
                );

                // Save new images
                for (int i = 0; i < newFiles.Count; i++)
                {
                    var file = newFiles[i];
                    string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    string safeFileName = $"{projectId}_{Guid.NewGuid()}{extension}";
                    string physicalPath = Path.Combine(uploadsDir, safeFileName);
                    string relativePath = $"/uploads/projects/{safeFileName}";

                    using (var stream = new FileStream(physicalPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    createdPhysicalPaths.Add(physicalPath);

                    currentMaxDisplayOrder++;

                    const string insertImgSql =
                        @"
                        INSERT INTO drs_project_image_mst (
                            project_id, OriginalFileName, StoredFileName, ImagePath,
                            ContentType, FileSize, DisplayOrder, IsActive, IsDeleted, CreatedDate
                        )
                        VALUES (
                            @ProjectId, @OriginalFileName, @StoredFileName, @ImagePath,
                            @ContentType, @FileSize, @DisplayOrder, 1, 0, GETDATE()
                        );";

                    using SqlCommand insertImgCmd = new SqlCommand(insertImgSql, conn, tran);
                    insertImgCmd.Parameters.AddWithValue("@ProjectId", projectId);
                    insertImgCmd.Parameters.AddWithValue("@OriginalFileName", file.FileName);
                    insertImgCmd.Parameters.AddWithValue("@StoredFileName", safeFileName);
                    insertImgCmd.Parameters.AddWithValue("@ImagePath", relativePath);
                    insertImgCmd.Parameters.AddWithValue("@ContentType", file.ContentType);
                    insertImgCmd.Parameters.AddWithValue("@FileSize", file.Length);
                    insertImgCmd.Parameters.AddWithValue("@DisplayOrder", currentMaxDisplayOrder);

                    await insertImgCmd.ExecuteNonQueryAsync();
                }

                // Apply image reordering if provided
                if (request.ImageOrders != null && request.ImageOrders.Count > 0)
                {
                    foreach (var orderItem in request.ImageOrders)
                    {
                        if (
                            _encryption.TryDecrypt(orderItem.EncryptedImageId, out long ordImgId)
                            && ordImgId > 0
                            && orderItem.DisplayOrder >= 0
                        )
                        {
                            const string reorderSql =
                                @"
                                UPDATE drs_project_image_mst
                                SET DisplayOrder = @DisplayOrder, ModifiedDate = GETDATE()
                                WHERE ID = @ImageId AND project_id = @ProjectId AND IsDeleted = 0;";

                            using SqlCommand reorderCmd = new SqlCommand(reorderSql, conn, tran);
                            reorderCmd.Parameters.AddWithValue(
                                "@DisplayOrder",
                                orderItem.DisplayOrder
                            );
                            reorderCmd.Parameters.AddWithValue("@ImageId", ordImgId);
                            reorderCmd.Parameters.AddWithValue("@ProjectId", projectId);
                            await reorderCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                await tran.CommitAsync();

                // Clean up physical files for removed images
                if (removeIds.Count > 0)
                {
                    foreach (DataRow r in existingImgsDt.Rows)
                    {
                        long rId = Convert.ToInt64(r["ID"]);
                        if (removeIds.Contains(rId))
                        {
                            string imgPath = r["ImagePath"]?.ToString() ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(imgPath))
                            {
                                string relative = imgPath.TrimStart('/', '\\');
                                string physical = Path.Combine(
                                    _env.WebRootPath
                                        ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                                    relative
                                );
                                try
                                {
                                    if (System.IO.File.Exists(physical))
                                        System.IO.File.Delete(physical);
                                }
                                catch (Exception delEx)
                                {
                                    Logs.Error(
                                        $"Failed to delete physical file '{physical}' for image ID {rId}",
                                        delEx
                                    );
                                }
                            }
                        }
                    }
                }

                long adminUserId = GetAuthenticatedUserId();
                await _audit.InsertAuditAsync(
                    adminUserId,
                    "Projects",
                    "Update",
                    "drs_project_mst",
                    projectId,
                    JsonSerializer.Serialize(
                        new
                        {
                            Heading = existingRow["Heading"]?.ToString(),
                            IsPublished = Convert.ToBoolean(existingRow["IsPublished"]),
                            IsActive = Convert.ToBoolean(existingRow["IsActive"]),
                        }
                    ),
                    JsonSerializer.Serialize(
                        new
                        {
                            request.Heading,
                            LayoutType = layoutType,
                            isPublished,
                            isActive,
                            AddedImages = newFiles.Count,
                            RemovedImages = removeIds.Count,
                        }
                    ),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info(
                    $"Project Update completed successfully | projectId: {projectId} | Elapsed: {sw.ElapsedMilliseconds}ms"
                );

                // Fetch fresh updated record & images
                var updatedImages = await GetImagesForProjectAsync(projectId, activeOnly: true);
                var responseData = new ProjectDetailResponse
                {
                    EncryptedProjectId = request.EncryptedProjectId,
                    Heading = request.Heading,
                    Subheading = request.Subheading,
                    ContentType = request.ContentType,
                    ProjectOrEventTitle = request.ProjectOrEventTitle,
                    DisplayYear = request.DisplayYear,
                    ShortDescription = request.ShortDescription,
                    DetailedDescription = request.DetailedDescription,
                    Credits = request.Credits,
                    Location = request.Location,
                    ExternalUrl = request.ExternalUrl,
                    DisplayOrder = request.DisplayOrder,
                    LayoutType = layoutType,
                    IsPublished = isPublished,
                    IsActive = isActive,
                    ImageCount = updatedImages.Count,
                    Images = updatedImages,
                    CreatedDate = DateTimeFormat.Format(existingRow["CreatedDate"] as DateTime?),
                    ModifiedDate = DateTimeFormat.Format(DateTime.Now),
                };

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PROJECT_UPDATED",
                        Data = responseData,
                    }
                );
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();

                foreach (var path in createdPhysicalPaths)
                {
                    try
                    {
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);
                    }
                    catch (Exception cleanEx)
                    {
                        Logs.Error(
                            $"Failed to clean up physical file '{path}' after update rollback",
                            cleanEx
                        );
                    }
                }

                sw.Stop();
                Logs.Error($"Project Update API failed | projectId: {projectId}", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while updating Project content.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Get Project By Id API
        /// <summary>
        /// Admin endpoint to retrieve a single Project content entry by encrypted ID with all associated images.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("{encryptedProjectId}")]
        [HttpGet("admin/{encryptedProjectId}")]
        public async Task<IActionResult> GetById([FromRoute] string encryptedProjectId)
        {
            Logs.Info($"Project GetById API started | encryptedProjectId: {encryptedProjectId}");
            var sw = Stopwatch.StartNew();

            if (!_encryption.TryDecrypt(encryptedProjectId, out long projectId) || projectId <= 0)
            {
                sw.Stop();
                Logs.Warning($"Project GetById rejected | Invalid encryptedProjectId: {encryptedProjectId}");
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_PROJECT_ID",
                        Errors = new List<string> { "Invalid or tampered Project ID." },
                    }
                );
            }

            try
            {
                const string query =
                    @"
                    SELECT *
                    FROM drs_project_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0;";

                DataTable dt = await _db.ExecuteQueryAsync(
                    query,
                    new[] { new SqlParameter("@ID", projectId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning($"Project GetById rejected | Project not found | ProjectId: {projectId}");
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "PROJECT_NOT_FOUND",
                            Errors = new List<string>
                            {
                                "Project content record not found or has been deleted.",
                            },
                        }
                    );
                }

                var images = await GetImagesForProjectAsync(projectId, activeOnly: false);
                var response = MapProjectDetail(dt.Rows[0], images);

                sw.Stop();
                Logs.Info($"Project GetById completed successfully | ProjectId: {projectId} Images: {images.Count} Elapsed: {sw.ElapsedMilliseconds}ms");
                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = response,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error($"Project GetById API failed | projectId: {projectId}", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while retrieving Project content.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Get Project List API
        /// <summary>
        /// Admin endpoint to list all Project content entries with pagination, search, status filtering, and sorting.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("list")]
        [HttpPost("list")]
        public async Task<IActionResult> GetAll(
            [FromQuery] ProjectListRequest? queryRequest,
            [FromBody] ProjectListRequest? bodyRequest
        )
        {
            Logs.Info("Project GetAll API started");
            var sw = Stopwatch.StartNew();

            var request =
                (HttpMethods.IsPost(Request.Method) ? bodyRequest : queryRequest)
                ?? queryRequest
                ?? bodyRequest
                ?? new ProjectListRequest();

            try
            {
                int pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
                int pageSize =
                    request.PageSize < 1 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);
                int offset = (pageIndex - 1) * pageSize;

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@Offset", offset),
                    new SqlParameter("@PageSize", pageSize),
                };

                string whereClause = "WHERE a.IsDeleted = 0";

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    whereClause +=
                        @" AND (
                        a.Heading LIKE @Search OR
                        a.Subheading LIKE @Search OR
                        a.ContentType LIKE @Search OR
                        a.ProjectOrEventTitle LIKE @Search OR
                        a.Credits LIKE @Search OR
                        a.Location LIKE @Search
                    )";
                    parameters.Add(new SqlParameter("@Search", $"%{request.Search.Trim()}%"));
                }

                if (!string.IsNullOrWhiteSpace(request.ContentType))
                {
                    whereClause += " AND a.ContentType LIKE @ContentType";
                    parameters.Add(
                        new SqlParameter("@ContentType", $"%{request.ContentType.Trim()}%")
                    );
                }

                if (request.IsPublished.HasValue)
                {
                    whereClause += " AND a.IsPublished = @IsPublished";
                    parameters.Add(new SqlParameter("@IsPublished", request.IsPublished.Value));
                }

                if (request.IsActive.HasValue)
                {
                    whereClause += " AND a.IsActive = @IsActive";
                    parameters.Add(new SqlParameter("@IsActive", request.IsActive.Value));
                }

                string orderByClause = (request.SortBy?.Trim().ToLowerInvariant()) switch
                {
                    "orderdesc" or "order_desc" or "displayorder_desc" =>
                        "ORDER BY a.DisplayOrder DESC, a.ID DESC",
                    "orderasc" or "order_asc" or "displayorder" or "displayorder_asc" =>
                        "ORDER BY a.DisplayOrder ASC, a.ID ASC",
                    "newest" or "date" or "createddate" or "date_desc" =>
                        "ORDER BY a.CreatedDate DESC, a.ID DESC",
                    "oldest" or "date_asc" => "ORDER BY a.CreatedDate ASC, a.ID ASC",
                    "headingasc" or "heading" or "heading_asc" =>
                        "ORDER BY a.Heading ASC, a.ID ASC",
                    "heading_desc" or "headingdesc" => "ORDER BY a.Heading DESC, a.ID DESC",
                    _ => "ORDER BY a.DisplayOrder ASC, a.ID ASC",
                };

                string countSql =
                    $"SELECT COUNT(1) FROM drs_project_mst a WITH (NOLOCK) {whereClause};";
                object? totalObj = await _db.ExecuteScalarAsync(countSql, parameters.ToArray());
                int totalRecords =
                    totalObj != null && totalObj != DBNull.Value ? Convert.ToInt32(totalObj) : 0;

                string dataSql =
                    $@"
                    SELECT a.*
                    FROM drs_project_mst a WITH (NOLOCK)
                    {whereClause}
                    {orderByClause}
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                DataTable dt = await _db.ExecuteQueryAsync(dataSql, parameters.ToArray());

                var projectIds = dt.AsEnumerable().Select(r => Convert.ToInt64(r["ID"])).ToList();
                var imageMap = await GetImagesForProjectBatchAsync(projectIds, activeOnly: false);

                var items = new List<ProjectDetailResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    long aId = Convert.ToInt64(row["ID"]);
                    var images = imageMap.TryGetValue(aId, out var imgList)
                        ? imgList
                        : new List<ProjectImageDto>();
                    items.Add(MapProjectDetail(row, images));
                }

                sw.Stop();
                Logs.Info(
                    $"Project GetAll completed | Total: {totalRecords} | Page: {pageIndex}/{pageSize} | Elapsed: {sw.ElapsedMilliseconds}ms"
                );

                var response = new ProjectListResponse
                {
                    Items = items,
                    TotalRecords = totalRecords,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                };

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = response,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Project GetAll API failed with exception", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while retrieving Projects listing.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Get Public Projects API
        /// <summary>
        /// Public endpoint to fetch all active, published Project sections in configured DisplayOrder for dynamic website rendering.
        /// Excludes sensitive internal administrative fields and unpublished drafts.
        /// </summary>
        [PublicResponseCache(PublicCacheRegions.Projects, 300, SkipForAdmin = true)]
        [AllowAnonymous]
        [HttpGet("public")]
        public async Task<IActionResult> GetPublishedPublicContent()
        {
            Logs.Info("Projects Public API started");
            var sw = Stopwatch.StartNew();

            try
            {
                const string sql =
                    @"
                    SELECT ID, Heading, Subheading, ContentType, ProjectOrEventTitle, DisplayYear,
                           ShortDescription, DetailedDescription, Credits, Location, ExternalUrl, DisplayOrder, LayoutType
                    FROM drs_project_mst WITH (NOLOCK)
                    WHERE IsDeleted = 0 AND IsActive = 1 AND IsPublished = 1
                    ORDER BY DisplayOrder ASC, ID ASC;";

                DataTable dt = await _db.ExecuteQueryAsync(sql);

                var projectIds = dt.AsEnumerable().Select(r => Convert.ToInt64(r["ID"])).ToList();
                var imageMap = await GetImagesForProjectBatchAsync(projectIds, activeOnly: true);

                var publicItems = new List<ProjectPublicResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    long aId = Convert.ToInt64(row["ID"]);
                    var images = imageMap.TryGetValue(aId, out var imgList)
                        ? imgList
                        : new List<ProjectImageDto>();
                    publicItems.Add(MapProjectPublic(row, images));
                }

                sw.Stop();
                Logs.Info(
                    $"Projects Public API completed | Sections returned: {publicItems.Count} | Elapsed: {sw.ElapsedMilliseconds}ms"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = publicItems,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Projects Public API failed with exception", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while retrieving Projects public content.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Toggle Project Publish API
        /// <summary>
        /// Admin endpoint to toggle or explicitly set the publication status of an Project content section.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("{encryptedProjectId}/publish")]
        [HttpPatch("publish")]
        public async Task<IActionResult> TogglePublish(
            [FromRoute] string? encryptedProjectId = null,
            [FromQuery] bool? isPublished = null,
            [FromBody] ProjectPublishToggleRequest? bodyRequest = null
        )
        {
            Logs.Info("Project TogglePublish API started");
            var sw = Stopwatch.StartNew();

            string targetEncryptedId = !string.IsNullOrWhiteSpace(encryptedProjectId)
                ? encryptedProjectId
                : bodyRequest?.EncryptedProjectId ?? string.Empty;

            if (!_encryption.TryDecrypt(targetEncryptedId, out long projectId) || projectId <= 0)
            {
                sw.Stop();
                Logs.Warning($"Project TogglePublish rejected | Invalid targetEncryptedId: {targetEncryptedId}");
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_PROJECT_ID",
                        Errors = new List<string> { "Invalid or tampered Project ID." },
                    }
                );
            }

            try
            {
                const string findSql =
                    "SELECT IsPublished FROM drs_project_mst WITH (NOLOCK) WHERE ID = @ID AND IsDeleted = 0;";
                object? currentPubObj = await _db.ExecuteScalarAsync(
                    findSql,
                    new[] { new SqlParameter("@ID", projectId) }
                );
                if (currentPubObj == null || currentPubObj == DBNull.Value)
                {
                    sw.Stop();
                    Logs.Warning($"Project TogglePublish rejected | Project not found | ProjectId: {projectId}");
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "PROJECT_NOT_FOUND",
                            Errors = new List<string>
                            {
                                "Project content record not found or has been deleted.",
                            },
                        }
                    );
                }

                bool currentPub = Convert.ToBoolean(currentPubObj);
                bool newPublishStatus = bodyRequest?.IsPublished ?? isPublished ?? !currentPub;

                const string updateSql =
                    @"
                    UPDATE drs_project_mst
                    SET IsPublished = @IsPublished, ModifiedDate = GETDATE()
                    WHERE ID = @ID AND IsDeleted = 0;";

                await _db.ExecuteNonQueryAsync(
                    updateSql,
                    new[]
                    {
                        new SqlParameter("@IsPublished", newPublishStatus),
                        new SqlParameter("@ID", projectId),
                    }
                );

                long adminUserId = GetAuthenticatedUserId();
                await _audit.InsertAuditAsync(
                    adminUserId,
                    "Projects",
                    newPublishStatus ? "Publish" : "Unpublish",
                    "drs_project_mst",
                    projectId,
                    JsonSerializer.Serialize(new { IsPublished = currentPub }),
                    JsonSerializer.Serialize(new { IsPublished = newPublishStatus }),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info(
                    $"Project TogglePublish completed | projectId: {projectId} | IsPublished: {newPublishStatus}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = newPublishStatus ? "PROJECT_PUBLISHED" : "PROJECT_UNPUBLISHED",
                        Data = new
                        {
                            EncryptedProjectId = targetEncryptedId,
                            IsPublished = newPublishStatus,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error($"Project TogglePublish API failed | projectId: {projectId}", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while updating publication status.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Delete Project API
        /// <summary>
        /// Admin endpoint to soft-delete an Project content section and soft-delete its associated image mappings safely.
        /// Shared or active images referenced elsewhere are not deleted.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("delete/{encryptedProjectId}")]
        [HttpDelete("{encryptedProjectId}")]
        public async Task<IActionResult> Delete([FromRoute] string encryptedProjectId)
        {
            Logs.Info($"Project Delete API started | encryptedProjectId: {encryptedProjectId}");
            var sw = Stopwatch.StartNew();

            if (!_encryption.TryDecrypt(encryptedProjectId, out long projectId) || projectId <= 0)
            {
                sw.Stop();
                Logs.Warning($"Project Delete rejected | Invalid encryptedProjectId: {encryptedProjectId}");
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_PROJECT_ID",
                        Errors = new List<string> { "Invalid or tampered Project ID." },
                    }
                );
            }

            using SqlConnection conn = await _db.GetOpenConnectionAsync();
            using SqlTransaction tran = conn.BeginTransaction();

            try
            {
                const string checkSql =
                    "SELECT Heading FROM drs_project_mst WITH (NOLOCK) WHERE ID = @ID AND IsDeleted = 0;";
                using SqlCommand checkCmd = new SqlCommand(checkSql, conn, tran);
                checkCmd.Parameters.AddWithValue("@ID", projectId);
                object? headingObj = await checkCmd.ExecuteScalarAsync();

                if (headingObj == null || headingObj == DBNull.Value)
                {
                    await tran.RollbackAsync();
                    sw.Stop();
                    Logs.Warning($"Project Delete rejected | Project not found | ProjectId: {projectId}");
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "PROJECT_NOT_FOUND",
                            Errors = new List<string>
                            {
                                "Project content record not found or already deleted.",
                            },
                        }
                    );
                }

                // Query images before soft deletion
                const string getImgsSql =
                    "SELECT ID, ImagePath FROM drs_project_image_mst WITH (NOLOCK) WHERE project_id = @ProjectId AND IsDeleted = 0;";
                using SqlCommand getImgsCmd = new SqlCommand(getImgsSql, conn, tran);
                getImgsCmd.Parameters.AddWithValue("@ProjectId", projectId);
                using SqlDataAdapter da = new SqlDataAdapter(getImgsCmd);
                DataTable imgsDt = new DataTable();
                da.Fill(imgsDt);

                // Soft delete main record
                const string deleteProjectSql =
                    @"
                    UPDATE drs_project_mst
                    SET IsDeleted = 1, IsActive = 0, ModifiedDate = GETDATE()
                    WHERE ID = @ID AND IsDeleted = 0;";
                using SqlCommand delProjectCmd = new SqlCommand(deleteProjectSql, conn, tran);
                delProjectCmd.Parameters.AddWithValue("@ID", projectId);
                await delProjectCmd.ExecuteNonQueryAsync();

                // Soft delete associated images
                const string deleteImgsSql =
                    @"
                    UPDATE drs_project_image_mst
                    SET IsDeleted = 1, IsActive = 0, ModifiedDate = GETDATE()
                    WHERE project_id = @ProjectId AND IsDeleted = 0;";
                using SqlCommand delImgsCmd = new SqlCommand(deleteImgsSql, conn, tran);
                delImgsCmd.Parameters.AddWithValue("@ProjectId", projectId);
                await delImgsCmd.ExecuteNonQueryAsync();

                await tran.CommitAsync();

                // Delete physical files
                foreach (DataRow r in imgsDt.Rows)
                {
                    string path = r["ImagePath"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        string relative = path.TrimStart('/', '\\');
                        string physical = Path.Combine(
                            _env.WebRootPath
                                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                            relative
                        );
                        try
                        {
                            if (System.IO.File.Exists(physical))
                                System.IO.File.Delete(physical);
                        }
                        catch (Exception delEx)
                        {
                            Logs.Error(
                                $"Failed to delete physical file '{physical}' during Project section deletion",
                                delEx
                            );
                        }
                    }
                }

                long adminUserId = GetAuthenticatedUserId();
                await _audit.InsertAuditAsync(
                    adminUserId,
                    "Projects",
                    "Delete",
                    "drs_project_mst",
                    projectId,
                    JsonSerializer.Serialize(
                        new { Heading = headingObj.ToString(), ImagesCount = imgsDt.Rows.Count }
                    ),
                    JsonSerializer.Serialize(new { IsDeleted = true, IsActive = false }),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"Project Delete completed successfully | projectId: {projectId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PROJECT_DELETED",
                        Data = new { EncryptedProjectId = encryptedProjectId },
                    }
                );
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                sw.Stop();
                Logs.Error($"Project Delete API failed | projectId: {projectId}", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while deleting Project content.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Upload Project Images API
        /// <summary>
        /// Admin endpoint to upload additional images to an existing Project entry, verifying the 15-image limit.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("images/upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImages([FromForm] ProjectImageUploadRequest? request)
        {
            Logs.Info("Project UploadImages API started");
            var sw = Stopwatch.StartNew();

            if (request == null || request.Images == null || request.Images.Count == 0)
            {
                sw.Stop();
                Logs.Warning("Project UploadImages rejected | Reason: Missing files or null request body");
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_REQUEST",
                        Errors = new List<string> { "At least one image file is required." },
                    }
                );
            }

            if (
                !_encryption.TryDecrypt(request.EncryptedProjectId, out long projectId)
                || projectId <= 0
            )
            {
                sw.Stop();
                Logs.Warning($"Project UploadImages rejected | Reason: Invalid encryptedProjectId: {request.EncryptedProjectId}");
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_PROJECT_ID",
                        Errors = new List<string> { "Invalid or tampered Project ID." },
                    }
                );
            }

            // Check existence
            const string checkSql =
                "SELECT COUNT(1) FROM drs_project_mst WITH (NOLOCK) WHERE ID = @ID AND IsDeleted = 0;";
            int exists = Convert.ToInt32(
                await _db.ExecuteScalarAsync(checkSql, new[] { new SqlParameter("@ID", projectId) })
                    ?? 0
            );
            if (exists == 0)
            {
                sw.Stop();
                Logs.Warning($"Project UploadImages rejected | Project not found | ProjectId: {projectId}");
                return NotFound(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "PROJECT_NOT_FOUND",
                        Errors = new List<string>
                        {
                            "Target Project content entry does not exist or has been deleted.",
                        },
                    }
                );
            }

            // Check max 15 images limit
            const string countSql =
                "SELECT COUNT(1) FROM drs_project_image_mst WITH (NOLOCK) WHERE project_id = @ProjectId AND IsDeleted = 0;";
            int currentCount = Convert.ToInt32(
                await _db.ExecuteScalarAsync(
                    countSql,
                    new[] { new SqlParameter("@ProjectId", projectId) }
                ) ?? 0
            );

            if (currentCount + request.Images.Count > MaxAllowedImages)
            {
                sw.Stop();
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "MAXIMUM_PROJECT_IMAGES_EXCEEDED",
                        Errors = new List<string>
                        {
                            $"Cannot upload more than {MaxAllowedImages} images per Project section. Currently active: {currentCount}, requested new: {request.Images.Count}.",
                        },
                    }
                );
            }

            // Validate files
            var validationErrors = new List<string>();
            for (int i = 0; i < request.Images.Count; i++)
            {
                var file = request.Images[i];
                var (isValid, errorMessage) = await FileValidationHelper.IsValidImageAsync(
                    file,
                    MaxImageSizeInBytes
                );
                if (!isValid)
                {
                    validationErrors.Add($"File #{i + 1} ('{file.FileName}'): {errorMessage}");
                }
            }

            if (validationErrors.Count > 0)
            {
                sw.Stop();
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

            string uploadsDir = GetUploadsDirectory();
            var createdPhysicalPaths = new List<string>();
            var savedImages = new List<ProjectImageDto>();

            using SqlConnection conn = await _db.GetOpenConnectionAsync();
            using SqlTransaction tran = conn.BeginTransaction();

            try
            {
                const string maxOrderSql =
                    "SELECT ISNULL(MAX(DisplayOrder), 0) FROM drs_project_image_mst WITH (NOLOCK) WHERE project_id = @ProjectId AND IsDeleted = 0;";
                using SqlCommand maxOrderCmd = new SqlCommand(maxOrderSql, conn, tran);
                maxOrderCmd.Parameters.AddWithValue("@ProjectId", projectId);
                int currentDisplayOrder = Convert.ToInt32(await maxOrderCmd.ExecuteScalarAsync());

                for (int i = 0; i < request.Images.Count; i++)
                {
                    var file = request.Images[i];
                    string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    string safeFileName = $"{projectId}_{Guid.NewGuid()}{extension}";
                    string physicalPath = Path.Combine(uploadsDir, safeFileName);
                    string relativePath = $"/uploads/projects/{safeFileName}";

                    using (var stream = new FileStream(physicalPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    createdPhysicalPaths.Add(physicalPath);

                    currentDisplayOrder++;

                    const string insertSql =
                        @"
                        INSERT INTO drs_project_image_mst (
                            project_id, OriginalFileName, StoredFileName, ImagePath,
                            ContentType, FileSize, DisplayOrder, IsActive, IsDeleted, CreatedDate
                        )
                        OUTPUT INSERTED.ID
                        VALUES (
                            @ProjectId, @OriginalFileName, @StoredFileName, @ImagePath,
                            @ContentType, @FileSize, @DisplayOrder, 1, 0, GETDATE()
                        );";

                    using SqlCommand insertCmd = new SqlCommand(insertSql, conn, tran);
                    insertCmd.Parameters.AddWithValue("@ProjectId", projectId);
                    insertCmd.Parameters.AddWithValue("@OriginalFileName", file.FileName);
                    insertCmd.Parameters.AddWithValue("@StoredFileName", safeFileName);
                    insertCmd.Parameters.AddWithValue("@ImagePath", relativePath);
                    insertCmd.Parameters.AddWithValue("@ContentType", file.ContentType);
                    insertCmd.Parameters.AddWithValue("@FileSize", file.Length);
                    insertCmd.Parameters.AddWithValue("@DisplayOrder", currentDisplayOrder);

                    long newImageId = Convert.ToInt64(await insertCmd.ExecuteScalarAsync());

                    savedImages.Add(
                        new ProjectImageDto
                        {
                            EncryptedImageId = _encryption.Encrypt(newImageId),
                            EncryptedProjectId = request.EncryptedProjectId,
                            OriginalFileName = file.FileName,
                            StoredFileName = safeFileName,
                            ImageUrl = relativePath,
                            ImagePath = relativePath,
                            FileSize = file.Length,
                            ContentType = file.ContentType,
                            DisplayOrder = currentDisplayOrder,
                            IsActive = true,
                            CreatedDate = DateTimeFormat.Format(DateTime.Now),
                        }
                    );
                }

                await tran.CommitAsync();

                long adminUserId = GetAuthenticatedUserId();
                await _audit.InsertAuditAsync(
                    adminUserId,
                    "ProjectImages",
                    "Upload",
                    "drs_project_image_mst",
                    projectId,
                    null,
                    $"Uploaded {savedImages.Count} images for Project section {projectId}",
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info(
                    $"Project UploadImages completed | projectId: {projectId} | Uploaded: {savedImages.Count}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PROJECT_IMAGE_UPLOADED",
                        Data = savedImages,
                    }
                );
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();

                foreach (var path in createdPhysicalPaths)
                {
                    try
                    {
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);
                    }
                    catch (Exception cleanEx)
                    {
                        Logs.Error(
                            $"Failed to clean up physical file '{path}' after upload failure",
                            cleanEx
                        );
                    }
                }

                sw.Stop();
                Logs.Error($"Project UploadImages API failed | projectId: {projectId}", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while uploading Project images.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Delete Project Image API
        /// <summary>
        /// Admin endpoint to delete a single Project image by its encrypted image ID.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("images/{encryptedImageId}")]
        public async Task<IActionResult> DeleteImage([FromRoute] string encryptedImageId)
        {
            Logs.Info($"Project DeleteImage API started | EncryptedImageId: {encryptedImageId}");
            var sw = Stopwatch.StartNew();

            if (!_encryption.TryDecrypt(encryptedImageId, out long imageId) || imageId <= 0)
            {
                sw.Stop();
                Logs.Warning($"Project DeleteImage rejected | Invalid encryptedImageId: {encryptedImageId}");
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_REQUEST",
                        Errors = new List<string> { "Invalid or tampered Image ID." },
                    }
                );
            }

            try
            {
                const string query =
                    "SELECT project_id, ImagePath FROM drs_project_image_mst WITH (NOLOCK) WHERE ID = @ID AND IsDeleted = 0;";
                DataTable dt = await _db.ExecuteQueryAsync(
                    query,
                    new[] { new SqlParameter("@ID", imageId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning($"Project DeleteImage rejected | Image not found | ImageId: {imageId}");
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "PROJECT_IMAGE_NOT_FOUND",
                            Errors = new List<string>
                            {
                                "Project image record not found or already deleted.",
                            },
                        }
                    );
                }

                long projectId = Convert.ToInt64(dt.Rows[0]["project_id"]);
                string imagePath = dt.Rows[0]["ImagePath"]?.ToString() ?? string.Empty;

                const string deleteSql =
                    "UPDATE drs_project_image_mst SET IsDeleted = 1, IsActive = 0, ModifiedDate = GETDATE() WHERE ID = @ID;";
                await _db.ExecuteNonQueryAsync(deleteSql, new[] { new SqlParameter("@ID", imageId) });

                // Delete physical file
                if (!string.IsNullOrWhiteSpace(imagePath))
                {
                    string relative = imagePath.TrimStart('/', '\\');
                    string physical = Path.Combine(
                        _env.WebRootPath
                            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                        relative
                    );
                    try
                    {
                        if (System.IO.File.Exists(physical))
                            System.IO.File.Delete(physical);
                    }
                    catch (Exception delEx)
                    {
                        Logs.Error(
                            $"Failed to delete physical file '{physical}' during Project image deletion",
                            delEx
                        );
                    }
                }

                long adminUserId = GetAuthenticatedUserId();
                await _audit.InsertAuditAsync(
                    adminUserId,
                    "ProjectImages",
                    "Delete",
                    "drs_project_image_mst",
                    imageId,
                    null,
                    $"Deleted image ID: {imageId} for Project section {projectId}",
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info(
                    $"Project DeleteImage completed | ImageId: {imageId} | projectId: {projectId}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PROJECT_IMAGE_DELETED",
                        Data = new
                        {
                            EncryptedImageId = encryptedImageId,
                            EncryptedProjectId = _encryption.Encrypt(projectId),
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error($"Project DeleteImage API failed | ImageId: {imageId}", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while deleting Project image.",
                        },
                    }
                );
            }
        }
        #endregion

        #region Reorder Project Images API
        /// <summary>
        /// Admin endpoint to update the display order sequence of images within an Project section.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("images/reorder")]
        [HttpPost("images/reorder")]
        public async Task<IActionResult> ReorderImages(
            [FromBody] ProjectImageReorderRequest? request
        )
        {
            Logs.Info("Project ReorderImages API started");
            var sw = Stopwatch.StartNew();

            if (request == null || request.ImageOrders == null || request.ImageOrders.Count == 0)
            {
                sw.Stop();
                Logs.Warning("Project ReorderImages rejected | Reason: Missing items or null request body");
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_REQUEST",
                        Errors = new List<string>
                        {
                            "Reorder request and image order items are required.",
                        },
                    }
                );
            }

            if (
                !_encryption.TryDecrypt(request.EncryptedProjectId, out long projectId)
                || projectId <= 0
            )
            {
                sw.Stop();
                Logs.Warning($"Project ReorderImages rejected | Reason: Invalid encryptedProjectId: {request.EncryptedProjectId}");
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_PROJECT_ID",
                        Errors = new List<string> { "Invalid or tampered Project ID." },
                    }
                );
            }

            using SqlConnection conn = await _db.GetOpenConnectionAsync();
            using SqlTransaction tran = conn.BeginTransaction();

            try
            {
                foreach (var item in request.ImageOrders)
                {
                    if (
                        _encryption.TryDecrypt(item.EncryptedImageId, out long imgId)
                        && imgId > 0
                        && item.DisplayOrder >= 0
                    )
                    {
                        const string updateSql =
                            @"
                            UPDATE drs_project_image_mst
                            SET DisplayOrder = @DisplayOrder, ModifiedDate = GETDATE()
                            WHERE ID = @ID AND project_id = @ProjectId AND IsDeleted = 0;";

                        using SqlCommand cmd = new SqlCommand(updateSql, conn, tran);
                        cmd.Parameters.AddWithValue("@DisplayOrder", item.DisplayOrder);
                        cmd.Parameters.AddWithValue("@ID", imgId);
                        cmd.Parameters.AddWithValue("@ProjectId", projectId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await tran.CommitAsync();

                long adminUserId = GetAuthenticatedUserId();
                await _audit.InsertAuditAsync(
                    adminUserId,
                    "ProjectImages",
                    "Reorder",
                    "drs_project_image_mst",
                    projectId,
                    null,
                    $"Reordered {request.ImageOrders.Count} images for Project section {projectId}",
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"Project ReorderImages completed successfully | projectId: {projectId}");

                var updatedImages = await GetImagesForProjectAsync(projectId, activeOnly: true);

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PROJECT_IMAGES_REORDERED",
                        Data = updatedImages,
                    }
                );
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                sw.Stop();
                Logs.Error($"Project ReorderImages API failed | projectId: {projectId}", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while reordering Project images.",
                        },
                    }
                );
            }
        }
        #endregion
    }
}
