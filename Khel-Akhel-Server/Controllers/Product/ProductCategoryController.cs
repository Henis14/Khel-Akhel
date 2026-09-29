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
            IAuditService audit)
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

        #region 4.1 ProductCategoryCreate
        [Authorize(Roles = "Admin")]
        [HttpPost("create")]
        public async Task<IActionResult> ProductCategoryCreate([FromBody] ProductCategoryCreateRequest? request)
        {
            Logs.Info("ProductCategoryCreate API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning("ProductCategoryCreate rejected | Reason: Null request body");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "REQUEST_BODY_REQUIRED",
                        Errors = new List<string> { "Request body cannot be null." }
                    });
                }

                request.CategoryName = request.CategoryName?.Trim() ?? string.Empty;
                request.Description = request.Description?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(request.CategoryName))
                {
                    sw.Stop();
                    Logs.Warning("ProductCategoryCreate rejected | Reason: Category name missing");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "VALIDATION_FAILED",
                        Errors = new List<string> { "Category name is required." }
                    });
                }

                // Uniqueness check for CategoryName where IsDeleted = 0
                string checkQuery = @"
                    SELECT COUNT(1)
                    FROM drs_product_category_mst WITH (NOLOCK)
                    WHERE CategoryName = @CategoryName AND IsDeleted = 0";

                int exists = Convert.ToInt32(_db.ExecuteScalar(checkQuery, new[]
                {
                    new SqlParameter("@CategoryName", request.CategoryName)
                }) ?? 0);

                if (exists > 0)
                {
                    sw.Stop();
                    Logs.Warning($"ProductCategoryCreate duplicate category name: {request.CategoryName}");
                    return Conflict(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 409,
                        Message = "CATEGORY_ALREADY_EXISTS",
                        Errors = new List<string> { "Product category with given name already exists." }
                    });
                }

                string insertQuery = @"
                    INSERT INTO drs_product_category_mst
                    (CategoryName, Description, DisplayOrder, IsActive, IsDeleted, CreatedDate)
                    OUTPUT INSERTED.ID
                    VALUES
                    (@CategoryName, @Description, @DisplayOrder, 1, 0, GETDATE())";

                object? result = _db.ExecuteScalar(insertQuery, new[]
                {
                    new SqlParameter("@CategoryName", request.CategoryName),
                    new SqlParameter("@Description", (object?)request.Description ?? DBNull.Value),
                    new SqlParameter("@DisplayOrder", request.DisplayOrder)
                });

                long categoryId = Convert.ToInt64(result);
                string encryptedId = _encryption.Encrypt(categoryId);

                var responseData = new ProductCategoryResponse
                {
                    EncryptedCategoryId = encryptedId,
                    CategoryName = request.CategoryName,
                    Description = request.Description,
                    DisplayOrder = request.DisplayOrder,
                    IsActive = true,
                    CreatedDate = DateTimeFormat.Format(DateTime.Now)
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

                return Ok(new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "CATEGORY_CREATED",
                    Data = responseData
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ProductCategoryCreate API", ex);
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

        #region 4.2 ProductCategoryUpdate [PATCH]
        [Authorize(Roles = "Admin")]
        [HttpPatch("update")]
        public async Task<IActionResult> ProductCategoryUpdate([FromBody] ProductCategoryUpdateRequest? request)
        {
            Logs.Info("ProductCategoryUpdate API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.EncryptedCategoryId))
                {
                    sw.Stop();
                    Logs.Warning("ProductCategoryUpdate rejected | Missing category ID or request body");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "VALIDATION_FAILED",
                        Errors = new List<string> { "Encrypted category ID is required." }
                    });
                }

                if (!_encryption.TryDecrypt(request.EncryptedCategoryId, out long categoryId) || categoryId <= 0)
                {
                    sw.Stop();
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_REQUEST",
                        Errors = new List<string> { "Invalid or tampered encrypted category ID." }
                    });
                }

                string selectQuery = @"
                    SELECT TOP 1 ID, CategoryName, Description, DisplayOrder, IsActive
                    FROM drs_product_category_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(selectQuery, new[] { new SqlParameter("@ID", categoryId) });
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "CATEGORY_NOT_FOUND",
                        Errors = new List<string> { "Product category not found." }
                    });
                }

                DataRow existing = dt.Rows[0];
                string oldName = existing["CategoryName"].ToString() ?? "";
                string newName = request.CategoryName != null ? request.CategoryName.Trim() : oldName;
                string newDesc = request.Description != null ? request.Description.Trim() : existing["Description"].ToString() ?? "";
                int newOrder = request.DisplayOrder ?? Convert.ToInt32(existing["DisplayOrder"]);
                bool newIsActive = request.IsActive ?? Convert.ToBoolean(existing["IsActive"]);

                if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
                {
                    string dupQuery = @"
                        SELECT COUNT(1)
                        FROM drs_product_category_mst WITH (NOLOCK)
                        WHERE CategoryName = @CategoryName AND ID <> @ID AND IsDeleted = 0";

                    int dup = Convert.ToInt32(_db.ExecuteScalar(dupQuery, new[]
                    {
                        new SqlParameter("@CategoryName", newName),
                        new SqlParameter("@ID", categoryId)
                    }) ?? 0);

                    if (dup > 0)
                    {
                        sw.Stop();
                        return Conflict(new ApiResponse
                        {
                            Success = false,
                            StatusCode = 409,
                            Message = "CATEGORY_ALREADY_EXISTS",
                            Errors = new List<string> { "Product category with given name already exists." }
                        });
                    }
                }

                string updateQuery = @"
                    UPDATE drs_product_category_mst
                    SET CategoryName = @CategoryName,
                        Description = @Description,
                        DisplayOrder = @DisplayOrder,
                        IsActive = @IsActive,
                        ModifiedDate = GETDATE()
                    WHERE ID = @ID AND IsDeleted = 0";

                _db.ExecuteNonQuery(updateQuery, new[]
                {
                    new SqlParameter("@CategoryName", newName),
                    new SqlParameter("@Description", (object?)newDesc ?? DBNull.Value),
                    new SqlParameter("@DisplayOrder", newOrder),
                    new SqlParameter("@IsActive", newIsActive),
                    new SqlParameter("@ID", categoryId)
                });

                var responseData = new ProductCategoryResponse
                {
                    EncryptedCategoryId = request.EncryptedCategoryId,
                    CategoryName = newName,
                    Description = newDesc,
                    DisplayOrder = newOrder,
                    IsActive = newIsActive,
                    CreatedDate = DateTimeFormat.Format(DateTime.Now)
                };

                await _audit.InsertAuditAsync(
                    GetAuthenticatedUserId(),
                    "ProductCategory",
                    "ProductCategoryUpdate",
                    "drs_product_category_mst",
                    categoryId,
                    System.Text.Json.JsonSerializer.Serialize(existing.Table.Columns),
                    System.Text.Json.JsonSerializer.Serialize(responseData),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"ProductCategoryUpdate completed | CategoryId:{categoryId}");

                return Ok(new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "CATEGORY_UPDATED",
                    Data = responseData
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ProductCategoryUpdate API", ex);
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

        #region 4.3 GetProductCategoryById [GET]
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
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_REQUEST",
                        Errors = new List<string> { "Invalid or tampered encrypted category ID." }
                    });
                }

                string query = @"
                    SELECT TOP 1 ID, CategoryName, Description, DisplayOrder, IsActive, CreatedDate
                    FROM drs_product_category_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(query, new[] { new SqlParameter("@ID", categoryId) });
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "CATEGORY_NOT_FOUND",
                        Errors = new List<string> { "Product category not found." }
                    });
                }

                DataRow row = dt.Rows[0];
                var categoryResponse = new ProductCategoryResponse
                {
                    EncryptedCategoryId = _encryption.Encrypt(Convert.ToInt64(row["ID"])),
                    CategoryName = row["CategoryName"].ToString() ?? "",
                    Description = row["Description"].ToString() ?? "",
                    DisplayOrder = Convert.ToInt32(row["DisplayOrder"]),
                    IsActive = Convert.ToBoolean(row["IsActive"]),
                    CreatedDate = DateTimeFormat.Format(Convert.ToDateTime(row["CreatedDate"]))
                };

                sw.Stop();
                Logs.Info($"GetProductCategoryById completed | CategoryId:{categoryId}");

                return Ok(new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = categoryResponse
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetProductCategoryById API", ex);
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

        #region 4.4 GetAllProductCategory [GET]
        [AllowAnonymous]
        [HttpGet("list")]
        public IActionResult GetAllProductCategory()
        {
            Logs.Info("GetAllProductCategory API started");
            var sw = Stopwatch.StartNew();

            try
            {
                string query = @"
                    SELECT ID, CategoryName, Description, DisplayOrder, IsActive, CreatedDate
                    FROM drs_product_category_mst WITH (NOLOCK)
                    WHERE IsDeleted = 0 AND IsActive = 1
                    ORDER BY DisplayOrder ASC, CategoryName ASC";

                DataTable dt = _db.ExecuteQuery(query);

                var categoryList = new List<ProductCategoryResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    categoryList.Add(new ProductCategoryResponse
                    {
                        EncryptedCategoryId = _encryption.Encrypt(Convert.ToInt64(row["ID"])),
                        CategoryName = row["CategoryName"].ToString() ?? "",
                        Description = row["Description"].ToString() ?? "",
                        DisplayOrder = Convert.ToInt32(row["DisplayOrder"]),
                        IsActive = Convert.ToBoolean(row["IsActive"]),
                        CreatedDate = DateTimeFormat.Format(Convert.ToDateTime(row["CreatedDate"]))
                    });
                }

                sw.Stop();
                Logs.Info($"GetAllProductCategory completed | Count:{categoryList.Count}");

                return Ok(new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = categoryList
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetAllProductCategory API", ex);
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
