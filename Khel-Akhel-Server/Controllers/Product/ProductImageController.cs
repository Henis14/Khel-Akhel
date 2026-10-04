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

namespace Khel_Akhel_Server.Controllers.Product
{
    [ApiController]
    [Route("api/product-image")]
    public class ProductImageController : ControllerBase
    {
        private readonly DbHelper _db;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService _audit;
        private readonly IWebHostEnvironment _env;

        private const int MaxImagesPerProduct = 7;
        private const int MinImagesPerProduct = 3;
        private const long MaxImageSizeInBytes = 5 * 1024 * 1024; // 5MB

        public ProductImageController(
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
        #endregion

        #region 1. Upload Product Images
        [Authorize(Roles = "Admin")]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadProductImages(
            [FromForm] ProductImageUploadRequest? request
        )
        {
            Logs.Info("ProductImageUpload API started");
            var sw = Stopwatch.StartNew();

            if (request == null || request.Images == null || request.Images.Count == 0)
            {
                sw.Stop();
                Logs.Warning("ProductImageUpload rejected | Reason: Missing files");
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
                !_encryption.TryDecrypt(request.EncryptedProductId, out long productId)
                || productId <= 0
            )
            {
                sw.Stop();
                Logs.Warning(
                    $"ProductImageUpload rejected | Reason: Invalid EncryptedProductId: {request.EncryptedProductId}"
                );
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_REQUEST",
                        Errors = new List<string> { "Invalid or tampered Product ID." },
                    }
                );
            }

            // Check Product Existence
            string checkProductSql =
                "SELECT COUNT(1) FROM drs_product_mst WHERE ID = @ProductId AND IsDeleted = 0;";
            var checkParams = new SqlParameter[] { new SqlParameter("@ProductId", productId) };
            object? productExistsObj = await _db.ExecuteScalarAsync(checkProductSql, checkParams);
            int productExists =
                (productExistsObj != null && productExistsObj != DBNull.Value)
                    ? Convert.ToInt32(productExistsObj)
                    : 0;

            if (productExists == 0)
            {
                sw.Stop();
                Logs.Warning(
                    $"ProductImageUpload failed | Product not found. ProductId: {productId}"
                );
                return NotFound(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "PRODUCT_NOT_FOUND",
                        Errors = new List<string>
                        {
                            "Target product does not exist or has been deleted.",
                        },
                    }
                );
            }

            // Check Max Allowed Images
            string countSql =
                "SELECT COUNT(1) FROM drs_product_image_mst WHERE product_id = @ProductId AND IsDeleted = 0;";
            object? countObj = await _db.ExecuteScalarAsync(
                countSql,
                new SqlParameter[] { new SqlParameter("@ProductId", productId) }
            );
            int existingCount =
                (countObj != null && countObj != DBNull.Value) ? Convert.ToInt32(countObj) : 0;

