using System.Data;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.Models;
using Microsoft.Data.SqlClient;

namespace Khel_Akhel_Server.BL.Services
{
    public interface ICartService
    {
        Task<CartResponse> GetCustomerCartAsync(long customerId);
        Task<(bool Success, string Message, CartResponse? Cart)> AddItemAsync(long customerId, long productId, int quantity, string? ipAddress, string? userAgent);
        Task<(bool Success, string Message, CartResponse? Cart)> UpdateItemQuantityAsync(long customerId, long productId, int quantity, string? ipAddress, string? userAgent);
        Task<(bool Success, string Message, CartResponse? Cart)> RemoveItemAsync(long customerId, long productId, string? ipAddress, string? userAgent);
        Task<(bool Success, string Message)> ClearCartAsync(long customerId, string? ipAddress, string? userAgent);
    }

    public class CartService : ICartService
    {
        private readonly DbHelper _db;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService _audit;

        public CartService(
            DbHelper db,
            IUrlEncryptionService encryption,
            IAuditService audit)
        {
            _db = db;
            _encryption = encryption;
            _audit = audit;
        }

        #region Internal Helper: Resolve or Create Active Cart
        private async Task<long> GetOrCreateActiveCartIdAsync(long customerId, bool createIfNotFound = true)
        {
            if (customerId <= 0)
                return 0;

            string query = @"
                DECLARE @CartId BIGINT;
                SELECT TOP 1 @CartId = ID FROM drs_cart_mst WITH (UPDLOCK, HOLDLOCK)
                WHERE customer_id = @CustomerId AND CartStatus = 'Active' AND IsDeleted = 0;

                IF @CartId IS NULL AND @CreateIfNotFound = 1
                BEGIN
                    BEGIN TRY
                        INSERT INTO drs_cart_mst (customer_id, CartStatus, IsActive, IsDeleted, CreatedDate)
                        VALUES (@CustomerId, 'Active', 1, 0, GETDATE());
                        SET @CartId = SCOPE_IDENTITY();
                    END TRY
                    BEGIN CATCH
                        SELECT TOP 1 @CartId = ID FROM drs_cart_mst WITH (NOLOCK)
                        WHERE customer_id = @CustomerId AND CartStatus = 'Active' AND IsDeleted = 0;
                    END CATCH
                END
                SELECT ISNULL(@CartId, 0);";

            object? cartIdObj = await _db.ExecuteScalarAsync(query, new[]
            {
                new SqlParameter("@CustomerId", customerId),
                new SqlParameter("@CreateIfNotFound", createIfNotFound ? 1 : 0)
            });

            return cartIdObj != null && cartIdObj != DBNull.Value ? Convert.ToInt64(cartIdObj) : 0;
        }
        #endregion

