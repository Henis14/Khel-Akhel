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
    [Route("api/product-category")]
    public class ProductCategoryController : ControllerBase
    {
        private readonly DbHelper _db;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService _audit;

        public ProductCategoryController(
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

        #region Category - Create
        [Authorize(Roles = "Admin")]
        [HttpPost("create")]
        public async Task<IActionResult> ProductCategoryCreate(
            [FromBody] ProductCategoryCreateRequest? request
        )
        {
            Logs.Info("ProductCategoryCreate API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning("ProductCategoryCreate rejected | Reason: Null request body");
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

                request.CategoryName = request.CategoryName?.Trim() ?? string.Empty;
                request.Description = request.Description?.Trim() ?? string.Empty;

                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.CategoryName))
                    errors.Add("Category name is required.");
                else if (!ValidationHelper.IsValidCategoryName(request.CategoryName))
                    errors.Add("Category name must be between 2 and 100 characters.");

                if (!ValidationHelper.IsValidCategoryDescription(request.Description))
                    errors.Add("Description cannot exceed 500 characters.");

                if (errors.Any())
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductCategoryCreate rejected | Validation errors: {string.Join(", ", errors)}"
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

                // Uniqueness check for CategoryName where IsDeleted = 0
                string checkQuery =
                    @"
                    SELECT COUNT(1)
                    FROM drs_product_category_mst WITH (NOLOCK)
                    WHERE CategoryName = @CategoryName AND IsDeleted = 0";

                int exists = Convert.ToInt32(
                    _db.ExecuteScalar(
                        checkQuery,
                        new[] { new SqlParameter("@CategoryName", request.CategoryName) }
                    ) ?? 0
                );

                if (exists > 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductCategoryCreate duplicate category name: {request.CategoryName}"
                    );
                    return Conflict(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 409,
                            Message = "CATEGORY_ALREADY_EXISTS",
                            Errors = new List<string>
                            {
                                "Product category with given name already exists.",
                            },
                        }
                    );
                }

                string insertQuery =
                    @"
                    INSERT INTO drs_product_category_mst
                    (CategoryName, Description, DisplayOrder, IsActive, IsDeleted, CreatedDate)
                    OUTPUT INSERTED.ID
                    VALUES
                    (@CategoryName, @Description, @DisplayOrder, 1, 0, GETDATE())";

                object? result = _db.ExecuteScalar(
                    insertQuery,
                    new[]
                    {
                        new SqlParameter("@CategoryName", request.CategoryName),
                        new SqlParameter(
                            "@Description",
                            (object?)request.Description ?? DBNull.Value
                        ),
                        new SqlParameter("@DisplayOrder", request.DisplayOrder),
                    }
                );

                long categoryId = Convert.ToInt64(result);
                string encryptedId = _encryption.Encrypt(categoryId);

                var responseData = new ProductCategoryResponse
                {
                    EncryptedCategoryId = encryptedId,
                    CategoryName = request.CategoryName ?? string.Empty,
                    Description = request.Description ?? string.Empty,
                    DisplayOrder = request.DisplayOrder,
                    IsActive = true,
                    CreatedDate = DateTimeFormat.Format(DateTime.Now),
                };

                await _audit.InsertAuditAsync(
                    GetAuthenticatedUserId(),
                    "ProductCategory",
                    "ProductCategoryCreate",
                    "drs_product_category_mst",
                    categoryId,
                    null,
                    System.Text.Json.JsonSerializer.Serialize(responseData),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"ProductCategoryCreate completed | CategoryId:{categoryId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "CATEGORY_CREATED",
                        Data = responseData,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ProductCategoryCreate API", ex);
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

        #region Category - Update
        [Authorize(Roles = "Admin")]
        [HttpPatch("update")]
        public async Task<IActionResult> ProductCategoryUpdate(
            [FromBody] ProductCategoryUpdateRequest? request
        )
        {
            Logs.Info("ProductCategoryUpdate API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.EncryptedCategoryId))
                {
                    sw.Stop();
                    Logs.Warning(
                        "ProductCategoryUpdate rejected | Missing category ID or request body"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = new List<string> { "Encrypted category ID is required." },
                        }
                    );
                }

                if (
                    !_encryption.TryDecrypt(request.EncryptedCategoryId, out long categoryId)
                    || categoryId <= 0
                )
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductCategoryUpdate rejected | Invalid encrypted category ID: {request.EncryptedCategoryId}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string>
                            {
                                "Invalid or tampered encrypted category ID.",
                            },
                        }
                    );
                }

                string selectQuery =
                    @"
                    SELECT TOP 1 ID, CategoryName, Description, DisplayOrder, IsActive
                    FROM drs_product_category_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(
                    selectQuery,
                    new[] { new SqlParameter("@ID", categoryId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductCategoryUpdate rejected | Category not found | CategoryId:{categoryId}"
                    );
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "CATEGORY_NOT_FOUND",
                            Errors = new List<string> { "Product category not found." },
                        }
                    );
                }

                DataRow existing = dt.Rows[0];
                string oldName = existing["CategoryName"].ToString() ?? "";
                string newName =
                    request.CategoryName != null ? request.CategoryName.Trim() : oldName;
                string newDesc =
                    request.Description != null
                        ? request.Description.Trim()
                        : existing["Description"].ToString() ?? "";
                int newOrder = request.DisplayOrder ?? Convert.ToInt32(existing["DisplayOrder"]);
                bool newIsActive = request.IsActive ?? Convert.ToBoolean(existing["IsActive"]);

                if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
                {
                    string dupQuery =
                        @"
                        SELECT COUNT(1)
                        FROM drs_product_category_mst WITH (NOLOCK)
                        WHERE CategoryName = @CategoryName AND ID <> @ID AND IsDeleted = 0";

                    int dup = Convert.ToInt32(
                        _db.ExecuteScalar(
                            dupQuery,
                            new[]
                            {
                                new SqlParameter("@CategoryName", newName),
                                new SqlParameter("@ID", categoryId),
                            }
                        ) ?? 0
                    );

                    if (dup > 0)
                    {
                        sw.Stop();
                        Logs.Warning(
                            $"ProductCategoryUpdate rejected | Duplicate category name '{newName}' for CategoryId:{categoryId}"
                        );
                        return Conflict(
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 409,
                                Message = "CATEGORY_ALREADY_EXISTS",
                                Errors = new List<string>
                                {
                                    "Product category with given name already exists.",
                                },
                            }
                        );
                    }
                }

                string updateQuery =
                    @"
                    UPDATE drs_product_category_mst
                    SET CategoryName = @CategoryName,
                        Description = @Description,
                        DisplayOrder = @DisplayOrder,
                        IsActive = @IsActive,
                        ModifiedDate = GETDATE()
                    WHERE ID = @ID AND IsDeleted = 0";

                _db.ExecuteNonQuery(
                    updateQuery,
                    new[]
                    {
                        new SqlParameter("@CategoryName", newName),
                        new SqlParameter("@Description", (object?)newDesc ?? DBNull.Value),
                        new SqlParameter("@DisplayOrder", newOrder),
                        new SqlParameter("@IsActive", newIsActive),
                        new SqlParameter("@ID", categoryId),
                    }
                );

                var responseData = new ProductCategoryResponse
                {
                    EncryptedCategoryId = request.EncryptedCategoryId ?? string.Empty,
                    CategoryName = newName ?? string.Empty,
                    Description = newDesc ?? string.Empty,
                    DisplayOrder = newOrder,
                    IsActive = newIsActive,
                    CreatedDate = DateTimeFormat.Format(DateTime.Now),
                };

                var oldAuditData = new
                {
                    EncryptedCategoryId = request.EncryptedCategoryId ?? string.Empty,
                    CategoryName = oldName,
                    Description = existing["Description"].ToString() ?? string.Empty,
                    DisplayOrder = Convert.ToInt32(existing["DisplayOrder"]),
                    IsActive = Convert.ToBoolean(existing["IsActive"]),
                };

                await _audit.InsertAuditAsync(
                    GetAuthenticatedUserId(),
                    "ProductCategory",
                    "ProductCategoryUpdate",
                    "drs_product_category_mst",
                    categoryId,
                    System.Text.Json.JsonSerializer.Serialize(oldAuditData),
                    System.Text.Json.JsonSerializer.Serialize(responseData),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"ProductCategoryUpdate completed | CategoryId:{categoryId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "CATEGORY_UPDATED",
                        Data = responseData,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ProductCategoryUpdate API", ex);
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

        #region Category - Get By Id
        [AllowAnonymous]
        [HttpGet("{encryptedId}")]
        public IActionResult GetProductCategoryById([FromRoute] string encryptedId)
        {
            Logs.Info("GetProductCategoryById API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (!_encryption.TryDecrypt(encryptedId, out long categoryId) || categoryId <= 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"GetProductCategoryById rejected | Invalid encrypted category ID: {encryptedId}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string>
                            {
                                "Invalid or tampered encrypted category ID.",
                            },
                        }
                    );
                }

                string query =
                    @"
                    SELECT TOP 1 ID, CategoryName, Description, DisplayOrder, IsActive, CreatedDate
                    FROM drs_product_category_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(
                    query,
                    new[] { new SqlParameter("@ID", categoryId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"GetProductCategoryById rejected | Category not found | CategoryId:{categoryId}"
                    );
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "CATEGORY_NOT_FOUND",
                            Errors = new List<string> { "Product category not found." },
                        }
                    );
                }

                DataRow row = dt.Rows[0];
                var categoryResponse = new ProductCategoryResponse
                {
                    EncryptedCategoryId = _encryption.Encrypt(Convert.ToInt64(row["ID"])),
                    CategoryName = row["CategoryName"].ToString() ?? "",
                    Description = row["Description"].ToString() ?? "",
                    DisplayOrder = Convert.ToInt32(row["DisplayOrder"]),
                    IsActive = Convert.ToBoolean(row["IsActive"]),
                    CreatedDate = DateTimeFormat.Format(Convert.ToDateTime(row["CreatedDate"])),
                };

                sw.Stop();
                Logs.Info($"GetProductCategoryById completed | CategoryId:{categoryId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = categoryResponse,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetProductCategoryById API", ex);
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

        #region Category - Get List
        [AllowAnonymous]
        [HttpGet("list")]
        public IActionResult GetAllProductCategory()
        {
            Logs.Info("GetAllProductCategory API started");
            var sw = Stopwatch.StartNew();

            try
            {
                string query =
                    @"
                    SELECT ID, CategoryName, Description, DisplayOrder, IsActive, CreatedDate
                    FROM drs_product_category_mst WITH (NOLOCK)
                    WHERE IsDeleted = 0 AND IsActive = 1
                    ORDER BY DisplayOrder ASC, CategoryName ASC";

                DataTable dt = _db.ExecuteQuery(query);

                var categoryList = new List<ProductCategoryResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    categoryList.Add(
                        new ProductCategoryResponse
                        {
                            EncryptedCategoryId = _encryption.Encrypt(Convert.ToInt64(row["ID"])),
                            CategoryName = row["CategoryName"].ToString() ?? "",
                            Description = row["Description"].ToString() ?? "",
                            DisplayOrder = Convert.ToInt32(row["DisplayOrder"]),
                            IsActive = Convert.ToBoolean(row["IsActive"]),
                            CreatedDate = DateTimeFormat.Format(
                                Convert.ToDateTime(row["CreatedDate"])
                            ),
                        }
                    );
                }

                sw.Stop();
                Logs.Info($"GetAllProductCategory completed | Count:{categoryList.Count}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = categoryList,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetAllProductCategory API", ex);
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

        #region Category - Delete
        [Authorize(Roles = "Admin")]
        [HttpDelete("delete/{encryptedCategoryId?}")]
        [HttpPatch("delete/{encryptedCategoryId?}")]
        public async Task<IActionResult> ProductCategorySoftDelete(
            [FromRoute] string? encryptedCategoryId = null,
            [FromQuery] string? queryEncryptedCategoryId = null
        )
        {
            Logs.Info("ProductCategorySoftDelete API started");
            var sw = Stopwatch.StartNew();

            try
            {
                string targetEncryptedId = !string.IsNullOrWhiteSpace(encryptedCategoryId)
                    ? encryptedCategoryId
                    : (queryEncryptedCategoryId ?? string.Empty);

                if (
                    string.IsNullOrWhiteSpace(targetEncryptedId)
                    || !_encryption.TryDecrypt(targetEncryptedId, out long categoryId)
                    || categoryId <= 0
                )
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductCategorySoftDelete rejected | Invalid encrypted category ID: {targetEncryptedId}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string>
                            {
                                "Invalid or tampered encrypted category ID.",
                            },
                        }
                    );
                }

                string checkQuery =
                    @"
                    SELECT TOP 1 ID, CategoryName, Description, DisplayOrder, IsActive
                    FROM drs_product_category_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(
                    checkQuery,
                    new[] { new SqlParameter("@ID", categoryId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductCategorySoftDelete rejected | Category not found | CategoryId:{categoryId}"
                    );
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "CATEGORY_NOT_FOUND",
                            Errors = new List<string>
                            {
                                "Product category not found or already deleted.",
                            },
                        }
                    );
                }

                DataRow existingRow = dt.Rows[0];

                // Referential integrity check 1: Active child subcategories
                string checkChildSubQuery =
                    @"
                    SELECT COUNT(1)
                    FROM drs_product_subcategory_mst WITH (NOLOCK)
                    WHERE product_category_id = @ID AND IsDeleted = 0";
                int childSubCount = Convert.ToInt32(
                    _db.ExecuteScalar(
                        checkChildSubQuery,
                        new[] { new SqlParameter("@ID", categoryId) }
                    ) ?? 0
                );
                if (childSubCount > 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductCategorySoftDelete rejected | Category {categoryId} contains active subcategories ({childSubCount})"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "CANNOT_DELETE_PARENT_CATEGORY",
                            Errors = new List<string>
                            {
                                "Cannot delete category because it contains active subcategories. Please remove or reassign them first.",
                            },
                        }
                    );
                }

                // Referential integrity check: Active products assigned to this category
                string checkProdQuery =
                    @"
                    SELECT COUNT(1)
                    FROM drs_product_mst WITH (NOLOCK)
                    WHERE product_category_id = @ID AND IsDeleted = 0";
                int prodCount = Convert.ToInt32(
                    _db.ExecuteScalar(checkProdQuery, new[] { new SqlParameter("@ID", categoryId) })
                        ?? 0
                );
                if (prodCount > 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductCategorySoftDelete rejected | Category {categoryId} contains active products ({prodCount})"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "CANNOT_DELETE_PARENT_CATEGORY",
                            Errors = new List<string>
                            {
                                "Cannot delete category because active products are assigned to it.",
                            },
                        }
                    );
                }

                string deleteQuery =
                    @"
                    UPDATE drs_product_category_mst
                    SET IsDeleted = 1,
                        IsActive = 0,
                        ModifiedDate = GETDATE()
                    WHERE ID = @ID";

                _db.ExecuteNonQuery(deleteQuery, new[] { new SqlParameter("@ID", categoryId) });

                await _audit.InsertAuditAsync(
                    GetAuthenticatedUserId(),
                    "ProductCategory",
                    "ProductCategorySoftDelete",
                    "drs_product_category_mst",
                    categoryId,
                    System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            CategoryName = existingRow["CategoryName"].ToString(),
                            IsActive = Convert.ToBoolean(existingRow["IsActive"]),
                            IsDeleted = false,
                        }
                    ),
                    System.Text.Json.JsonSerializer.Serialize(
                        new { IsActive = false, IsDeleted = true }
                    ),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info(
                    $"ProductCategorySoftDelete completed successfully | CategoryId:{categoryId}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "CATEGORY_DELETED",
                        Data = null,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ProductCategorySoftDelete API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while deleting product category.",
                        },
                    }
                );
            }
        }
        #endregion

        #region SubCategory - Create
        [Authorize(Roles = "Admin")]
        [HttpPost("subcategory/create")]
        public async Task<IActionResult> ProductSubCategoryCreate(
            [FromBody] ProductSubCategoryCreateRequest? request
        )
        {
            Logs.Info("ProductSubCategoryCreate API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning("ProductSubCategoryCreate rejected | Reason: Null request body");
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
                    !_encryption.TryDecrypt(request.EncryptedCategoryId, out long categoryId)
                    || categoryId <= 0
                )
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductSubCategoryCreate rejected | Invalid parent Category ID: {request.EncryptedCategoryId}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string> { "Invalid parent Category ID." },
                        }
                    );
                }

                // Verify parent Category exists and is active
                string checkCatQuery =
                    @"
                    SELECT TOP 1 CategoryName FROM drs_product_category_mst WITH (NOLOCK)
                    WHERE ID = @CategoryID AND IsDeleted = 0 AND IsActive = 1";
                object? catNameObj = _db.ExecuteScalar(
                    checkCatQuery,
                    new[] { new SqlParameter("@CategoryID", categoryId) }
                );
                if (catNameObj == null)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductSubCategoryCreate rejected | Parent category not found or inactive | CategoryId:{categoryId}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "CATEGORY_NOT_FOUND",
                            Errors = new List<string>
                            {
                                "Parent product category does not exist or is inactive.",
                            },
                        }
                    );
                }
                string parentCategoryName = catNameObj.ToString() ?? "";

                request.SubCategoryName = request.SubCategoryName?.Trim() ?? string.Empty;
                request.Description = request.Description?.Trim() ?? string.Empty;

                var errors = new List<string>();
                if (string.IsNullOrWhiteSpace(request.SubCategoryName))
                    errors.Add("SubCategory name is required.");
                else if (!ValidationHelper.IsValidSubCategoryName(request.SubCategoryName))
                    errors.Add("SubCategory name must be between 2 and 100 characters.");

                if (!ValidationHelper.IsValidSubCategoryDescription(request.Description))
                    errors.Add("Description cannot exceed 500 characters.");

                if (errors.Any())
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductSubCategoryCreate rejected | Validation errors: {string.Join(", ", errors)}"
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

                // Uniqueness check for SubCategoryName under the same Category
                string checkDupQuery =
                    @"
                    SELECT COUNT(1) FROM drs_product_subcategory_mst WITH (NOLOCK)
                    WHERE product_category_id = @CategoryId AND SubCategoryName = @SubCategoryName AND IsDeleted = 0";
                int dup = Convert.ToInt32(
                    _db.ExecuteScalar(
                        checkDupQuery,
                        new[]
                        {
                            new SqlParameter("@CategoryId", categoryId),
                            new SqlParameter("@SubCategoryName", request.SubCategoryName),
                        }
                    ) ?? 0
                );

                if (dup > 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductSubCategoryCreate rejected | Duplicate subcategory name '{request.SubCategoryName}' under CategoryId:{categoryId}"
                    );
                    return Conflict(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 409,
                            Message = "SUBCATEGORY_ALREADY_EXISTS",
                            Errors = new List<string>
                            {
                                "SubCategory with given name already exists under this Category.",
                            },
                        }
                    );
                }

                string insertQuery =
                    @"
                    INSERT INTO drs_product_subcategory_mst
                    (product_category_id, SubCategoryName, Description, DisplayOrder, IsActive, IsDeleted, CreatedDate)
                    OUTPUT INSERTED.ID
                    VALUES
                    (@CategoryId, @SubCategoryName, @Description, @DisplayOrder, 1, 0, GETDATE())";

                object? result = _db.ExecuteScalar(
                    insertQuery,
                    new[]
                    {
                        new SqlParameter("@CategoryId", categoryId),
                        new SqlParameter("@SubCategoryName", request.SubCategoryName),
                        new SqlParameter(
                            "@Description",
                            (object?)request.Description ?? DBNull.Value
                        ),
                        new SqlParameter("@DisplayOrder", request.DisplayOrder),
                    }
                );

                long subCategoryId = Convert.ToInt64(result);
                string encryptedSubCatId = _encryption.Encrypt(subCategoryId);

                var responseData = new ProductSubCategoryResponse
                {
                    EncryptedSubCategoryId = encryptedSubCatId,
                    EncryptedCategoryId = request.EncryptedCategoryId,
                    CategoryName = parentCategoryName,
                    SubCategoryName = request.SubCategoryName,
                    Description = request.Description ?? string.Empty,
                    DisplayOrder = request.DisplayOrder,
                    IsActive = true,
                    CreatedDate = DateTimeFormat.Format(DateTime.Now),
                };

                await _audit.InsertAuditAsync(
                    GetAuthenticatedUserId(),
                    "ProductSubCategory",
                    "ProductSubCategoryCreate",
                    "drs_product_subcategory_mst",
                    subCategoryId,
                    null,
                    System.Text.Json.JsonSerializer.Serialize(responseData),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"ProductSubCategoryCreate completed | SubCategoryId:{subCategoryId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUBCATEGORY_CREATED",
                        Data = responseData,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ProductSubCategoryCreate API", ex);
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

        #region SubCategory - Update
        [Authorize(Roles = "Admin")]
        [HttpPatch("subcategory/update")]
        public async Task<IActionResult> ProductSubCategoryUpdate(
            [FromBody] ProductSubCategoryUpdateRequest? request
        )
        {
            Logs.Info("ProductSubCategoryUpdate API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.EncryptedSubCategoryId))
                {
                    sw.Stop();
                    Logs.Warning(
                        "ProductSubCategoryUpdate rejected | Missing subcategory ID or request body"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = new List<string> { "Encrypted SubCategory ID is required." },
                        }
                    );
                }

                if (
                    !_encryption.TryDecrypt(request.EncryptedSubCategoryId, out long subCategoryId)
                    || subCategoryId <= 0
                )
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductSubCategoryUpdate rejected | Invalid encrypted SubCategory ID: {request.EncryptedSubCategoryId}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string>
                            {
                                "Invalid or tampered encrypted SubCategory ID.",
                            },
                        }
                    );
                }

                string selectQuery =
                    @"
                    SELECT TOP 1 sc.ID, sc.product_category_id, sc.SubCategoryName, sc.Description, sc.DisplayOrder, sc.IsActive, c.CategoryName
                    FROM drs_product_subcategory_mst sc WITH (NOLOCK)
                    LEFT JOIN drs_product_category_mst c WITH (NOLOCK) ON c.ID = sc.product_category_id
                    WHERE sc.ID = @ID AND sc.IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(
                    selectQuery,
                    new[] { new SqlParameter("@ID", subCategoryId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductSubCategoryUpdate rejected | Subcategory not found | SubCategoryId:{subCategoryId}"
                    );
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "SUBCATEGORY_NOT_FOUND",
                            Errors = new List<string> { "Product subcategory not found." },
                        }
                    );
                }

                DataRow existing = dt.Rows[0];
                long currentCategoryId = Convert.ToInt64(existing["product_category_id"]);
                long newCategoryId = currentCategoryId;
                string categoryName = existing["CategoryName"]?.ToString() ?? "";

                if (!string.IsNullOrWhiteSpace(request.EncryptedCategoryId))
                {
                    if (
                        !_encryption.TryDecrypt(request.EncryptedCategoryId, out long decCatId)
                        || decCatId <= 0
                    )
                    {
                        sw.Stop();
                        Logs.Warning(
                            $"ProductSubCategoryUpdate rejected | Invalid parent category ID: {request.EncryptedCategoryId}"
                        );
                        return BadRequest(
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 400,
                                Message = "INVALID_REQUEST",
                                Errors = new List<string> { "Invalid parent category ID." },
                            }
                        );
                    }

                    string checkCat =
                        "SELECT TOP 1 CategoryName FROM drs_product_category_mst WITH (NOLOCK) WHERE ID = @ID AND IsDeleted = 0 AND IsActive = 1";
                    object? cObj = _db.ExecuteScalar(
                        checkCat,
                        new[] { new SqlParameter("@ID", decCatId) }
                    );
                    if (cObj == null)
                    {
                        sw.Stop();
                        Logs.Warning(
                            $"ProductSubCategoryUpdate rejected | Parent category not found or inactive | CategoryId:{decCatId}"
                        );
                        return BadRequest(
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 400,
                                Message = "CATEGORY_NOT_FOUND",
                                Errors = new List<string>
                                {
                                    "Parent category not found or inactive.",
                                },
                            }
                        );
                    }
                    newCategoryId = decCatId;
                    categoryName = cObj.ToString() ?? "";
                }

                string oldName = existing["SubCategoryName"].ToString() ?? "";
                string newName =
                    request.SubCategoryName != null ? request.SubCategoryName.Trim() : oldName;
                string newDesc =
                    request.Description != null
                        ? request.Description.Trim()
                        : existing["Description"].ToString() ?? "";
                int newOrder = request.DisplayOrder ?? Convert.ToInt32(existing["DisplayOrder"]);
                bool newIsActive = request.IsActive ?? Convert.ToBoolean(existing["IsActive"]);

                if (
                    request.SubCategoryName != null
                    && !ValidationHelper.IsValidSubCategoryName(newName)
                )
                {
                    sw.Stop();
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = new List<string>
                            {
                                "SubCategory name must be between 2 and 100 characters.",
                            },
                        }
                    );
                }

                if (
                    request.Description != null
                    && !ValidationHelper.IsValidSubCategoryDescription(newDesc)
                )
                {
                    sw.Stop();
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = new List<string>
                            {
                                "Description cannot exceed 500 characters.",
                            },
                        }
                    );
                }

                // Check uniqueness under category if changed
                if (
                    !string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)
                    || newCategoryId != currentCategoryId
                )
                {
                    string dupQuery =
                        @"
                        SELECT COUNT(1) FROM drs_product_subcategory_mst WITH (NOLOCK)
                        WHERE product_category_id = @CategoryId AND SubCategoryName = @Name AND ID <> @ID AND IsDeleted = 0";
                    int dup = Convert.ToInt32(
                        _db.ExecuteScalar(
                            dupQuery,
                            new[]
                            {
                                new SqlParameter("@CategoryId", newCategoryId),
                                new SqlParameter("@Name", newName),
                                new SqlParameter("@ID", subCategoryId),
                            }
                        ) ?? 0
                    );

                    if (dup > 0)
                    {
                        sw.Stop();
                        Logs.Warning(
                            $"ProductSubCategoryUpdate rejected | Duplicate subcategory name '{newName}' under CategoryId:{newCategoryId}"
                        );
                        return Conflict(
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 409,
                                Message = "SUBCATEGORY_ALREADY_EXISTS",
                                Errors = new List<string>
                                {
                                    "SubCategory with given name already exists under this category.",
                                },
                            }
                        );
                    }
                }

                string updateQuery =
                    @"
                    UPDATE drs_product_subcategory_mst
                    SET product_category_id = @CategoryId,
                        SubCategoryName = @SubCategoryName,
                        Description = @Description,
                        DisplayOrder = @DisplayOrder,
                        IsActive = @IsActive,
                        ModifiedDate = GETDATE()
                    WHERE ID = @ID AND IsDeleted = 0";

                _db.ExecuteNonQuery(
                    updateQuery,
                    new[]
                    {
                        new SqlParameter("@CategoryId", newCategoryId),
                        new SqlParameter("@SubCategoryName", newName),
                        new SqlParameter("@Description", (object?)newDesc ?? DBNull.Value),
                        new SqlParameter("@DisplayOrder", newOrder),
                        new SqlParameter("@IsActive", newIsActive),
                        new SqlParameter("@ID", subCategoryId),
                    }
                );

                var responseData = new ProductSubCategoryResponse
                {
                    EncryptedSubCategoryId = request.EncryptedSubCategoryId,
                    EncryptedCategoryId = _encryption.Encrypt(newCategoryId),
                    CategoryName = categoryName,
                    SubCategoryName = newName,
                    Description = newDesc ?? string.Empty,
                    DisplayOrder = newOrder,
                    IsActive = newIsActive,
                    CreatedDate = DateTimeFormat.Format(DateTime.Now),
                };

                await _audit.InsertAuditAsync(
                    GetAuthenticatedUserId(),
                    "ProductSubCategory",
                    "ProductSubCategoryUpdate",
                    "drs_product_subcategory_mst",
                    subCategoryId,
                    System.Text.Json.JsonSerializer.Serialize(
                        new { SubCategoryName = oldName, DisplayOrder = existing["DisplayOrder"] }
                    ),
                    System.Text.Json.JsonSerializer.Serialize(responseData),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"ProductSubCategoryUpdate completed | SubCategoryId:{subCategoryId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUBCATEGORY_UPDATED",
                        Data = responseData,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ProductSubCategoryUpdate API", ex);
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

        #region SubCategory - Get By Id
        [AllowAnonymous]
        [HttpGet("subcategory/{encryptedSubCategoryId}")]
        public IActionResult GetProductSubCategoryById([FromRoute] string encryptedSubCategoryId)
        {
            Logs.Info("GetProductSubCategoryById API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (
                    !_encryption.TryDecrypt(encryptedSubCategoryId, out long subCategoryId)
                    || subCategoryId <= 0
                )
                {
                    sw.Stop();
                    Logs.Warning(
                        $"GetProductSubCategoryById rejected | Invalid encrypted SubCategory ID: {encryptedSubCategoryId}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string> { "Invalid encrypted SubCategory ID." },
                        }
                    );
                }

                string query =
                    @"
                    SELECT TOP 1 sc.ID, sc.product_category_id, sc.SubCategoryName, sc.Description, sc.DisplayOrder, sc.IsActive, sc.CreatedDate, c.CategoryName
                    FROM drs_product_subcategory_mst sc WITH (NOLOCK)
                    LEFT JOIN drs_product_category_mst c WITH (NOLOCK) ON c.ID = sc.product_category_id AND c.IsDeleted = 0
                    WHERE sc.ID = @ID AND sc.IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(
                    query,
                    new[] { new SqlParameter("@ID", subCategoryId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"GetProductSubCategoryById rejected | Subcategory not found | SubCategoryId:{subCategoryId}"
                    );
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "SUBCATEGORY_NOT_FOUND",
                            Errors = new List<string> { "Product subcategory not found." },
                        }
                    );
                }

                DataRow row = dt.Rows[0];
                var subCategoryResponse = new ProductSubCategoryResponse
                {
                    EncryptedSubCategoryId = _encryption.Encrypt(Convert.ToInt64(row["ID"])),
                    EncryptedCategoryId = _encryption.Encrypt(
                        Convert.ToInt64(row["product_category_id"])
                    ),
                    CategoryName = row["CategoryName"]?.ToString() ?? "",
                    SubCategoryName = row["SubCategoryName"]?.ToString() ?? "",
                    Description = row["Description"]?.ToString() ?? "",
                    DisplayOrder = Convert.ToInt32(row["DisplayOrder"]),
                    IsActive = Convert.ToBoolean(row["IsActive"]),
                    CreatedDate = DateTimeFormat.Format(Convert.ToDateTime(row["CreatedDate"])),
                };

                sw.Stop();
                Logs.Info($"GetProductSubCategoryById completed | SubCategoryId:{subCategoryId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = subCategoryResponse,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetProductSubCategoryById API", ex);
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

        #region SubCategory - Get List
        [AllowAnonymous]
        [HttpGet("subcategory/list/{encryptedCategoryId?}")]
        public IActionResult GetProductSubCategories(
            [FromRoute] string? encryptedCategoryId = null,
            [FromQuery] string? queryEncryptedCategoryId = null,
            [FromQuery] string? categoryId = null
        )
        {
            Logs.Info("GetProductSubCategories API started");
            var sw = Stopwatch.StartNew();

            try
            {
                string? targetCatId = !string.IsNullOrWhiteSpace(encryptedCategoryId)
                    ? encryptedCategoryId
                    : (
                        !string.IsNullOrWhiteSpace(queryEncryptedCategoryId)
                            ? queryEncryptedCategoryId
                            : categoryId
                    );

                string query =
                    @"
                    SELECT sc.ID, sc.product_category_id, sc.SubCategoryName, sc.Description, sc.DisplayOrder, sc.IsActive, sc.CreatedDate, c.CategoryName
                    FROM drs_product_subcategory_mst sc WITH (NOLOCK)
                    INNER JOIN drs_product_category_mst c WITH (NOLOCK) ON c.ID = sc.product_category_id AND c.IsDeleted = 0 AND c.IsActive = 1
                    WHERE sc.IsDeleted = 0 AND sc.IsActive = 1";

                var parameters = new List<SqlParameter>();

                if (!string.IsNullOrWhiteSpace(targetCatId))
                {
                    if (_encryption.TryDecrypt(targetCatId, out long decCatId) && decCatId > 0)
                    {
                        query += " AND sc.product_category_id = @CategoryId";
                        parameters.Add(new SqlParameter("@CategoryId", decCatId));
                    }
                    else
                    {
                        sw.Stop();
                        return BadRequest(
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 400,
                                Message = "INVALID_REQUEST",
                                Errors = new List<string> { "Invalid category ID filter." },
                            }
                        );
                    }
                }

                query += " ORDER BY sc.DisplayOrder ASC, sc.SubCategoryName ASC";

                DataTable dt = _db.ExecuteQuery(query, parameters.ToArray());

                var list = new List<ProductSubCategoryResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(
                        new ProductSubCategoryResponse
                        {
                            EncryptedSubCategoryId = _encryption.Encrypt(
                                Convert.ToInt64(row["ID"])
                            ),
                            EncryptedCategoryId = _encryption.Encrypt(
                                Convert.ToInt64(row["product_category_id"])
                            ),
                            CategoryName = row["CategoryName"]?.ToString() ?? "",
                            SubCategoryName = row["SubCategoryName"]?.ToString() ?? "",
                            Description = row["Description"]?.ToString() ?? "",
                            DisplayOrder = Convert.ToInt32(row["DisplayOrder"]),
                            IsActive = Convert.ToBoolean(row["IsActive"]),
                            CreatedDate = DateTimeFormat.Format(
                                Convert.ToDateTime(row["CreatedDate"])
                            ),
                        }
                    );
                }

                sw.Stop();
                Logs.Info($"GetProductSubCategories completed | Count:{list.Count}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = list,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetProductSubCategories API", ex);
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

        #region SubCategory - Delete
        [Authorize(Roles = "Admin")]
        [HttpDelete("subcategory/delete/{encryptedSubCategoryId?}")]
        [HttpPatch("subcategory/delete/{encryptedSubCategoryId?}")]
        public async Task<IActionResult> ProductSubCategorySoftDelete(
            [FromRoute] string? encryptedSubCategoryId = null,
            [FromQuery] string? queryEncryptedSubCategoryId = null
        )
        {
            Logs.Info("ProductSubCategorySoftDelete API started");
            var sw = Stopwatch.StartNew();

            try
            {
                string targetEncryptedId = !string.IsNullOrWhiteSpace(encryptedSubCategoryId)
                    ? encryptedSubCategoryId
                    : (queryEncryptedSubCategoryId ?? string.Empty);

                if (
                    string.IsNullOrWhiteSpace(targetEncryptedId)
                    || !_encryption.TryDecrypt(targetEncryptedId, out long subCategoryId)
                    || subCategoryId <= 0
                )
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductSubCategorySoftDelete rejected | Invalid encrypted SubCategory ID: {targetEncryptedId}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string> { "Invalid encrypted SubCategory ID." },
                        }
                    );
                }

                string checkQuery =
                    @"
                    SELECT TOP 1 ID, SubCategoryName, IsActive FROM drs_product_subcategory_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0";
                DataTable dt = _db.ExecuteQuery(
                    checkQuery,
                    new[] { new SqlParameter("@ID", subCategoryId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductSubCategorySoftDelete rejected | SubCategory not found | SubCategoryId:{subCategoryId}"
                    );
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "SUBCATEGORY_NOT_FOUND",
                            Errors = new List<string>
                            {
                                "SubCategory not found or already deleted.",
                            },
                        }
                    );
                }

                DataRow existingRow = dt.Rows[0];

                // Referential integrity check: Active products assigned
                string checkProdQuery =
                    @"
                    SELECT COUNT(1) FROM drs_product_mst WITH (NOLOCK)
                    WHERE product_subcategory_id = @ID AND IsDeleted = 0";
                int prodCount = Convert.ToInt32(
                    _db.ExecuteScalar(
                        checkProdQuery,
                        new[] { new SqlParameter("@ID", subCategoryId) }
                    ) ?? 0
                );
                if (prodCount > 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductSubCategorySoftDelete rejected | SubCategory {subCategoryId} has {prodCount} active products assigned"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "CANNOT_DELETE_PARENT_SUBCATEGORY",
                            Errors = new List<string>
                            {
                                "Cannot delete subcategory because active products are assigned to it.",
                            },
                        }
                    );
                }

                string deleteQuery =
                    @"
                    UPDATE drs_product_subcategory_mst
                    SET IsDeleted = 1, IsActive = 0, ModifiedDate = GETDATE()
                    WHERE ID = @ID";
                _db.ExecuteNonQuery(deleteQuery, new[] { new SqlParameter("@ID", subCategoryId) });

                await _audit.InsertAuditAsync(
                    GetAuthenticatedUserId(),
                    "ProductSubCategory",
                    "ProductSubCategorySoftDelete",
                    "drs_product_subcategory_mst",
                    subCategoryId,
                    System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            SubCategoryName = existingRow["SubCategoryName"]?.ToString() ?? "",
                            IsActive = Convert.ToBoolean(existingRow["IsActive"]),
                        }
                    ),
                    System.Text.Json.JsonSerializer.Serialize(
                        new { IsActive = false, IsDeleted = true }
                    ),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info(
                    $"ProductSubCategorySoftDelete completed | SubCategoryId:{subCategoryId}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUBCATEGORY_DELETED",
                        Data = null,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ProductSubCategorySoftDelete API", ex);
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
