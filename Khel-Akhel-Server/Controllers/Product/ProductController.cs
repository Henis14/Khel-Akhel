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
    [Route("api/product")]
    public class ProductController : ControllerBase
    {
        private readonly DbHelper _db;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService _audit;

        public ProductController(DbHelper db, IUrlEncryptionService encryption, IAuditService audit)
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

        #region 5.1 ProductCreate
        [Authorize(Roles = "Admin")]
        [HttpPost("create")]
        public async Task<IActionResult> ProductCreate([FromBody] ProductCreateRequest? request)
        {
            Logs.Info("ProductCreate API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning("ProductCreate rejected | Reason: Null request body");
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
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string> { "Invalid or tampered category ID." },
                        }
                    );
                }

                request.ProductName = request.ProductName?.Trim() ?? string.Empty;
                request.SKU = request.SKU?.Trim() ?? string.Empty;
                request.ShortDescription = request.ShortDescription?.Trim() ?? string.Empty;
                request.Description = request.Description?.Trim() ?? string.Empty;

                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.ProductName))
                    errors.Add("Product name is required.");
                else if (!ValidationHelper.IsValidProductName(request.ProductName))
                    errors.Add("Product name must be between 2 and 200 characters.");

                if (string.IsNullOrWhiteSpace(request.SKU))
                    errors.Add("SKU is required.");
                else if (!ValidationHelper.IsValidSku(request.SKU))
                    errors.Add(
                        "SKU must be 3-50 alphanumeric characters (hyphens and underscores allowed)."
                    );

                if (request.MRP < 0)
                    errors.Add("MRP cannot be negative.");
                else if (decimal.Round(request.MRP, 2) != request.MRP)
                    errors.Add("MRP cannot have more than 2 decimal places.");

                if (request.SellingPrice < 0)
                    errors.Add("Selling price cannot be negative.");
                else if (decimal.Round(request.SellingPrice, 2) != request.SellingPrice)
                    errors.Add("Selling price cannot have more than 2 decimal places.");
                else if (request.SellingPrice > request.MRP)
                    errors.Add("Selling price cannot be greater than MRP.");

                if (errors.Any())
                {
                    sw.Stop();
                    Logs.Warning(
                        $"ProductCreate validation failed | Errors:{string.Join(", ", errors)}"
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

                // Verify Category Exists
                string checkCategoryQuery =
                    @"
                    SELECT COUNT(1)
                    FROM drs_product_category_mst WITH (NOLOCK)
                    WHERE ID = @CategoryID AND IsDeleted = 0 AND IsActive = 1";

                int categoryExists = Convert.ToInt32(
                    _db.ExecuteScalar(
                        checkCategoryQuery,
                        new[] { new SqlParameter("@CategoryID", categoryId) }
                    ) ?? 0
                );

                if (categoryExists == 0)
                {
                    sw.Stop();
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "CATEGORY_NOT_FOUND",
                            Errors = new List<string>
                            {
                                "Specified product category does not exist or is inactive.",
                            },
                        }
                    );
                }

                // Check Duplicate SKU
                string checkSkuQuery =
                    @"
                    SELECT COUNT(1)
                    FROM drs_product_mst WITH (NOLOCK)
                    WHERE SKU = @SKU AND IsDeleted = 0";

                int skuExists = Convert.ToInt32(
                    _db.ExecuteScalar(
                        checkSkuQuery,
                        new[] { new SqlParameter("@SKU", request.SKU) }
                    ) ?? 0
                );

                if (skuExists > 0)
                {
                    sw.Stop();
                    return Conflict(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 409,
                            Message = "PRODUCT_ALREADY_EXISTS",
                            Errors = new List<string> { "Product with given SKU already exists." },
                        }
                    );
                }

                using SqlConnection con = _db.GetOpenConnection();
                using SqlTransaction tx = con.BeginTransaction();

                try
                {
                    string genCodeQuery =
                        "SELECT 'DRP' + RIGHT('000000' + CAST(NEXT VALUE FOR dbo.Seq_ProductCode AS VARCHAR(6)), 6);";
                    using SqlCommand genCmd = new(genCodeQuery, con, tx);
                    string productCode = Convert.ToString(genCmd.ExecuteScalar()) ?? string.Empty;

                    if (
                        !System.Text.RegularExpressions.Regex.IsMatch(productCode, @"^DRP[0-9]{6}$")
                    )
                    {
                        tx.Rollback();
                        throw new InvalidOperationException(
                            $"Invalid generated ProductCode format: {productCode}"
                        );
                    }

                    string insertProductQuery =
                        @"
                        INSERT INTO drs_product_mst
                        (product_category_id, ProductName, ProductCode, SKU, ShortDescription, Description, MRP, SellingPrice, IsFeatured, IsNewArrival, IsBestSeller, IsTrending, IsCustomerFavourite, IsActive, IsDeleted, CreatedDate)
                        VALUES
                        (@CategoryId, @ProductName, @ProductCode, @SKU, @ShortDescription, @Description, @MRP, @SellingPrice, @IsFeatured, @IsNewArrival, @IsBestSeller, @IsTrending, @IsCustomerFavourite, 1, 0, GETDATE());
                        SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

                    using SqlCommand productCmd = new(insertProductQuery, con, tx);
                    productCmd.Parameters.AddWithValue("@CategoryId", categoryId);
                    productCmd.Parameters.AddWithValue("@ProductName", request.ProductName);
                    productCmd.Parameters.AddWithValue("@ProductCode", productCode);
                    productCmd.Parameters.AddWithValue("@SKU", request.SKU);
                    productCmd.Parameters.AddWithValue(
                        "@ShortDescription",
                        (object?)request.ShortDescription ?? DBNull.Value
                    );
                    productCmd.Parameters.AddWithValue(
                        "@Description",
                        (object?)request.Description ?? DBNull.Value
                    );
                    productCmd.Parameters.AddWithValue("@MRP", request.MRP);
                    productCmd.Parameters.AddWithValue("@SellingPrice", request.SellingPrice);
                    productCmd.Parameters.AddWithValue("@IsFeatured", request.IsFeatured);
                    productCmd.Parameters.AddWithValue("@IsNewArrival", request.IsNewArrival);
                    productCmd.Parameters.AddWithValue("@IsBestSeller", request.IsBestSeller);
                    productCmd.Parameters.AddWithValue("@IsTrending", request.IsTrending);
                    productCmd.Parameters.AddWithValue(
                        "@IsCustomerFavourite",
                        request.IsCustomerFavourite
                    );

                    long productId = Convert.ToInt64(productCmd.ExecuteScalar());

                    // Create Initial Stock Record
                    string insertStockQuery =
                        @"
                        INSERT INTO drs_product_stock_mst
                        (product_id, AvailableQty, ReservedQty, IsActive, IsDeleted, CreatedDate)
                        VALUES
                        (@ProductId, 0, 0, 1, 0, GETDATE())";

                    using SqlCommand stockCmd = new(insertStockQuery, con, tx);
                    stockCmd.Parameters.AddWithValue("@ProductId", productId);
                    stockCmd.ExecuteNonQuery();

                    tx.Commit();

                    string encryptedProductId = _encryption.Encrypt(productId);
                    var responseData = new ProductResponse
                    {
                        EncryptedProductId = encryptedProductId,
                        EncryptedCategoryId = request.EncryptedCategoryId,
                        ProductName = request.ProductName,
                        ProductCode = productCode,
                        SKU = request.SKU,
                        ShortDescription = request.ShortDescription ?? string.Empty,
                        Description = request.Description ?? string.Empty,
                        MRP = request.MRP,
                        SellingPrice = request.SellingPrice,
                        IsFeatured = request.IsFeatured,
                        IsNewArrival = request.IsNewArrival,
                        IsBestSeller = request.IsBestSeller,
                        IsTrending = request.IsTrending,
                        IsCustomerFavourite = request.IsCustomerFavourite,
                        IsActive = true,
                        AvailableQty = 0,
                        CreatedDate = DateTimeFormat.Format(DateTime.Now),
                    };

                    await _audit.InsertAuditAsync(
                        GetAuthenticatedUserId(),
                        "Product",
                        "ProductCreate",
                        "drs_product_mst",
                        productId,
                        null,
                        System.Text.Json.JsonSerializer.Serialize(responseData),
                        GetClientIpAddress(),
                        GetUserAgent()
                    );

                    sw.Stop();
                    Logs.Info($"ProductCreate completed | ProductId:{productId}");

                    return Ok(
                        new ApiResponse
                        {
                            Success = true,
                            StatusCode = 200,
                            Message = "PRODUCT_CREATED",
                            Data = responseData,
                        }
                    );
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ProductCreate API", ex);
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

        #region 5.2 ProductUpdate [PATCH]
        [Authorize(Roles = "Admin")]
        [HttpPatch("update")]
        public async Task<IActionResult> ProductUpdate([FromBody] ProductUpdateRequest? request)
        {
            Logs.Info("ProductUpdate API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.EncryptedProductId))
                {
                    sw.Stop();
                    Logs.Warning("ProductUpdate rejected | Missing product ID or request body");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = new List<string> { "Encrypted product ID is required." },
                        }
                    );
                }

                if (
                    !_encryption.TryDecrypt(request.EncryptedProductId, out long productId)
                    || productId <= 0
                )
                {
                    sw.Stop();
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string>
                            {
                                "Invalid or tampered encrypted product ID.",
                            },
                        }
                    );
                }

                string selectQuery =
                    @"
                    SELECT TOP 1 ID, product_category_id, ProductName, ProductCode, SKU, ShortDescription, Description, MRP, SellingPrice, IsFeatured, IsNewArrival, IsBestSeller, IsTrending, IsCustomerFavourite, IsActive
                    FROM drs_product_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(
                    selectQuery,
                    new[] { new SqlParameter("@ID", productId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "PRODUCT_NOT_FOUND",
                            Errors = new List<string> { "Product record not found." },
                        }
                    );
                }

                DataRow existing = dt.Rows[0];
                long currentCategoryId = Convert.ToInt64(existing["product_category_id"]);

                long newCategoryId = currentCategoryId;
                if (!string.IsNullOrWhiteSpace(request.EncryptedCategoryId))
                {
                    if (
                        !_encryption.TryDecrypt(request.EncryptedCategoryId, out long decCatId)
                        || decCatId <= 0
                    )
                    {
                        sw.Stop();
                        return BadRequest(
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 400,
                                Message = "INVALID_REQUEST",
                                Errors = new List<string> { "Invalid category ID." },
                            }
                        );
                    }
                    newCategoryId = decCatId;
                }

                string newName =
                    request.ProductName != null
                        ? request.ProductName.Trim()
                        : existing["ProductName"].ToString() ?? "";
                string newCode =
                    request.ProductCode != null
                        ? request.ProductCode.Trim()
                        : existing["ProductCode"].ToString() ?? "";
                string newSku =
                    request.SKU != null ? request.SKU.Trim() : existing["SKU"].ToString() ?? "";
                string newShortDesc =
                    request.ShortDescription != null
                        ? request.ShortDescription.Trim()
                        : existing["ShortDescription"].ToString() ?? "";
                string newDesc =
                    request.Description != null
                        ? request.Description.Trim()
                        : existing["Description"].ToString() ?? "";
                decimal newMrp = request.MRP ?? Convert.ToDecimal(existing["MRP"]);
                decimal newPrice =
                    request.SellingPrice ?? Convert.ToDecimal(existing["SellingPrice"]);
                bool newFeatured = request.IsFeatured ?? Convert.ToBoolean(existing["IsFeatured"]);
                bool newNewArrival =
                    request.IsNewArrival ?? Convert.ToBoolean(existing["IsNewArrival"]);
                bool newBestSeller =
                    request.IsBestSeller ?? Convert.ToBoolean(existing["IsBestSeller"]);
                bool newTrending = request.IsTrending ?? Convert.ToBoolean(existing["IsTrending"]);
                bool newFavourite =
                    request.IsCustomerFavourite
                    ?? Convert.ToBoolean(existing["IsCustomerFavourite"]);
                bool newIsActive = request.IsActive ?? Convert.ToBoolean(existing["IsActive"]);

                var errors = new List<string>();
                if (request.ProductName != null && !ValidationHelper.IsValidProductName(newName))
                    errors.Add("Product name must be between 2 and 200 characters.");
                if (request.SKU != null && !ValidationHelper.IsValidSku(newSku))
                    errors.Add(
                        "SKU must be 3-50 alphanumeric characters (hyphens and underscores allowed)."
                    );
                if (newMrp < 0)
                    errors.Add("MRP cannot be negative.");
                else if (decimal.Round(newMrp, 2) != newMrp)
                    errors.Add("MRP cannot have more than 2 decimal places.");
                if (newPrice < 0)
                    errors.Add("Selling price cannot be negative.");
                else if (decimal.Round(newPrice, 2) != newPrice)
                    errors.Add("Selling price cannot have more than 2 decimal places.");
                if (newPrice > newMrp)
                    errors.Add("Selling price cannot be greater than MRP.");

                if (errors.Any())
                {
                    sw.Stop();
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

                // Check ProductCode Uniqueness if changed
                string oldCode = existing["ProductCode"].ToString() ?? "";
                if (!string.Equals(oldCode, newCode, StringComparison.OrdinalIgnoreCase))
                {
                    string dupCode =
                        @"SELECT COUNT(1) FROM drs_product_mst WITH (NOLOCK) WHERE ProductCode = @Code AND ID <> @ID AND IsDeleted = 0";
                    int cDup = Convert.ToInt32(
                        _db.ExecuteScalar(
                            dupCode,
                            new[]
                            {
                                new SqlParameter("@Code", newCode),
                                new SqlParameter("@ID", productId),
                            }
                        ) ?? 0
                    );

                    if (cDup > 0)
                    {
                        sw.Stop();
                        return Conflict(
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 409,
                                Message = "PRODUCT_ALREADY_EXISTS",
                                Errors = new List<string>
                                {
                                    "Product with given ProductCode already exists.",
                                },
                            }
                        );
                    }
                }

                // Check SKU Uniqueness if changed
                string oldSku = existing["SKU"].ToString() ?? "";
                if (!string.Equals(oldSku, newSku, StringComparison.OrdinalIgnoreCase))
                {
                    string dupSku =
                        @"SELECT COUNT(1) FROM drs_product_mst WITH (NOLOCK) WHERE SKU = @SKU AND ID <> @ID AND IsDeleted = 0";
                    int sDup = Convert.ToInt32(
                        _db.ExecuteScalar(
                            dupSku,
                            new[]
                            {
                                new SqlParameter("@SKU", newSku),
                                new SqlParameter("@ID", productId),
                            }
                        ) ?? 0
                    );

                    if (sDup > 0)
                    {
                        sw.Stop();
                        return Conflict(
                            new ApiResponse
                            {
                                Success = false,
                                StatusCode = 409,
                                Message = "PRODUCT_ALREADY_EXISTS",
                                Errors = new List<string>
                                {
                                    "Product with given SKU already exists.",
                                },
                            }
                        );
                    }
                }

                string updateQuery =
                    @"
                    UPDATE drs_product_mst
                    SET product_category_id = @CategoryId,
                        ProductName = @ProductName,
                        ProductCode = @ProductCode,
                        SKU = @SKU,
                        ShortDescription = @ShortDescription,
                        Description = @Description,
                        MRP = @MRP,
                        SellingPrice = @SellingPrice,
                        IsFeatured = @IsFeatured,
                        IsNewArrival = @IsNewArrival,
                        IsBestSeller = @IsBestSeller,
                        IsTrending = @IsTrending,
                        IsCustomerFavourite = @IsCustomerFavourite,
                        IsActive = @IsActive,
                        ModifiedDate = GETDATE()
                    WHERE ID = @ID AND IsDeleted = 0";

                _db.ExecuteNonQuery(
                    updateQuery,
                    new[]
                    {
                        new SqlParameter("@CategoryId", newCategoryId),
                        new SqlParameter("@ProductName", newName),
                        new SqlParameter("@ProductCode", newCode),
                        new SqlParameter("@SKU", newSku),
                        new SqlParameter(
                            "@ShortDescription",
                            (object?)newShortDesc ?? DBNull.Value
                        ),
                        new SqlParameter("@Description", (object?)newDesc ?? DBNull.Value),
                        new SqlParameter("@MRP", newMrp),
                        new SqlParameter("@SellingPrice", newPrice),
                        new SqlParameter("@IsFeatured", newFeatured),
                        new SqlParameter("@IsNewArrival", newNewArrival),
                        new SqlParameter("@IsBestSeller", newBestSeller),
                        new SqlParameter("@IsTrending", newTrending),
                        new SqlParameter("@IsCustomerFavourite", newFavourite),
                        new SqlParameter("@IsActive", newIsActive),
                        new SqlParameter("@ID", productId),
                    }
                );

                var responseData = new ProductResponse
                {
                    EncryptedProductId = request.EncryptedProductId,
                    EncryptedCategoryId = _encryption.Encrypt(newCategoryId),
                    ProductName = newName,
                    ProductCode = newCode,
                    SKU = newSku,
                    ShortDescription = newShortDesc ?? string.Empty,
                    Description = newDesc ?? string.Empty,
                    MRP = newMrp,
                    SellingPrice = newPrice,
                    IsFeatured = newFeatured,
                    IsNewArrival = newNewArrival,
                    IsBestSeller = newBestSeller,
                    IsTrending = newTrending,
                    IsCustomerFavourite = newFavourite,
                    IsActive = newIsActive,
                };

                await _audit.InsertAuditAsync(
                    GetAuthenticatedUserId(),
                    "Product",
                    "ProductUpdate",
                    "drs_product_mst",
                    productId,
                    System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            ID = productId,
                            ProductName = existing["ProductName"]?.ToString() ?? "",
                        }
                    ),
                    System.Text.Json.JsonSerializer.Serialize(responseData),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"ProductUpdate completed | ProductId:{productId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PRODUCT_UPDATED",
                        Data = responseData,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ProductUpdate API", ex);
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

        #region 5.3 ProductSoftDelete
        [Authorize(Roles = "Admin")]
        [HttpDelete("delete/{encryptedId?}")]
        [HttpPatch("delete/{encryptedId?}")]
        [HttpDelete("delete")]
        [HttpPatch("delete")]
        public async Task<IActionResult> ProductSoftDelete(
            [FromRoute] string? encryptedId = null,
            [FromQuery] string? encryptedIdQuery = null,
            [FromQuery] string? id = null
        )
        {
            Logs.Info("ProductSoftDelete API started");
            var sw = Stopwatch.StartNew();

            try
            {
                string? targetEncryptedId = !string.IsNullOrWhiteSpace(encryptedId)
                    ? encryptedId
                    : (!string.IsNullOrWhiteSpace(encryptedIdQuery) ? encryptedIdQuery : id);

                if (
                    string.IsNullOrWhiteSpace(targetEncryptedId)
                    || !_encryption.TryDecrypt(targetEncryptedId, out long productId)
                    || productId <= 0
                )
                {
                    sw.Stop();
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string> { "Encrypted product ID is required." },
                        }
                    );
                }

                string checkQuery =
                    @"SELECT COUNT(1) FROM drs_product_mst WITH (NOLOCK) WHERE ID = @ID AND IsDeleted = 0";
                int exists = Convert.ToInt32(
                    _db.ExecuteScalar(checkQuery, new[] { new SqlParameter("@ID", productId) }) ?? 0
                );

                if (exists == 0)
                {
                    sw.Stop();
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "PRODUCT_NOT_FOUND",
                            Errors = new List<string>
                            {
                                "Product record not found or already deleted.",
                            },
                        }
                    );
                }

                string deleteQuery =
                    @"UPDATE drs_product_mst SET IsDeleted = 1, IsActive = 0, ModifiedDate = GETDATE() WHERE ID = @ID";
                _db.ExecuteNonQuery(deleteQuery, new[] { new SqlParameter("@ID", productId) });

                await _audit.InsertAuditAsync(
                    GetAuthenticatedUserId(),
                    "Product",
                    "ProductSoftDelete",
                    "drs_product_mst",
                    productId,
                    System.Text.Json.JsonSerializer.Serialize(new { IsActive = 1, IsDeleted = 0 }),
                    System.Text.Json.JsonSerializer.Serialize(new { IsActive = 0, IsDeleted = 1 }),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"ProductSoftDelete completed | ProductId:{productId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "PRODUCT_DELETED",
                        Data = null,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ProductSoftDelete API", ex);
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

        #region 5.4 GetProductById [GET]
        [AllowAnonymous]
        [HttpGet("{encryptedId}")]
        public IActionResult GetProductById([FromRoute] string encryptedId)
        {
            Logs.Info("GetProductById API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (!_encryption.TryDecrypt(encryptedId, out long productId) || productId <= 0)
                {
                    sw.Stop();
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string>
                            {
                                "Invalid or tampered encrypted product ID.",
                            },
                        }
                    );
                }

                string query =
                    @"
                    SELECT p.ID, p.product_category_id, c.CategoryName, p.ProductName, p.ProductCode, p.SKU, p.ShortDescription, p.Description, p.MRP, p.SellingPrice, p.IsFeatured, p.IsNewArrival, p.IsBestSeller, p.IsTrending, p.IsCustomerFavourite, p.IsActive, p.CreatedDate, ISNULL(s.AvailableQty, 0) AS AvailableQty
                    FROM drs_product_mst p WITH (NOLOCK)
                    LEFT JOIN drs_product_category_mst c WITH (NOLOCK) ON c.ID = p.product_category_id AND c.IsDeleted = 0
                    LEFT JOIN drs_product_stock_mst s WITH (NOLOCK) ON s.product_id = p.ID AND s.IsDeleted = 0
                    WHERE p.ID = @ID AND p.IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(
                    query,
                    new[] { new SqlParameter("@ID", productId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "PRODUCT_NOT_FOUND",
                            Errors = new List<string> { "Product record not found." },
                        }
                    );
                }

                DataRow row = dt.Rows[0];

                // Fetch Product Images
                string imageQuery =
                    @"SELECT ImagePath FROM drs_product_image_mst WITH (NOLOCK) WHERE product_id = @ProductId AND IsDeleted = 0 ORDER BY IsDefault DESC, DisplayOrder ASC";
                DataTable dtImages = _db.ExecuteQuery(
                    imageQuery,
                    new[] { new SqlParameter("@ProductId", productId) }
                );
                var images = new List<string>();
                foreach (DataRow imgRow in dtImages.Rows)
                {
                    images.Add(imgRow["ImagePath"].ToString() ?? "");
                }

                var productResponse = new ProductResponse
                {
                    EncryptedProductId = _encryption.Encrypt(Convert.ToInt64(row["ID"])),
                    EncryptedCategoryId = _encryption.Encrypt(
                        Convert.ToInt64(row["product_category_id"])
                    ),
                    CategoryName = row["CategoryName"].ToString() ?? "",
                    ProductName = row["ProductName"].ToString() ?? "",
                    ProductCode = row["ProductCode"].ToString() ?? "",
                    SKU = row["SKU"].ToString() ?? "",
                    ShortDescription = row["ShortDescription"].ToString() ?? "",
                    Description = row["Description"].ToString() ?? "",
                    MRP = Convert.ToDecimal(row["MRP"]),
                    SellingPrice = Convert.ToDecimal(row["SellingPrice"]),
                    IsFeatured = Convert.ToBoolean(row["IsFeatured"]),
                    IsNewArrival = Convert.ToBoolean(row["IsNewArrival"]),
                    IsBestSeller = Convert.ToBoolean(row["IsBestSeller"]),
                    IsTrending = Convert.ToBoolean(row["IsTrending"]),
                    IsCustomerFavourite = Convert.ToBoolean(row["IsCustomerFavourite"]),
                    IsActive = Convert.ToBoolean(row["IsActive"]),
                    AvailableQty = Convert.ToInt32(row["AvailableQty"]),
                    ImagePaths = images,
                    CreatedDate = DateTimeFormat.Format(Convert.ToDateTime(row["CreatedDate"])),
                };

                sw.Stop();
                Logs.Info($"GetProductById completed | ProductId:{productId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = productResponse,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetProductById API", ex);
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

        #region 5.5 GetAllProduct & 5.6 SearchProducts [GET / POST]
        [AllowAnonymous]
        [HttpGet("list")]
        [HttpPost("list")]
        public IActionResult GetAllProduct(
            [FromQuery] ProductListRequest? queryRequest,
            [FromBody] ProductListRequest? bodyRequest
        )
        {
            var request =
                (HttpMethods.IsPost(Request.Method) ? bodyRequest : queryRequest)
                ?? queryRequest
                ?? bodyRequest
                ?? new ProductListRequest();
            return FetchProducts(request);
        }

        [AllowAnonymous]
        [HttpGet("search")]
        [HttpPost("search")]
        public IActionResult SearchProducts(
            [FromQuery] ProductListRequest? queryRequest,
            [FromBody] ProductListRequest? bodyRequest
        )
        {
            var request =
                (HttpMethods.IsPost(Request.Method) ? bodyRequest : queryRequest)
                ?? queryRequest
                ?? bodyRequest
                ?? new ProductListRequest();
            return FetchProducts(request);
        }

        private IActionResult FetchProducts(ProductListRequest request)
        {
            Logs.Info("FetchProducts API started");
            var sw = Stopwatch.StartNew();

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

                string whereClause = "WHERE p.IsDeleted = 0";

                if (!string.IsNullOrWhiteSpace(request.EncryptedCategoryId))
                {
                    if (
                        _encryption.TryDecrypt(request.EncryptedCategoryId, out long catId)
                        && catId > 0
                    )
                    {
                        whereClause += " AND p.product_category_id = @CategoryId";
                        parameters.Add(new SqlParameter("@CategoryId", catId));
                    }
                }

                if (request.MinPrice.HasValue)
                {
                    whereClause += " AND p.SellingPrice >= @MinPrice";
                    parameters.Add(new SqlParameter("@MinPrice", request.MinPrice.Value));
                }

                if (request.MaxPrice.HasValue)
                {
                    whereClause += " AND p.SellingPrice <= @MaxPrice";
                    parameters.Add(new SqlParameter("@MaxPrice", request.MaxPrice.Value));
                }

                if (request.IsActive.HasValue)
                {
                    whereClause += " AND p.IsActive = @IsActive";
                    parameters.Add(new SqlParameter("@IsActive", request.IsActive.Value));
                }

                if (request.IsFeatured.HasValue)
                {
                    whereClause += " AND p.IsFeatured = @IsFeatured";
                    parameters.Add(new SqlParameter("@IsFeatured", request.IsFeatured.Value));
                }

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    string term = $"%{request.Search.Trim()}%";
                    whereClause +=
                        " AND (p.ProductName LIKE @Search OR p.ProductCode LIKE @Search OR p.SKU LIKE @Search OR p.Description LIKE @Search)";
                    parameters.Add(new SqlParameter("@Search", term));
                }

                string orderByClause = (request.SortBy?.ToLower()) switch
                {
                    "price_asc" => "p.SellingPrice ASC",
                    "price_desc" => "p.SellingPrice DESC",
                    "name" => "p.ProductName ASC",
                    _ => "p.ID DESC",
                };

                string countQuery =
                    $"SELECT COUNT(1) FROM drs_product_mst p WITH (NOLOCK) {whereClause}";
                int totalRecords = Convert.ToInt32(
                    _db.ExecuteScalar(countQuery, parameters.ToArray()) ?? 0
                );

                string listQuery =
                    $@"
                    SELECT p.ID, p.product_category_id, c.CategoryName, p.ProductName, p.ProductCode, p.SKU, p.ShortDescription, p.Description, p.MRP, p.SellingPrice, p.IsFeatured, p.IsNewArrival, p.IsBestSeller, p.IsTrending, p.IsCustomerFavourite, p.IsActive, p.CreatedDate, ISNULL(s.AvailableQty, 0) AS AvailableQty
                    FROM drs_product_mst p WITH (NOLOCK)
                    LEFT JOIN drs_product_category_mst c WITH (NOLOCK) ON c.ID = p.product_category_id AND c.IsDeleted = 0
                    LEFT JOIN drs_product_stock_mst s WITH (NOLOCK) ON s.product_id = p.ID AND s.IsDeleted = 0
                    {whereClause}
                    ORDER BY {orderByClause}
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                DataTable dt = _db.ExecuteQuery(listQuery, parameters.ToArray());

                var productList = new List<ProductResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    long pId = Convert.ToInt64(row["ID"]);
                    productList.Add(
                        new ProductResponse
                        {
                            EncryptedProductId = _encryption.Encrypt(pId),
                            EncryptedCategoryId = _encryption.Encrypt(
                                Convert.ToInt64(row["product_category_id"])
                            ),
                            CategoryName = row["CategoryName"].ToString() ?? "",
                            ProductName = row["ProductName"].ToString() ?? "",
                            ProductCode = row["ProductCode"].ToString() ?? "",
                            SKU = row["SKU"].ToString() ?? "",
                            ShortDescription = row["ShortDescription"].ToString() ?? "",
                            Description = row["Description"].ToString() ?? "",
                            MRP = Convert.ToDecimal(row["MRP"]),
                            SellingPrice = Convert.ToDecimal(row["SellingPrice"]),
                            IsFeatured = Convert.ToBoolean(row["IsFeatured"]),
                            IsNewArrival = Convert.ToBoolean(row["IsNewArrival"]),
                            IsBestSeller = Convert.ToBoolean(row["IsBestSeller"]),
                            IsTrending = Convert.ToBoolean(row["IsTrending"]),
                            IsCustomerFavourite = Convert.ToBoolean(row["IsCustomerFavourite"]),
                            IsActive = Convert.ToBoolean(row["IsActive"]),
                            AvailableQty = Convert.ToInt32(row["AvailableQty"]),
                            CreatedDate = DateTimeFormat.Format(
                                Convert.ToDateTime(row["CreatedDate"])
                            ),
                        }
                    );
                }

                var responseData = new ProductListResponse
                {
                    Products = productList,
                    TotalRecords = totalRecords,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                };

                sw.Stop();
                Logs.Info(
                    $"FetchProducts completed | Count:{productList.Count} TotalRecords:{totalRecords}"
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
                Logs.Error("Exception occurred in FetchProducts API", ex);
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