        #region Public Methods
        public async Task<CartResponse> GetCustomerCartAsync(long customerId)
        {
            if (customerId <= 0)
            {
                return new CartResponse { CartStatus = "Empty" };
            }

            long cartId = await GetOrCreateActiveCartIdAsync(customerId, createIfNotFound: true);

            string query = @"
                SELECT 
                    ci.ID AS CartItemId,
                    ci.product_id,
                    ci.Quantity,
                    ci.CreatedDate AS ItemAddedDate,
                    p.ProductCode,
                    p.SKU,
                    p.ProductName,
                    c.CategoryName,
                    p.ShortDescription,
                    p.MRP,
                    p.SellingPrice,
                    p.IsActive AS ProductActive,
                    ISNULL(s.AvailableQty, 0) AS AvailableQty,
                    img.ImagePath AS PrimaryImagePath
                FROM drs_cart_item_mst ci WITH (NOLOCK)
                INNER JOIN drs_product_mst p WITH (NOLOCK) ON p.ID = ci.product_id AND p.IsDeleted = 0
                LEFT JOIN drs_product_category_mst c WITH (NOLOCK) ON c.ID = p.product_category_id AND c.IsDeleted = 0
                LEFT JOIN drs_product_stock_mst s WITH (NOLOCK) ON s.product_id = p.ID AND s.IsDeleted = 0
                LEFT JOIN drs_product_image_mst img WITH (NOLOCK) ON img.product_id = p.ID AND img.IsDefault = 1 AND img.IsDeleted = 0
                WHERE ci.cart_id = @CartId AND ci.IsDeleted = 0
                ORDER BY ci.CreatedDate DESC";

            DataTable dt = await _db.ExecuteQueryAsync(query, new[]
            {
                new SqlParameter("@CartId", cartId)
            });

            var cart = new CartResponse
            {
                EncryptedCartId = _encryption.Encrypt(cartId),
                CartStatus = "Active",
                DistinctItemsCount = dt.Rows.Count,
                Items = new List<CartItemResponse>()
            };

            int totalQuantity = 0;
            decimal subTotal = 0m;
            decimal totalMrp = 0m;

            foreach (DataRow row in dt.Rows)
            {
                long prodId = Convert.ToInt64(row["product_id"]);
                long itemId = Convert.ToInt64(row["CartItemId"]);
                int qty = Convert.ToInt32(row["Quantity"]);
                decimal mrp = Convert.ToDecimal(row["MRP"]);
                decimal sp = Convert.ToDecimal(row["SellingPrice"]);
                decimal itemTotal = sp * qty;
                decimal itemMrpTotal = mrp * qty;
                int available = Convert.ToInt32(row["AvailableQty"]);
                bool isProductActive = Convert.ToBoolean(row["ProductActive"]);

                totalQuantity += qty;
                subTotal += itemTotal;
                totalMrp += itemMrpTotal;

                cart.Items.Add(new CartItemResponse
                {
                    EncryptedCartItemId = _encryption.Encrypt(itemId),
                    EncryptedProductId = _encryption.Encrypt(prodId),
                    ProductCode = row["ProductCode"].ToString() ?? "",
                    SKU = row["SKU"].ToString() ?? "",
                    ProductName = row["ProductName"].ToString() ?? "",
                    CategoryName = row["CategoryName"]?.ToString() ?? "General",
                    ShortDescription = row["ShortDescription"]?.ToString(),
                    MRP = mrp,
                    SellingPrice = sp,
                    ItemTotal = itemTotal,
                    Quantity = qty,
                    PrimaryImagePath = row["PrimaryImagePath"]?.ToString(),
                    AvailableQty = available,
                    IsInStock = isProductActive && available >= qty && available > 0,
                    IsActive = isProductActive,
                    AddedDate = Convert.ToDateTime(row["ItemAddedDate"])
                });
            }

            cart.TotalItems = totalQuantity;
            cart.SubTotal = subTotal;
            cart.TotalMRP = totalMrp;
            cart.TotalDiscount = totalMrp > subTotal ? (totalMrp - subTotal) : 0m;

            return cart;
        }