            if (existingCount + request.Images.Count > MaxImagesPerProduct)
            {
                sw.Stop();
                Logs.Warning(
                    $"ProductImageUpload rejected | Exceeded limit. Existing: {existingCount}, New: {request.Images.Count}, Max: {MaxImagesPerProduct}"
                );
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "MAXIMUM_PRODUCT_IMAGES_EXCEEDED",
                        Errors = new List<string>
                        {
                            $"Cannot upload more than {MaxImagesPerProduct} images per product. Currently active: {existingCount}.",
                        },
                    }
                );
            }

            // Validate all image files prior to disk write
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
                    validationErrors.Add($"File '{file.FileName}': {errorMessage}");
                }
            }

            if (validationErrors.Count > 0)
            {
                sw.Stop();
                Logs.Warning(
                    $"ProductImageUpload validation failed | Errors: {string.Join("; ", validationErrors)}"
                );
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_IMAGE",
                        Errors = validationErrors,
                    }
                );
            }

            string uploadsRoot = Path.Combine(
                _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads",
                "products"
            );
            if (!Directory.Exists(uploadsRoot))
            {
                Directory.CreateDirectory(uploadsRoot);
            }

            var createdPhysicalPaths = new List<string>();
            var uploadedImageResponses = new List<ProductImageResponse>();

            using SqlConnection conn = await _db.GetOpenConnectionAsync();
            using SqlTransaction tran = conn.BeginTransaction();

            try
            {
                // Check if default image exists for product
                string hasDefaultSql =
                    "SELECT COUNT(1) FROM drs_product_image_mst WHERE product_id = @ProductId AND IsDefault = 1 AND IsDeleted = 0;";
                using SqlCommand hasDefaultCmd = new SqlCommand(hasDefaultSql, conn, tran);
                hasDefaultCmd.Parameters.AddWithValue("@ProductId", productId);
                int defaultCount = Convert.ToInt32(await hasDefaultCmd.ExecuteScalarAsync());
                bool hasDefault = defaultCount > 0;

                // Get current max display order
                string maxOrderSql =
                    "SELECT ISNULL(MAX(DisplayOrder), 0) FROM drs_product_image_mst WHERE product_id = @ProductId AND IsDeleted = 0;";
                using SqlCommand maxOrderCmd = new SqlCommand(maxOrderSql, conn, tran);
                maxOrderCmd.Parameters.AddWithValue("@ProductId", productId);
                int currentDisplayOrder = Convert.ToInt32(await maxOrderCmd.ExecuteScalarAsync());

                for (int i = 0; i < request.Images.Count; i++)
                {
                    var file = request.Images[i];
                    string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    string safeFileName = $"{productId}_{Guid.NewGuid()}{extension}";
                    string physicalFilePath = Path.Combine(uploadsRoot, safeFileName);
                    string relativeDbPath = $"/uploads/products/{safeFileName}";

                    // Save physical file
                    using (var stream = new FileStream(physicalFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    createdPhysicalPaths.Add(physicalFilePath);

                    currentDisplayOrder++;
                    bool isDefault = !hasDefault && (i == 0);

                    string insertSql =
                        @"
                        INSERT INTO drs_product_image_mst 
                        (product_id, ImagePath, DisplayOrder, IsDefault, IsActive, IsDeleted, CreatedDate)
                        OUTPUT INSERTED.ID
                        VALUES 
                        (@ProductId, @ImagePath, @DisplayOrder, @IsDefault, 1, 0, GETDATE());";

                    using SqlCommand insertCmd = new SqlCommand(insertSql, conn, tran);
                    insertCmd.Parameters.AddWithValue("@ProductId", productId);
                    insertCmd.Parameters.AddWithValue("@ImagePath", relativeDbPath);
                    insertCmd.Parameters.AddWithValue("@DisplayOrder", currentDisplayOrder);
                    insertCmd.Parameters.AddWithValue("@IsDefault", isDefault ? 1 : 0);

                    object? newIdObj = await insertCmd.ExecuteScalarAsync();
                    long newImageId = Convert.ToInt64(newIdObj);

                    uploadedImageResponses.Add(
                        new ProductImageResponse
                        {
                            EncryptedImageId = _encryption.Encrypt(newImageId),
                            EncryptedProductId = request.EncryptedProductId,
                            ImagePath = relativeDbPath,
                            DisplayOrder = currentDisplayOrder,
                            IsDefault = isDefault,
                            IsActive = true,
                            CreatedDate = DateTimeFormat.Format(DateTime.Now),
                        }
                    );
                }

                await tran.CommitAsync();

                long adminUserId = GetAuthenticatedUserId();
                await _audit.InsertAuditAsync(
                    adminUserId,
                    "ProductImage",
                    "Upload",
                    "drs_product_image_mst",
                    productId,
                    null,
                    $"Uploaded {request.Images.Count} images for ProductId: {productId}",
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info(
                    $"ProductImageUpload completed successfully | ProductId: {productId} | ImagesCount: {request.Images.Count} | ElapsedMs: {sw.ElapsedMilliseconds}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PRODUCT_IMAGES_UPLOADED",
                        Data = new ProductImageUploadResponse
                        {
                            EncryptedProductId = request.EncryptedProductId,
                            Images = uploadedImageResponses,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();

                // Rollback physical files written to disk
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
                            $"Failed to clean up orphaned image file '{filePath}' during rollback",
                            fileEx
                        );
                    }
                }

                sw.Stop();
                Logs.Error($"ProductImageUpload failed | Exception: {ex.Message}", ex);

                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while uploading product images.",
                        },
                    }
                );
            }
        }
        #endregion

        #region 2. Get Product Images
        [HttpGet("{encryptedProductId}")]
        public async Task<IActionResult> GetProductImages(string encryptedProductId)
        {
            Logs.Info($"GetProductImages API started | EncryptedProductId: {encryptedProductId}");

            if (!_encryption.TryDecrypt(encryptedProductId, out long productId) || productId <= 0)
            {
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_REQUEST",
                        Errors = new List<string> { "Invalid or tampered Product ID." },
                    }
                );
            }

            string sql =
                @"
                SELECT ID, product_id, ImagePath, DisplayOrder, IsDefault, IsActive, CreatedDate
                FROM drs_product_image_mst
                WHERE product_id = @ProductId AND IsDeleted = 0
                ORDER BY IsDefault DESC, DisplayOrder ASC, ID ASC;";

            var p = new SqlParameter[] { new SqlParameter("@ProductId", productId) };
            DataTable dt = await _db.ExecuteQueryAsync(sql, p);

            var images = new List<ProductImageResponse>();
            foreach (DataRow row in dt.Rows)
            {
                long imageId = Convert.ToInt64(row["ID"]);
                images.Add(
                    new ProductImageResponse
                    {
                        EncryptedImageId = _encryption.Encrypt(imageId),
                        EncryptedProductId = encryptedProductId,
                        ImagePath = Convert.ToString(row["ImagePath"]) ?? string.Empty,
                        DisplayOrder = Convert.ToInt32(row["DisplayOrder"]),
                        IsDefault = Convert.ToBoolean(row["IsDefault"]),
                        IsActive = Convert.ToBoolean(row["IsActive"]),
                        CreatedDate = DateTimeFormat.Format(row["CreatedDate"] as DateTime?),
                    }
                );
            }

            return Ok(
                new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "PRODUCT_IMAGES_RETRIEVED",
                    Data = images,
                }
            );
        }
        #endregion

        #region 3. Set Default Image
        [Authorize(Roles = "Admin")]
        [HttpPatch("set-default")]
        public async Task<IActionResult> SetDefaultImage([FromBody] SetDefaultImageRequest? request)
        {
            Logs.Info("SetDefaultImage API started");

            if (request == null || string.IsNullOrWhiteSpace(request.EncryptedImageId))
            {
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_REQUEST",
                        Errors = new List<string> { "EncryptedImageId is required." },
                    }
                );
            }

            if (!_encryption.TryDecrypt(request.EncryptedImageId, out long imageId) || imageId <= 0)
            {
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

            using SqlConnection conn = await _db.GetOpenConnectionAsync();

            string findImgSql =
                "SELECT product_id FROM drs_product_image_mst WHERE ID = @ImageId AND IsDeleted = 0;";
            using SqlCommand findCmd = new SqlCommand(findImgSql, conn);
            findCmd.Parameters.AddWithValue("@ImageId", imageId);
            object? productIdObj = await findCmd.ExecuteScalarAsync();

            if (productIdObj == null || productIdObj == DBNull.Value)
            {
                return NotFound(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "PRODUCT_IMAGE_NOT_FOUND",
                        Errors = new List<string>
                        {
                            "Product image record not found or has been deleted.",
                        },
                    }
                );
            }

            long productId = Convert.ToInt64(productIdObj);

            using SqlTransaction tran = conn.BeginTransaction();
            try
            {
                string resetDefaultSql =
                    "UPDATE drs_product_image_mst SET IsDefault = 0, ModifiedDate = GETDATE() WHERE product_id = @ProductId AND IsDeleted = 0;";
                using SqlCommand resetCmd = new SqlCommand(resetDefaultSql, conn, tran);
                resetCmd.Parameters.AddWithValue("@ProductId", productId);
                await resetCmd.ExecuteNonQueryAsync();

                string setDefaultSql =
                    "UPDATE drs_product_image_mst SET IsDefault = 1, ModifiedDate = GETDATE() WHERE ID = @ImageId AND IsDeleted = 0;";
                using SqlCommand setCmd = new SqlCommand(setDefaultSql, conn, tran);
                setCmd.Parameters.AddWithValue("@ImageId", imageId);
                await setCmd.ExecuteNonQueryAsync();

                await tran.CommitAsync();

                long adminUserId = GetAuthenticatedUserId();
                await _audit.InsertAuditAsync(
                    adminUserId,
                    "ProductImage",
                    "SetDefault",
                    "drs_product_image_mst",
                    imageId,
                    null,
                    $"Set ImageId: {imageId} as default for ProductId: {productId}",
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                Logs.Info(
                    $"SetDefaultImage completed successfully | ImageId: {imageId} | ProductId: {productId}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PRODUCT_IMAGE_SET_DEFAULT",
                        Data = new
                        {
                            EncryptedImageId = request.EncryptedImageId,
                            EncryptedProductId = _encryption.Encrypt(productId),
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                Logs.Error($"SetDefaultImage failed | Exception: {ex.Message}", ex);
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

        #region 4. Delete Product Image
        [Authorize(Roles = "Admin")]
        [HttpDelete("{encryptedImageId}")]
        public async Task<IActionResult> DeleteProductImage(string encryptedImageId)
        {
            Logs.Info($"DeleteProductImage API started | EncryptedImageId: {encryptedImageId}");

            if (!_encryption.TryDecrypt(encryptedImageId, out long imageId) || imageId <= 0)
            {
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

            using SqlConnection conn = await _db.GetOpenConnectionAsync();

            string findImgSql =
                "SELECT product_id, ImagePath, IsDefault FROM drs_product_image_mst WHERE ID = @ImageId AND IsDeleted = 0;";
            using SqlCommand findCmd = new SqlCommand(findImgSql, conn);
            findCmd.Parameters.AddWithValue("@ImageId", imageId);

            long productId = 0;
            string imagePath = string.Empty;
            bool wasDefault = false;

            using (SqlDataReader reader = await findCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    productId = reader.GetInt64(0);
                    imagePath = reader.GetString(1);
                    wasDefault = reader.GetBoolean(2);
                }
                else
                {
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "PRODUCT_IMAGE_NOT_FOUND",
                            Errors = new List<string>
                            {
                                "Image record not found or already deleted.",
                            },
                        }
                    );
                }
            }

            string countSql =
                "SELECT COUNT(1) FROM drs_product_image_mst WHERE product_id = @ProductId AND IsDeleted = 0;";
            using SqlCommand countCmd = new SqlCommand(countSql, conn);
            countCmd.Parameters.AddWithValue("@ProductId", productId);
            int currentCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            if (currentCount <= MinImagesPerProduct)
            {
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "MINIMUM_PRODUCT_IMAGES_REQUIRED",
                        Errors = new List<string>
                        {
                            $"Minimum {MinImagesPerProduct} images are required for a product. Deleting an image is not allowed unless another image is added.",
                        },
                    }
                );
            }

            using SqlTransaction tran = conn.BeginTransaction();
            try
            {
                // Soft delete current image
                string deleteSql =
                    "UPDATE drs_product_image_mst SET IsDeleted = 1, ModifiedDate = GETDATE() WHERE ID = @ImageId;";
                using SqlCommand deleteCmd = new SqlCommand(deleteSql, conn, tran);
                deleteCmd.Parameters.AddWithValue("@ImageId", imageId);
                await deleteCmd.ExecuteNonQueryAsync();

                // If deleted image was default, promote next active image
                if (wasDefault)
                {
                    string promoteSql =
                        @"
                        UPDATE drs_product_image_mst 
                        SET IsDefault = 1, ModifiedDate = GETDATE() 
                        WHERE ID = (
                            SELECT TOP 1 ID 
                            FROM drs_product_image_mst 
                            WHERE product_id = @ProductId AND IsDeleted = 0 
                            ORDER BY DisplayOrder ASC, ID ASC
                        );";
                    using SqlCommand promoteCmd = new SqlCommand(promoteSql, conn, tran);
                    promoteCmd.Parameters.AddWithValue("@ProductId", productId);
                    await promoteCmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();

                // Remove physical file from disk
                if (!string.IsNullOrWhiteSpace(imagePath))
                {
                    string relativePath = imagePath.TrimStart('/', '\\');
                    string physicalPath = Path.Combine(
                        _env.WebRootPath
                            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                        relativePath
                    );
                    try
                    {
                        if (System.IO.File.Exists(physicalPath))
                        {
                            System.IO.File.Delete(physicalPath);
                        }
                    }
                    catch (Exception fileEx)
                    {
                        Logs.Error(
                            $"Failed to delete physical file '{physicalPath}' during image deletion",
                            fileEx
                        );
                    }
                }

                long adminUserId = GetAuthenticatedUserId();
                await _audit.InsertAuditAsync(
                    adminUserId,
                    "ProductImage",
                    "Delete",
                    "drs_product_image_mst",
                    imageId,
                    null,
                    $"Deleted image ID: {imageId} for ProductId: {productId}",
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                Logs.Info(
                    $"DeleteProductImage completed successfully | ImageId: {imageId} | ProductId: {productId}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PRODUCT_IMAGE_DELETED",
                        Data = new
                        {
                            EncryptedImageId = encryptedImageId,
                            EncryptedProductId = _encryption.Encrypt(productId),
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                Logs.Error($"DeleteProductImage failed | Exception: {ex.Message}", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while deleting product image.",
                        },
                    }
                );
            }
        }
        #endregion
    }
}
