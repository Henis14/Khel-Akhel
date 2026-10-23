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

namespace Khel_Akhel_Server.Controllers.HeroBanner
{
    [ApiController]
    [Route("api/hero-banner")]
    public class HeroBannerController : ControllerBase
    {
        private readonly DbHelper _db;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService _audit;
        private readonly IWebHostEnvironment _env;

        private const long MaxImageSizeInBytes = 5 * 1024 * 1024; // 5MB
        private const long MaxVideoSizeInBytes = 50 * 1024 * 1024; // 50MB

        public HeroBannerController(
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

        private bool TryResolveBannerId(string idStr, out long bannerId)
        {
            bannerId = 0;
            if (string.IsNullOrWhiteSpace(idStr))
                return false;

            if (long.TryParse(idStr, out bannerId) && bannerId > 0)
                return true;

            if (_encryption.TryDecrypt(idStr, out bannerId) && bannerId > 0)
                return true;

            return false;
        }

        private HeroBannerItemResponse MapHeroBanner(DataRow row)
        {
            long id = Convert.ToInt64(row["Id"]);
            return new HeroBannerItemResponse
            {
                Id = id,
                EncryptedId = _encryption.Encrypt(id),
                Title = row["Title"] == DBNull.Value ? null : row["Title"].ToString(),
                MediaUrl = row["MediaUrl"]?.ToString() ?? string.Empty,
                IsImage = Convert.ToBoolean(row["IsImage"]),
                IsVideo = Convert.ToBoolean(row["IsVideo"]),
                DisplayOrder = Convert.ToInt32(row["DisplayOrder"]),
                IsActive = Convert.ToBoolean(row["IsActive"]),
                CreatedAt = row["CreatedAt"] == DBNull.Value
                    ? string.Empty
                    : Convert.ToDateTime(row["CreatedAt"]).ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedAt = row["UpdatedAt"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(row["UpdatedAt"]).ToString("yyyy-MM-dd HH:mm:ss"),
            };
        }

        private string GetUploadsDirectory()
        {
            string webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string targetDir = Path.Combine(webRoot, "uploads", "hero-banner");
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
            return targetDir;
        }

        private void SafeDeletePhysicalFile(string? mediaUrl)
        {
            if (string.IsNullOrWhiteSpace(mediaUrl))
                return;

            try
            {
                // Only delete files inside /uploads/hero-banner
                if (!mediaUrl.StartsWith("/uploads/hero-banner/", StringComparison.OrdinalIgnoreCase))
                    return;

                string fileName = Path.GetFileName(mediaUrl);
                string physicalPath = Path.Combine(GetUploadsDirectory(), fileName);
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }
            catch (Exception ex)
            {
                Logs.Warning($"Could not delete file for mediaUrl: {mediaUrl} | Reason: {ex.Message}");
            }
        }
        #endregion

        #region GET: Public Retrieve Active Hero Media
        [HttpGet]
        [HttpGet("public")]
        public async Task<IActionResult> GetPublicHeroMedia([FromQuery] bool? all, [FromQuery] bool? admin)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                // If admin specifically requested all records and has Admin role
                bool isAdminRequest = (all == true || admin == true) && User.IsInRole("Admin");

                string query = isAdminRequest
                    ? @"SELECT Id, Title, MediaUrl, IsImage, IsVideo, DisplayOrder, IsActive, CreatedAt, UpdatedAt
                        FROM HeroBanners
                        WHERE IsDeleted = 0
                        ORDER BY DisplayOrder ASC, CreatedAt ASC;"
                    : @"SELECT Id, Title, MediaUrl, IsImage, IsVideo, DisplayOrder, IsActive, CreatedAt, UpdatedAt
                        FROM HeroBanners
                        WHERE IsDeleted = 0 AND IsActive = 1
                        ORDER BY DisplayOrder ASC, CreatedAt ASC;";

                DataTable dt = await _db.ExecuteQueryAsync(query);
                var list = new List<HeroBannerItemResponse>();

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(MapHeroBanner(row));
                }

                sw.Stop();
                return Ok(new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "HERO_MEDIA_RETRIEVED",
                    Data = list,
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception in GetPublicHeroMedia", ex);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "SERVER_ERROR",
                    Errors = new List<string> { "An internal server error occurred while retrieving hero media." },
                });
            }
        }
        #endregion

        #region GET: Admin Retrieve All Hero Media
        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        [HttpGet("admin/list")]
        public async Task<IActionResult> GetAdminHeroMediaList()
        {
            var sw = Stopwatch.StartNew();

            try
            {
                string query = @"SELECT Id, Title, MediaUrl, IsImage, IsVideo, DisplayOrder, IsActive, CreatedAt, UpdatedAt
                                 FROM HeroBanners
                                 WHERE IsDeleted = 0
                                 ORDER BY DisplayOrder ASC, CreatedAt ASC;";

                DataTable dt = await _db.ExecuteQueryAsync(query);
                var list = new List<HeroBannerItemResponse>();

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(MapHeroBanner(row));
                }

                sw.Stop();
                return Ok(new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "HERO_MEDIA_ADMIN_RETRIEVED",
                    Data = list,
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception in GetAdminHeroMediaList", ex);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "SERVER_ERROR",
                    Errors = new List<string> { "An internal server error occurred." },
                });
            }
        }
        #endregion

        #region GET: Retrieve Specific Hero Media Record
        [HttpGet("{id}")]
        public async Task<IActionResult> GetHeroBannerById(string id)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                if (!TryResolveBannerId(id, out long bannerId))
                {
                    sw.Stop();
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_ID",
                        Errors = new List<string> { "Invalid Hero Banner ID specified." },
                    });
                }

                string query = @"SELECT Id, Title, MediaUrl, IsImage, IsVideo, DisplayOrder, IsActive, CreatedAt, UpdatedAt
                                 FROM HeroBanners
                                 WHERE Id = @Id AND IsDeleted = 0;";

                DataTable dt = await _db.ExecuteQueryAsync(query, new[] { new SqlParameter("@Id", bannerId) });

                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "HERO_BANNER_NOT_FOUND",
                        Errors = new List<string> { $"Hero Banner with ID '{id}' was not found." },
                    });
                }

                var response = MapHeroBanner(dt.Rows[0]);
                sw.Stop();

                return Ok(new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "HERO_BANNER_FOUND",
                    Data = response,
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error($"Exception in GetHeroBannerById for id {id}", ex);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "SERVER_ERROR",
                    Errors = new List<string> { "An internal server error occurred." },
                });
            }
        }
        #endregion

        #region POST: Create Hero Media (Images or MP4 Video)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [HttpPost("create")]
        public async Task<IActionResult> CreateHeroMedia([FromForm] HeroBannerCreateRequest request)
        {
            Logs.Info("CreateHeroMedia API started");
            var sw = Stopwatch.StartNew();

            if (request == null)
            {
                sw.Stop();
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "INVALID_REQUEST",
                    Errors = new List<string> { "Request payload cannot be empty." },
                });
            }

            // Flag validation: Both cannot be true simultaneously
            if (request.IsImage == true && request.IsVideo == true)
            {
                sw.Stop();
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "INVALID_FLAGS",
                    Errors = new List<string> { "Both IsImage and IsVideo cannot be true simultaneously." },
                });
            }

            // Flag validation: Both cannot be false if flags were explicitly provided
            if (request.IsImage == false && request.IsVideo == false)
            {
                sw.Stop();
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "INVALID_FLAGS",
                    Errors = new List<string> { "Both IsImage and IsVideo cannot be false for a valid media record." },
                });
            }

            // Mutual exclusion: Cannot upload both images and video in the same request
            bool hasImages = request.Images != null && request.Images.Count > 0;
            bool hasVideo = request.Video != null;

            if (hasImages && hasVideo)
            {
                sw.Stop();
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "MIXED_MEDIA_PROHIBITED",
                    Errors = new List<string> { "The hero section may contain multiple images OR one video, not both in the same request." },
                });
            }

            if (!hasImages && !hasVideo)
            {
                sw.Stop();
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "MISSING_MEDIA",
                    Errors = new List<string> { "Please provide either image file(s) or an MP4 video file." },
                });
            }

            // If flag was provided, ensure it matches actual payload
            if (hasImages && request.IsVideo == true)
            {
                sw.Stop();
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "FLAG_PAYLOAD_MISMATCH",
                    Errors = new List<string> { "Request contains images but IsVideo flag was set to true." },
                });
            }

            if (hasVideo && request.IsImage == true)
            {
                sw.Stop();
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "FLAG_PAYLOAD_MISMATCH",
                    Errors = new List<string> { "Request contains video but IsImage flag was set to true." },
                });
            }

            bool isImageMode = hasImages;
            var validationErrors = new List<string>();

            // Validate uploaded files
            if (isImageMode)
            {
                foreach (var file in request.Images!)
                {
                    var (isValid, errorMessage) = await FileValidationHelper.IsValidImageAsync(file, MaxImageSizeInBytes);
                    if (!isValid)
                    {
                        validationErrors.Add($"File '{file.FileName}': {errorMessage}");
                    }
                }
            }
            else
            {
                var (isValid, errorMessage) = await FileValidationHelper.IsValidMp4VideoAsync(request.Video!, MaxVideoSizeInBytes);
                if (!isValid)
                {
                    validationErrors.Add($"File '{request.Video!.FileName}': {errorMessage}");
                }
            }

            if (validationErrors.Count > 0)
            {
                sw.Stop();
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "VALIDATION_FAILED",
                    Errors = validationErrors,
                });
            }

            string uploadsDir = GetUploadsDirectory();
            var createdPhysicalPaths = new List<string>();
            var oldMediaUrlsToClean = new List<string>();
            var createdResponses = new List<HeroBannerItemResponse>();

            using SqlConnection conn = await _db.GetOpenConnectionAsync();
            using SqlTransaction tran = conn.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                // Concurrency and mode enforcement: Check current hero configuration with update lock
                string checkModeSql = @"
                    SELECT Id, MediaUrl, IsImage, IsVideo 
                    FROM HeroBanners WITH (UPDLOCK, HOLDLOCK) 
                    WHERE IsDeleted = 0;";

                var existingRows = new List<(long Id, string MediaUrl, bool IsImage, bool IsVideo)>();
                using (SqlCommand checkCmd = new SqlCommand(checkModeSql, conn, tran))
                using (SqlDataReader reader = await checkCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        existingRows.Add((
                            reader.GetInt64(0),
                            reader.GetString(1),
                            reader.GetBoolean(2),
                            reader.GetBoolean(3)
                        ));
                    }
                }

                bool existingHasImages = existingRows.Any(r => r.IsImage);
                bool existingHasVideo = existingRows.Any(r => r.IsVideo);

                // Business rule checks:
                if (isImageMode && existingHasVideo)
                {
                    if (!request.ReplaceExisting)
                    {
                        tran.Rollback();
                        sw.Stop();
                        return Conflict(new ApiResponse
                        {
                            Success = false,
                            StatusCode = 409,
                            Message = "MEDIA_MODE_CONFLICT",
                            Errors = new List<string>
                            {
                                "The hero section currently contains a video configuration. To upload images, set ReplaceExisting to true to replace the video configuration.",
                            },
                        });
                    }
                }
                else if (!isImageMode && existingHasImages)
                {
                    if (!request.ReplaceExisting)
                    {
                        tran.Rollback();
                        sw.Stop();
                        return Conflict(new ApiResponse
                        {
                            Success = false,
                            StatusCode = 409,
                            Message = "MEDIA_MODE_CONFLICT",
                            Errors = new List<string>
                            {
                                "The hero section currently contains image banners. To upload a video, set ReplaceExisting to true to replace the image configuration.",
                            },
                        });
                    }
                }
                else if (!isImageMode && existingHasVideo)
                {
                    if (!request.ReplaceExisting)
                    {
                        tran.Rollback();
                        sw.Stop();
                        return Conflict(new ApiResponse
                        {
                            Success = false,
                            StatusCode = 409,
                            Message = "VIDEO_LIMIT_REACHED",
                            Errors = new List<string>
                            {
                                "A hero video is already configured. Only one video is permitted. Set ReplaceExisting to true to replace it.",
                            },
                        });
                    }
                }

                // If replacing existing, soft delete all existing hero banners
                if (request.ReplaceExisting && existingRows.Count > 0)
                {
                    string softDeleteSql = "UPDATE HeroBanners SET IsDeleted = 1, UpdatedAt = GETDATE() WHERE IsDeleted = 0;";
                    using SqlCommand deleteCmd = new SqlCommand(softDeleteSql, conn, tran);
                    await deleteCmd.ExecuteNonQueryAsync();

                    foreach (var existing in existingRows)
                    {
                        oldMediaUrlsToClean.Add(existing.MediaUrl);
                    }
                }

                // Process files and insert records
                if (isImageMode)
                {
                    int orderIndex = request.DisplayOrder > 0 ? request.DisplayOrder : 1;

                    // If existing images exist and not replacing, determine next display order
                    if (!request.ReplaceExisting && existingHasImages)
                    {
                        string maxOrderSql = "SELECT ISNULL(MAX(DisplayOrder), 0) FROM HeroBanners WHERE IsDeleted = 0;";
                        using SqlCommand maxOrderCmd = new SqlCommand(maxOrderSql, conn, tran);
                        object? maxOrderObj = await maxOrderCmd.ExecuteScalarAsync();
                        int maxOrder = (maxOrderObj != null && maxOrderObj != DBNull.Value) ? Convert.ToInt32(maxOrderObj) : 0;
                        if (request.DisplayOrder <= 0)
                        {
                            orderIndex = maxOrder + 1;
                        }
                    }

                    for (int i = 0; i < request.Images!.Count; i++)
                    {
                        var file = request.Images[i];
                        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        string uniqueName = $"{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}{ext}";
                        string diskPath = Path.Combine(uploadsDir, uniqueName);

                        using (var stream = new FileStream(diskPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        createdPhysicalPaths.Add(diskPath);

                        string relativeUrl = $"/uploads/hero-banner/{uniqueName}";
                        int currentOrder = request.DisplayOrder > 0 ? request.DisplayOrder + i : orderIndex + i;

                        string insertSql = @"
                            INSERT INTO HeroBanners (Title, MediaUrl, IsImage, IsVideo, DisplayOrder, IsActive, IsDeleted, CreatedAt)
                            OUTPUT INSERTED.Id, INSERTED.CreatedAt
                            VALUES (@Title, @MediaUrl, 1, 0, @DisplayOrder, 1, 0, GETDATE());";

                        using SqlCommand insertCmd = new SqlCommand(insertSql, conn, tran);
                        insertCmd.Parameters.AddWithValue("@Title", (object?)request.Title?.Trim() ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@MediaUrl", relativeUrl);
                        insertCmd.Parameters.AddWithValue("@DisplayOrder", currentOrder);

                        using SqlDataReader insertReader = await insertCmd.ExecuteReaderAsync();
                        if (await insertReader.ReadAsync())
                        {
                            long insertedId = insertReader.GetInt64(0);
                            DateTime createdAt = insertReader.GetDateTime(1);

                            createdResponses.Add(new HeroBannerItemResponse
                            {
                                Id = insertedId,
                                EncryptedId = _encryption.Encrypt(insertedId),
                                Title = request.Title?.Trim(),
                                MediaUrl = relativeUrl,
                                IsImage = true,
                                IsVideo = false,
                                DisplayOrder = currentOrder,
                                IsActive = true,
                                CreatedAt = createdAt.ToString("yyyy-MM-dd HH:mm:ss"),
                            });
                        }
                    }
                }
                else
                {
                    // Video Mode
                    var file = request.Video!;
                    string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    string uniqueName = $"{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}{ext}";
                    string diskPath = Path.Combine(uploadsDir, uniqueName);

                    using (var stream = new FileStream(diskPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    createdPhysicalPaths.Add(diskPath);

                    string relativeUrl = $"/uploads/hero-banner/{uniqueName}";
                    int displayOrder = request.DisplayOrder > 0 ? request.DisplayOrder : 1;

                    string insertSql = @"
                        INSERT INTO HeroBanners (Title, MediaUrl, IsImage, IsVideo, DisplayOrder, IsActive, IsDeleted, CreatedAt)
                        OUTPUT INSERTED.Id, INSERTED.CreatedAt
                        VALUES (@Title, @MediaUrl, 0, 1, @DisplayOrder, 1, 0, GETDATE());";

                    using SqlCommand insertCmd = new SqlCommand(insertSql, conn, tran);
                    insertCmd.Parameters.AddWithValue("@Title", (object?)request.Title?.Trim() ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@MediaUrl", relativeUrl);
                    insertCmd.Parameters.AddWithValue("@DisplayOrder", displayOrder);

                    using SqlDataReader insertReader = await insertCmd.ExecuteReaderAsync();
                    if (await insertReader.ReadAsync())
                    {
                        long insertedId = insertReader.GetInt64(0);
                        DateTime createdAt = insertReader.GetDateTime(1);

                        createdResponses.Add(new HeroBannerItemResponse
                        {
                            Id = insertedId,
                            EncryptedId = _encryption.Encrypt(insertedId),
                            Title = request.Title?.Trim(),
                            MediaUrl = relativeUrl,
                            IsImage = false,
                            IsVideo = true,
                            DisplayOrder = displayOrder,
                            IsActive = true,
                            CreatedAt = createdAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        });
                    }
                }

                tran.Commit();

                // Safe cleanup of replaced old files after successful commit
                foreach (var oldUrl in oldMediaUrlsToClean)
                {
                    SafeDeletePhysicalFile(oldUrl);
                }

                sw.Stop();
                Logs.Info($"CreateHeroMedia succeeded | Created {createdResponses.Count} items");

                return StatusCode(201, new ApiResponse
                {
                    Success = true,
                    StatusCode = 201,
                    Message = isImageMode ? "HERO_IMAGES_CREATED" : "HERO_VIDEO_CREATED",
                    Data = createdResponses,
                });
            }
            catch (Exception ex)
            {
                try { tran.Rollback(); } catch { }

                // Safe cleanup of newly written files upon DB persistence failure
                foreach (var physicalPath in createdPhysicalPaths)
                {
                    try
                    {
                        if (System.IO.File.Exists(physicalPath))
                        {
                            System.IO.File.Delete(physicalPath);
                        }
                    }
                    catch (Exception fileEx)
                    {
                        Logs.Warning($"Failed to cleanup orphaned file: {physicalPath} | Reason: {fileEx.Message}");
                    }
                }

                sw.Stop();
                Logs.Error("Exception in CreateHeroMedia", ex);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "SERVER_ERROR",
                    Errors = new List<string> { "An internal server error occurred while persisting hero media." },
                });
            }
        }
        #endregion

        #region PUT: Update Hero Media Record or Replace File
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHeroMedia(string id, [FromForm] HeroBannerUpdateRequest request)
        {
            Logs.Info($"UpdateHeroMedia API started for id: {id}");
            var sw = Stopwatch.StartNew();

            if (!TryResolveBannerId(id, out long bannerId))
            {
                sw.Stop();
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "INVALID_ID",
                    Errors = new List<string> { "Invalid Hero Banner ID specified." },
                });
            }

            if (request == null)
            {
                sw.Stop();
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "INVALID_REQUEST",
                    Errors = new List<string> { "Request payload cannot be empty." },
                });
            }

            string? newPhysicalPath = null;
            string? newMediaUrl = null;
            string? oldMediaUrlToDelete = null;

            using SqlConnection conn = await _db.GetOpenConnectionAsync();
            using SqlTransaction tran = conn.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                // Fetch target record with update lock
                string selectSql = @"
                    SELECT Id, Title, MediaUrl, IsImage, IsVideo, DisplayOrder, IsActive 
                    FROM HeroBanners WITH (UPDLOCK) 
                    WHERE Id = @Id AND IsDeleted = 0;";

                DataRow? record = null;
                using (SqlCommand selectCmd = new SqlCommand(selectSql, conn, tran))
                {
                    selectCmd.Parameters.AddWithValue("@Id", bannerId);
                    using SqlDataAdapter da = new SqlDataAdapter(selectCmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        record = dt.Rows[0];
                    }
                }

                if (record == null)
                {
                    tran.Rollback();
                    sw.Stop();
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "HERO_BANNER_NOT_FOUND",
                        Errors = new List<string> { $"Hero Banner with ID '{id}' was not found." },
                    });
                }

                bool isImageRecord = Convert.ToBoolean(record["IsImage"]);
                bool isVideoRecord = Convert.ToBoolean(record["IsVideo"]);
                string currentMediaUrl = record["MediaUrl"].ToString() ?? string.Empty;

                // Validate replacement file if provided
                if (request.MediaFile != null)
                {
                    if (isImageRecord)
                    {
                        var (isValid, errorMessage) = await FileValidationHelper.IsValidImageAsync(request.MediaFile, MaxImageSizeInBytes);
                        if (!isValid)
                        {
                            tran.Rollback();
                            sw.Stop();
                            return BadRequest(new ApiResponse
                            {
                                Success = false,
                                StatusCode = 400,
                                Message = "INVALID_IMAGE",
                                Errors = new List<string> { errorMessage },
                            });
                        }
                    }
                    else if (isVideoRecord)
                    {
                        var (isValid, errorMessage) = await FileValidationHelper.IsValidMp4VideoAsync(request.MediaFile, MaxVideoSizeInBytes);
                        if (!isValid)
                        {
                            tran.Rollback();
                            sw.Stop();
                            return BadRequest(new ApiResponse
                            {
                                Success = false,
                                StatusCode = 400,
                                Message = "INVALID_VIDEO",
                                Errors = new List<string> { errorMessage },
                            });
                        }
                    }

                    // Save new media file before updating record
                    string uploadsDir = GetUploadsDirectory();
                    string ext = Path.GetExtension(request.MediaFile.FileName).ToLowerInvariant();
                    string uniqueName = $"{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}{ext}";
                    newPhysicalPath = Path.Combine(uploadsDir, uniqueName);

                    using (var stream = new FileStream(newPhysicalPath, FileMode.Create))
                    {
                        await request.MediaFile.CopyToAsync(stream);
                    }
                    newMediaUrl = $"/uploads/hero-banner/{uniqueName}";
                    oldMediaUrlToDelete = currentMediaUrl;
                }

                // Build update query
                string updateSql = @"
                    UPDATE HeroBanners
                    SET Title = COALESCE(@Title, Title),
                        DisplayOrder = COALESCE(@DisplayOrder, DisplayOrder),
                        IsActive = COALESCE(@IsActive, IsActive),
                        MediaUrl = COALESCE(@MediaUrl, MediaUrl),
                        UpdatedAt = GETDATE()
                    OUTPUT INSERTED.Id, INSERTED.Title, INSERTED.MediaUrl, INSERTED.IsImage, INSERTED.IsVideo,
                           INSERTED.DisplayOrder, INSERTED.IsActive, INSERTED.CreatedAt, INSERTED.UpdatedAt
                    WHERE Id = @Id AND IsDeleted = 0;";

                HeroBannerItemResponse? updatedResponse = null;
                using (SqlCommand updateCmd = new SqlCommand(updateSql, conn, tran))
                {
                    updateCmd.Parameters.AddWithValue("@Id", bannerId);
                    updateCmd.Parameters.AddWithValue("@Title", (object?)request.Title?.Trim() ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@DisplayOrder", request.DisplayOrder.HasValue ? (object)request.DisplayOrder.Value : DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@IsActive", request.IsActive.HasValue ? (object)request.IsActive.Value : DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@MediaUrl", (object?)newMediaUrl ?? DBNull.Value);

                    using SqlDataReader reader = await updateCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        long uId = reader.GetInt64(0);
                        updatedResponse = new HeroBannerItemResponse
                        {
                            Id = uId,
                            EncryptedId = _encryption.Encrypt(uId),
                            Title = reader.IsDBNull(1) ? null : reader.GetString(1),
                            MediaUrl = reader.GetString(2),
                            IsImage = reader.GetBoolean(3),
                            IsVideo = reader.GetBoolean(4),
                            DisplayOrder = reader.GetInt32(5),
                            IsActive = reader.GetBoolean(6),
                            CreatedAt = reader.GetDateTime(7).ToString("yyyy-MM-dd HH:mm:ss"),
                            UpdatedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8).ToString("yyyy-MM-dd HH:mm:ss"),
                        };
                    }
                }

                tran.Commit();

                // Safe cleanup of replaced old file after commit
                if (!string.IsNullOrWhiteSpace(oldMediaUrlToDelete))
                {
                    SafeDeletePhysicalFile(oldMediaUrlToDelete);
                }

                sw.Stop();
                return Ok(new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "HERO_BANNER_UPDATED",
                    Data = updatedResponse,
                });
            }
            catch (Exception ex)
            {
                try { tran.Rollback(); } catch { }

                // Safe cleanup of newly written file upon DB failure
                if (newPhysicalPath != null && System.IO.File.Exists(newPhysicalPath))
                {
                    try { System.IO.File.Delete(newPhysicalPath); } catch { }
                }

                sw.Stop();
                Logs.Error($"Exception in UpdateHeroMedia for id {id}", ex);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "SERVER_ERROR",
                    Errors = new List<string> { "An internal server error occurred while updating the hero banner." },
                });
            }
        }
        #endregion

        #region DELETE: Delete Hero Media Record and Associated File
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHeroMedia(string id)
        {
            Logs.Info($"DeleteHeroMedia API started for id: {id}");
            var sw = Stopwatch.StartNew();

            if (!TryResolveBannerId(id, out long bannerId))
            {
                sw.Stop();
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "INVALID_ID",
                    Errors = new List<string> { "Invalid Hero Banner ID specified." },
                });
            }

            try
            {
                string selectSql = @"SELECT Id, MediaUrl FROM HeroBanners WHERE Id = @Id AND IsDeleted = 0;";
                DataTable dt = await _db.ExecuteQueryAsync(selectSql, new[] { new SqlParameter("@Id", bannerId) });

                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "HERO_BANNER_NOT_FOUND",
                        Errors = new List<string> { $"Hero Banner with ID '{id}' was not found." },
                    });
                }

                string mediaUrl = dt.Rows[0]["MediaUrl"]?.ToString() ?? string.Empty;

                string deleteSql = @"UPDATE HeroBanners SET IsDeleted = 1, UpdatedAt = GETDATE() WHERE Id = @Id;";
                _db.ExecuteNonQuery(deleteSql, new[] { new SqlParameter("@Id", bannerId) });

                // Safely delete associated physical file
                SafeDeletePhysicalFile(mediaUrl);

                sw.Stop();
                return Ok(new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "HERO_BANNER_DELETED",
                    Data = new { Id = bannerId, EncryptedId = _encryption.Encrypt(bannerId) },
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error($"Exception in DeleteHeroMedia for id {id}", ex);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "SERVER_ERROR",
                    Errors = new List<string> { "An internal server error occurred while deleting the hero banner." },
                });
            }
        }
        #endregion

        #region PATCH: Toggle Active Status
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateHeroMediaStatus(string id, [FromBody] HeroBannerStatusUpdateRequest? request)
        {
            var sw = Stopwatch.StartNew();

            if (!TryResolveBannerId(id, out long bannerId))
            {
                sw.Stop();
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "INVALID_ID",
                    Errors = new List<string> { "Invalid Hero Banner ID specified." },
                });
            }

            if (request == null)
            {
                sw.Stop();
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "INVALID_REQUEST",
                    Errors = new List<string> { "Request body cannot be null." },
                });
            }

            try
            {
                string updateSql = @"
                    UPDATE HeroBanners
                    SET IsActive = @IsActive, UpdatedAt = GETDATE()
                    OUTPUT INSERTED.Id, INSERTED.Title, INSERTED.MediaUrl, INSERTED.IsImage, INSERTED.IsVideo,
                           INSERTED.DisplayOrder, INSERTED.IsActive, INSERTED.CreatedAt, INSERTED.UpdatedAt
                    WHERE Id = @Id AND IsDeleted = 0;";

                DataTable dt = await _db.ExecuteQueryAsync(updateSql, new[]
                {
                    new SqlParameter("@Id", bannerId),
                    new SqlParameter("@IsActive", request.IsActive),
                });

                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "HERO_BANNER_NOT_FOUND",
                        Errors = new List<string> { $"Hero Banner with ID '{id}' was not found." },
                    });
                }

                var response = MapHeroBanner(dt.Rows[0]);
                sw.Stop();

                return Ok(new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "HERO_BANNER_STATUS_UPDATED",
                    Data = response,
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error($"Exception in UpdateHeroMediaStatus for id {id}", ex);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "SERVER_ERROR",
                    Errors = new List<string> { "An internal server error occurred." },
                });
            }
        }
        #endregion
    }
}