        public async Task<(bool Success, string Message, CartResponse? Cart)> AddItemAsync(
            long customerId,
            long productId,
            int quantity,
            string? ipAddress,
            string? userAgent)
        {
            if (customerId <= 0)
                return (false, "UNAUTHORIZED", null);

            if (productId <= 0)
                return (false, "INVALID_PRODUCT", null);

            if (quantity <= 0)
                return (false, "INVALID_QUANTITY", null);

            long cartId = await GetOrCreateActiveCartIdAsync(customerId, createIfNotFound: true);
            if (cartId <= 0)
                return (false, "CART_NOT_FOUND", null);

            // Execute atomic check and upsert with inventory validation
            string upsertSql = @"
                SET NOCOUNT ON;
                DECLARE @AvailableQty INT;
                DECLARE @IsActive BIT, @IsDeleted BIT;

                SELECT @IsActive = p.IsActive, @IsDeleted = p.IsDeleted, @AvailableQty = ISNULL(s.AvailableQty, 0)
                FROM drs_product_mst p WITH (UPDLOCK, HOLDLOCK)
                LEFT JOIN drs_product_stock_mst s WITH (UPDLOCK, HOLDLOCK) ON s.product_id = p.ID AND s.IsDeleted = 0
                WHERE p.ID = @ProductId;

                IF @IsActive IS NULL
                BEGIN
                    SELECT 'PRODUCT_NOT_FOUND' AS StatusResult, 0 AS NewQuantity;
                    RETURN;
                END

                IF @IsActive = 0 OR @IsDeleted = 1
                BEGIN
                    SELECT 'PRODUCT_INACTIVE' AS StatusResult, 0 AS NewQuantity;
                    RETURN;
                END

                DECLARE @CurrentQty INT = 0;
                DECLARE @CartItemId BIGINT = NULL;

                SELECT @CartItemId = ID, @CurrentQty = Quantity
                FROM drs_cart_item_mst WITH (UPDLOCK, HOLDLOCK)
                WHERE cart_id = @CartId AND product_id = @ProductId AND IsDeleted = 0;

                IF @AvailableQty < (@CurrentQty + @Quantity)
                BEGIN
                    SELECT 'INSUFFICIENT_STOCK' AS StatusResult, 0 AS NewQuantity;
                    RETURN;
                END

                IF @CartItemId IS NOT NULL
                BEGIN
                    UPDATE drs_cart_item_mst
                    SET Quantity = Quantity + @Quantity, ModifiedDate = GETDATE()
                    WHERE ID = @CartItemId;
                    SET @CurrentQty = @CurrentQty + @Quantity;
                END
                ELSE
                BEGIN
                    -- Check if soft-deleted previously
                    UPDATE drs_cart_item_mst
                    SET Quantity = @Quantity, IsDeleted = 0, ModifiedDate = GETDATE()
                    WHERE cart_id = @CartId AND product_id = @ProductId AND IsDeleted = 1;

                    IF @@ROWCOUNT = 0
                    BEGIN
                        BEGIN TRY
                            INSERT INTO drs_cart_item_mst (cart_id, product_id, Quantity, IsActive, IsDeleted, CreatedDate)
                            VALUES (@CartId, @ProductId, @Quantity, 1, 0, GETDATE());
                            SET @CurrentQty = @Quantity;
                        END TRY
                        BEGIN CATCH
                            UPDATE drs_cart_item_mst
                            SET Quantity = Quantity + @Quantity, ModifiedDate = GETDATE()
                            WHERE cart_id = @CartId AND product_id = @ProductId AND IsDeleted = 0;

                            SELECT @CurrentQty = Quantity FROM drs_cart_item_mst WITH (NOLOCK)
                            WHERE cart_id = @CartId AND product_id = @ProductId AND IsDeleted = 0;
                        END CATCH
                    END
                    ELSE
                    BEGIN
                        SET @CurrentQty = @Quantity;
                    END
                END

                SELECT 'CART_ITEM_ADDED' AS StatusResult, @CurrentQty AS NewQuantity;";

            DataTable dt = await _db.ExecuteQueryAsync(upsertSql, new[]
            {
                new SqlParameter("@CartId", cartId),
                new SqlParameter("@ProductId", productId),
                new SqlParameter("@Quantity", quantity)
            });

            if (dt.Rows.Count == 0)
            {
                return (false, "SERVER_ERROR", null);
            }

            string statusResult = dt.Rows[0]["StatusResult"].ToString() ?? "SERVER_ERROR";
            int newTotalQty = Convert.ToInt32(dt.Rows[0]["NewQuantity"]);

            if (statusResult != "CART_ITEM_ADDED")
            {
                return (false, statusResult, null);
            }

            await _audit.InsertAuditAsync(
                customerId,
                "Cart",
                "AddItem",
                "drs_cart_item_mst",
                productId,
                null,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    CustomerId = customerId,
                    CartId = cartId,
                    ProductId = productId,
                    AddedQuantity = quantity,
                    NewTotalQuantity = newTotalQty
                }),
                ipAddress,
                userAgent
            );

