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
    [Route("api/product-stock")]
    [Authorize(Roles = "Admin")]
    public class ProductStockController : ControllerBase
    {
        private readonly DbHelper _db;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService _audit;

        public ProductStockController(
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

        #region 6.1 AddStockByproductId
        [HttpPost("add")]
        public async Task<IActionResult> AddStockByproductId([FromBody] ProductStockAddRequest? request)
        {
            Logs.Info("AddStockByproductId API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.EncryptedProductId))
                {
                    sw.Stop();
                    Logs.Warning("AddStockByproductId rejected | Null request body or missing product ID");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "VALIDATION_FAILED",
                        Errors = new List<string> { "Product ID is required." }
                    });
                }

                if (!_encryption.TryDecrypt(request.EncryptedProductId, out long productId) || productId <= 0)
                {
                    sw.Stop();
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_REQUEST",
                        Errors = new List<string> { "Invalid or tampered encrypted product ID." }
                    });
                }

                if (request.Quantity <= 0)
                {
                    sw.Stop();
                    Logs.Warning($"AddStockByproductId rejected | Invalid quantity: {request.Quantity}");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "VALIDATION_FAILED",
                        Errors = new List<string> { "Quantity to add must be greater than zero." }
                    });
                }

                // Verify Product Exists
                string checkProductQuery = @"SELECT COUNT(1) FROM drs_product_mst WITH (NOLOCK) WHERE ID = @ID AND IsDeleted = 0 AND IsActive = 1";
                int productExists = Convert.ToInt32(_db.ExecuteScalar(checkProductQuery, new[] { new SqlParameter("@ID", productId) }) ?? 0);

                if (productExists == 0)
                {
                    sw.Stop();
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "PRODUCT_NOT_FOUND",
                        Errors = new List<string> { "Product not found or inactive." }
                    });
                }

                using SqlConnection con = _db.GetOpenConnection();
                using SqlTransaction tx = con.BeginTransaction();

                try
                {
                    // Check if stock record exists for product
                    string selectStockQuery = @"SELECT TOP 1 ID, AvailableQty, ReservedQty FROM drs_product_stock_mst WITH (UPDLOCK, ROWLOCK) WHERE product_id = @ProductId AND IsDeleted = 0";
                    using SqlCommand selectCmd = new(selectStockQuery, con, tx);
                    selectCmd.Parameters.AddWithValue("@ProductId", productId);

                    DataTable dtStock = new();
                    new SqlDataAdapter(selectCmd).Fill(dtStock);

                    int oldAvailable = 0;
                    int newAvailable = 0;
                    long stockId = 0;

                    if (dtStock.Rows.Count > 0)
                    {
                        DataRow sRow = dtStock.Rows[0];
                        stockId = Convert.ToInt64(sRow["ID"]);
                        oldAvailable = Convert.ToInt32(sRow["AvailableQty"]);
                        newAvailable = oldAvailable + request.Quantity;

                        string updateStock = @"UPDATE drs_product_stock_mst SET AvailableQty = @AvailableQty, ModifiedDate = GETDATE() WHERE ID = @ID";
                        using SqlCommand uCmd = new(updateStock, con, tx);
                        uCmd.Parameters.AddWithValue("@AvailableQty", newAvailable);
                        uCmd.Parameters.AddWithValue("@ID", stockId);
                        uCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        oldAvailable = 0;
                        newAvailable = request.Quantity;

                        string insertStock = @"
                            INSERT INTO drs_product_stock_mst (product_id, AvailableQty, ReservedQty, IsActive, IsDeleted, CreatedDate)
                            VALUES (@ProductId, @AvailableQty, 0, 1, 0, GETDATE());
                            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

                        using SqlCommand iCmd = new(insertStock, con, tx);
                        iCmd.Parameters.AddWithValue("@ProductId", productId);
                        iCmd.Parameters.AddWithValue("@AvailableQty", newAvailable);
                        stockId = Convert.ToInt64(iCmd.ExecuteScalar());
                    }

                    tx.Commit();

                    await _audit.InsertAuditAsync(
                        GetAuthenticatedUserId(),
                        "ProductStock",
                        "AddStockByproductId",
                        "drs_product_stock_mst",
                        stockId,
                        System.Text.Json.JsonSerializer.Serialize(new { AvailableQty = oldAvailable }),
                        System.Text.Json.JsonSerializer.Serialize(new { AvailableQty = newAvailable, AddedQty = request.Quantity }),
                        GetClientIpAddress(),
                        GetUserAgent()
                    );

                    sw.Stop();
                    Logs.Info($"AddStockByproductId completed | ProductId:{productId} Added:{request.Quantity} Total:{newAvailable}");

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "STOCK_ADDED",
                        Data = new
                        {
                            EncryptedProductId = request.EncryptedProductId,
                            AddedQuantity = request.Quantity,
                            TotalAvailableQuantity = newAvailable
                        }
                    });
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
                Logs.Error("Exception occurred in AddStockByproductId API", ex);
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

        #region 6.2 UpdateStockByproductId [PATCH]
        [HttpPatch("update")]
        public async Task<IActionResult> UpdateStockByproductId([FromBody] ProductStockUpdateRequest? request)
        {
            Logs.Info("UpdateStockByproductId API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.EncryptedProductId))
                {
                    sw.Stop();
                    Logs.Warning("UpdateStockByproductId rejected | Null request body or missing product ID");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "VALIDATION_FAILED",
                        Errors = new List<string> { "Product ID is required." }
                    });
                }

                if (!_encryption.TryDecrypt(request.EncryptedProductId, out long productId) || productId <= 0)
                {
                    sw.Stop();
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_REQUEST",
                        Errors = new List<string> { "Invalid or tampered encrypted product ID." }
                    });
                }

                if (request.AvailableQuantity < 0 || request.ReservedQuantity < 0)
                {
                    sw.Stop();
                    Logs.Warning($"UpdateStockByproductId rejected | Negative quantity: Available:{request.AvailableQuantity} Reserved:{request.ReservedQuantity}");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "VALIDATION_FAILED",
                        Errors = new List<string> { "Stock quantities cannot be negative." }
                    });
                }

                using SqlConnection con = _db.GetOpenConnection();
                using SqlTransaction tx = con.BeginTransaction();

                try
                {
                    string selectStockQuery = @"SELECT TOP 1 ID, AvailableQty, ReservedQty FROM drs_product_stock_mst WITH (UPDLOCK, ROWLOCK) WHERE product_id = @ProductId AND IsDeleted = 0";
                    using SqlCommand selectCmd = new(selectStockQuery, con, tx);
                    selectCmd.Parameters.AddWithValue("@ProductId", productId);

                    DataTable dtStock = new();
                    new SqlDataAdapter(selectCmd).Fill(dtStock);

                    int oldAvailable = 0;
                    int oldReserved = 0;
                    long stockId = 0;

                    if (dtStock.Rows.Count > 0)
                    {
                        DataRow sRow = dtStock.Rows[0];
                        stockId = Convert.ToInt64(sRow["ID"]);
                        oldAvailable = Convert.ToInt32(sRow["AvailableQty"]);
                        oldReserved = Convert.ToInt32(sRow["ReservedQty"]);

                        string updateStock = @"UPDATE drs_product_stock_mst SET AvailableQty = @AvailableQty, ReservedQty = @ReservedQty, ModifiedDate = GETDATE() WHERE ID = @ID";
                        using SqlCommand uCmd = new(updateStock, con, tx);
                        uCmd.Parameters.AddWithValue("@AvailableQty", request.AvailableQuantity);
                        uCmd.Parameters.AddWithValue("@ReservedQty", request.ReservedQuantity);
                        uCmd.Parameters.AddWithValue("@ID", stockId);
                        uCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        string insertStock = @"
                            INSERT INTO drs_product_stock_mst (product_id, AvailableQty, ReservedQty, IsActive, IsDeleted, CreatedDate)
                            VALUES (@ProductId, @AvailableQty, @ReservedQty, 1, 0, GETDATE());
                            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

                        using SqlCommand iCmd = new(insertStock, con, tx);
                        iCmd.Parameters.AddWithValue("@ProductId", productId);
                        iCmd.Parameters.AddWithValue("@AvailableQty", request.AvailableQuantity);
                        iCmd.Parameters.AddWithValue("@ReservedQty", request.ReservedQuantity);
                        stockId = Convert.ToInt64(iCmd.ExecuteScalar());
                    }

                    tx.Commit();

                    await _audit.InsertAuditAsync(
                        GetAuthenticatedUserId(),
                        "ProductStock",
                        "UpdateStockByproductId",
                        "drs_product_stock_mst",
                        stockId,
                        System.Text.Json.JsonSerializer.Serialize(new { AvailableQty = oldAvailable, ReservedQty = oldReserved }),
                        System.Text.Json.JsonSerializer.Serialize(new { AvailableQty = request.AvailableQuantity, ReservedQty = request.ReservedQuantity }),
                        GetClientIpAddress(),
                        GetUserAgent()
                    );

                    sw.Stop();
                    Logs.Info($"UpdateStockByproductId completed | ProductId:{productId} Available:{request.AvailableQuantity}");

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "STOCK_UPDATED",
                        Data = new
                        {
                            EncryptedProductId = request.EncryptedProductId,
                            AvailableQuantity = request.AvailableQuantity,
                            ReservedQuantity = request.ReservedQuantity
                        }
                    });
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
                Logs.Error("Exception occurred in UpdateStockByproductId API", ex);
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

        #region 6.3 GetAllProductStock [GET]
        [HttpGet("list")]
        public IActionResult GetAllProductStock([FromQuery] ProductStockListRequest request)
        {
            Logs.Info("GetAllProductStock API started");
            var sw = Stopwatch.StartNew();

            try
            {
                int pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
                int pageSize = request.PageSize < 1 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);
                int offset = (pageIndex - 1) * pageSize;

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@Offset", offset),
                    new SqlParameter("@PageSize", pageSize)
                };

                string whereClause = "WHERE p.IsDeleted = 0";

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    whereClause += " AND (p.ProductName LIKE @Search OR p.ProductCode LIKE @Search OR p.SKU LIKE @Search)";
                    parameters.Add(new SqlParameter("@Search", $"%{request.Search.Trim()}%"));
                }

                if (request.LowStockOnly == true)
                {
                    whereClause += " AND ISNULL(s.AvailableQty, 0) <= 5";
                }

                string countQuery = $"SELECT COUNT(1) FROM drs_product_mst p WITH (NOLOCK) LEFT JOIN drs_product_stock_mst s WITH (NOLOCK) ON s.product_id = p.ID AND s.IsDeleted = 0 {whereClause}";
                int totalRecords = Convert.ToInt32(_db.ExecuteScalar(countQuery, parameters.ToArray()) ?? 0);

                string listQuery = $@"
                    SELECT s.ID AS StockId, p.ID AS ProductId, p.ProductName, p.ProductCode, p.SKU, ISNULL(s.AvailableQty, 0) AS AvailableQty, ISNULL(s.ReservedQty, 0) AS ReservedQty, ISNULL(s.IsActive, 1) AS IsActive, p.CreatedDate
                    FROM drs_product_mst p WITH (NOLOCK)
                    LEFT JOIN drs_product_stock_mst s WITH (NOLOCK) ON s.product_id = p.ID AND s.IsDeleted = 0
                    {whereClause}
                    ORDER BY p.ID DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                DataTable dt = _db.ExecuteQuery(listQuery, parameters.ToArray());

                var stockList = new List<ProductStockResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    long stockId = row["StockId"] != DBNull.Value ? Convert.ToInt64(row["StockId"]) : 0;
                    long productId = Convert.ToInt64(row["ProductId"]);

                    stockList.Add(new ProductStockResponse
                    {
                        EncryptedStockId = stockId > 0 ? _encryption.Encrypt(stockId) : "",
                        EncryptedProductId = _encryption.Encrypt(productId),
                        ProductName = row["ProductName"].ToString() ?? "",
                        ProductCode = row["ProductCode"].ToString() ?? "",
                        SKU = row["SKU"].ToString() ?? "",
                        AvailableQty = Convert.ToInt32(row["AvailableQty"]),
                        ReservedQty = Convert.ToInt32(row["ReservedQty"]),
                        IsActive = Convert.ToBoolean(row["IsActive"]),
                        CreatedDate = DateTimeFormat.Format(Convert.ToDateTime(row["CreatedDate"]))
                    });
                }

                var responseData = new ProductStockListResponse
                {
                    Stocks = stockList,
                    TotalRecords = totalRecords,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };

                sw.Stop();
                Logs.Info($"GetAllProductStock completed | Count:{stockList.Count} TotalRecords:{totalRecords}");

                return Ok(new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = responseData
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetAllProductStock API", ex);
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