            var updatedCart = await GetCustomerCartAsync(customerId);
            return (true, "CART_ITEM_ADDED", updatedCart);
        }

        public async Task<(bool Success, string Message, CartResponse? Cart)> UpdateItemQuantityAsync(
            long customerId,
            long productId,
            int quantity,
            string? ipAddress,
            string? userAgent)
        {
            if (customerId <= 0)
                return (false, "UNAUTHORIZED", null);

            if (productId <= 0)
                return (false, "INVALID_PRODUCT", null);

            if (quantity <= 0)
                return (false, "INVALID_QUANTITY", null);

            long cartId = await GetOrCreateActiveCartIdAsync(customerId, createIfNotFound: false);
            if (cartId <= 0)
                return (false, "CART_NOT_FOUND", null);

            string updateSql = @"
                SET NOCOUNT ON;
                DECLARE @AvailableQty INT;
                DECLARE @IsActive BIT, @IsDeleted BIT;

                SELECT @IsActive = p.IsActive, @IsDeleted = p.IsDeleted, @AvailableQty = ISNULL(s.AvailableQty, 0)
                FROM drs_product_mst p WITH (UPDLOCK, HOLDLOCK)
                LEFT JOIN drs_product_stock_mst s WITH (UPDLOCK, HOLDLOCK) ON s.product_id = p.ID AND s.IsDeleted = 0
                WHERE p.ID = @ProductId;

                IF @IsActive IS NULL
                BEGIN
                    SELECT 'PRODUCT_NOT_FOUND' AS StatusResult, 0 AS OldQuantity;
                    RETURN;
                END

                IF @IsActive = 0 OR @IsDeleted = 1
                BEGIN
                    SELECT 'PRODUCT_INACTIVE' AS StatusResult, 0 AS OldQuantity;
                    RETURN;
                END

                IF @AvailableQty < @NewQuantity
                BEGIN
                    SELECT 'INSUFFICIENT_STOCK' AS StatusResult, 0 AS OldQuantity;
                    RETURN;
                END

                DECLARE @CartItemId BIGINT = NULL;
                DECLARE @OldQty INT = 0;

                SELECT @CartItemId = ID, @OldQty = Quantity
                FROM drs_cart_item_mst WITH (UPDLOCK, HOLDLOCK)
                WHERE cart_id = @CartId AND product_id = @ProductId AND IsDeleted = 0;

                IF @CartItemId IS NULL
                BEGIN
                    SELECT 'CART_ITEM_NOT_FOUND' AS StatusResult, 0 AS OldQuantity;
                    RETURN;
                END

                UPDATE drs_cart_item_mst
                SET Quantity = @NewQuantity, ModifiedDate = GETDATE()
                WHERE ID = @CartItemId;

                SELECT 'CART_ITEM_UPDATED' AS StatusResult, @OldQty AS OldQuantity;";

            DataTable dt = await _db.ExecuteQueryAsync(updateSql, new[]
            {
                new SqlParameter("@CartId", cartId),
                new SqlParameter("@ProductId", productId),
                new SqlParameter("@NewQuantity", quantity)
            });

            if (dt.Rows.Count == 0)
            {
                return (false, "SERVER_ERROR", null);
            }

            string statusResult = dt.Rows[0]["StatusResult"].ToString() ?? "SERVER_ERROR";
            int oldQuantity = Convert.ToInt32(dt.Rows[0]["OldQuantity"]);

            if (statusResult != "CART_ITEM_UPDATED")
            {
                return (false, statusResult, null);
            }

            await _audit.InsertAuditAsync(
                customerId,
                "Cart",
                "UpdateItemQuantity",
                "drs_cart_item_mst",
                productId,
                System.Text.Json.JsonSerializer.Serialize(new { OldQuantity = oldQuantity }),
                System.Text.Json.JsonSerializer.Serialize(new { NewQuantity = quantity }),
                ipAddress,
                userAgent
            );

            var updatedCart = await GetCustomerCartAsync(customerId);
            return (true, "CART_ITEM_UPDATED", updatedCart);
        }

        public async Task<(bool Success, string Message, CartResponse? Cart)> RemoveItemAsync(
            long customerId,
            long productId,
            string? ipAddress,
            string? userAgent)
        {
            if (customerId <= 0)
                return (false, "UNAUTHORIZED", null);

            if (productId <= 0)
                return (false, "INVALID_PRODUCT", null);

            long cartId = await GetOrCreateActiveCartIdAsync(customerId, createIfNotFound: false);
            if (cartId <= 0)
                return (false, "CART_NOT_FOUND", null);

            string removeSql = @"
                UPDATE drs_cart_item_mst
                SET IsDeleted = 1, ModifiedDate = GETDATE()
                WHERE cart_id = @CartId AND product_id = @ProductId AND IsDeleted = 0";

            int rows = await _db.ExecuteNonQueryAsync(removeSql, new[]
            {
                new SqlParameter("@CartId", cartId),
                new SqlParameter("@ProductId", productId)
            });

            if (rows == 0)
            {
                return (false, "CART_ITEM_NOT_FOUND", null);
            }

            await _audit.InsertAuditAsync(
                customerId,
                "Cart",
                "RemoveItem",
                "drs_cart_item_mst",
                productId,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { CartId = cartId, ProductId = productId }),
                ipAddress,
                userAgent
            );

            var updatedCart = await GetCustomerCartAsync(customerId);
            return (true, "CART_ITEM_REMOVED", updatedCart);
        }

        public async Task<(bool Success, string Message)> ClearCartAsync(
            long customerId,
            string? ipAddress,
            string? userAgent)
        {
            if (customerId <= 0)
                return (false, "UNAUTHORIZED");

            long cartId = await GetOrCreateActiveCartIdAsync(customerId, createIfNotFound: false);
            if (cartId <= 0)
                return (true, "CART_CLEARED");

            string clearSql = @"
                UPDATE drs_cart_item_mst
                SET IsDeleted = 1, ModifiedDate = GETDATE()
                WHERE cart_id = @CartId AND IsDeleted = 0";

            await _db.ExecuteNonQueryAsync(clearSql, new[]
            {
                new SqlParameter("@CartId", cartId)
            });

            await _audit.InsertAuditAsync(
                customerId,
                "Cart",
                "ClearCart",
                "drs_cart_item_mst",
                cartId,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { CartId = cartId }),
                ipAddress,
                userAgent
            );

            return (true, "CART_CLEARED");
        }
        #endregion
    }
}
