using System.Data;
using System.Security.Cryptography;
using System.Text;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.Models;
using Microsoft.Data.SqlClient;

namespace Khel_Akhel_Server.BL.Services
{
    public interface IWishlistService
    {
        Task<(WishlistResponse Wishlist, string? IssuedGuestToken)> GetOrCreateWishlistAsync(
            long? customerId,
            string? guestToken
        );
        Task<(bool Success, string Message, string? IssuedGuestToken)> AddItemAsync(
            long? customerId,
            string? guestToken,
            long productId,
            string? ipAddress,
            string? userAgent
        );
        Task<(bool Success, string Message)> RemoveItemAsync(
            long? customerId,
            string? guestToken,
            long productId,
            string? ipAddress,
            string? userAgent
        );
        Task<(bool Success, string Message)> ClearWishlistAsync(
            long? customerId,
            string? guestToken,
            string? ipAddress,
            string? userAgent
        );
        Task<(bool Success, string Message, int MergedCount)> MergeGuestWishlistAsync(
            long customerId,
            string? guestToken,
            string? ipAddress,
            string? userAgent
        );
        Task<(bool Success, string Message, CartResponse? Cart)> MoveToCartAsync(
            long customerId,
            long productId,
            int quantity,
            string? ipAddress,
            string? userAgent
        );
    }

    public class WishlistService : IWishlistService
    {
        private readonly DbHelper _db;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService _audit;
        private readonly ICartService _cartService;

        public WishlistService(
            DbHelper db,
            IUrlEncryptionService encryption,
            IAuditService audit,
            ICartService cartService
        )
        {
            _db = db;
            _encryption = encryption;
            _audit = audit;
            _cartService = cartService;
        }

        #region Helper Token & Hashing Methods
        public static string GenerateGuestToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        public static string HashGuestToken(string token)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
        #endregion

        #region Get Or Create Wishlist Internal
        private async Task<(
            long WishlistId,
            string? IssuedGuestToken,
            bool IsGuest
        )> ResolveWishlistIdAsync(
            long? customerId,
            string? guestToken,
            bool createIfNotFound = true
        )
        {
            if (customerId.HasValue && customerId.Value > 0)
            {
                // Authenticated customer path with concurrency protection
                string query =
                    @"
DECLARE @WishlistId BIGINT;
SELECT TOP 1 @WishlistId = ID FROM drs_wishlist_mst WITH (UPDLOCK, HOLDLOCK)
WHERE customer_id = @CustomerId AND IsDeleted = 0;

IF @WishlistId IS NULL AND @CreateIfNotFound = 1
BEGIN
BEGIN TRY
INSERT INTO drs_wishlist_mst (customer_id, GuestTokenHash, IsActive, IsDeleted, CreatedDate)
VALUES (@CustomerId, NULL, 1, 0, GETDATE());
SET @WishlistId = SCOPE_IDENTITY();
END TRY
BEGIN CATCH
SELECT TOP 1 @WishlistId = ID FROM drs_wishlist_mst WITH (NOLOCK)
WHERE customer_id = @CustomerId AND IsDeleted = 0;
END CATCH
END
SELECT ISNULL(@WishlistId, 0);";

                object? idObj = await _db.ExecuteScalarAsync(
                    query,
                    new[]
                    {
                        new SqlParameter("@CustomerId", customerId.Value),
                        new SqlParameter("@CreateIfNotFound", createIfNotFound ? 1 : 0),
                    }
                );

                long id = idObj != null && idObj != DBNull.Value ? Convert.ToInt64(idObj) : 0;
                return (id, null, false);
            }
            else
            {
                // Guest path
                string? effectiveGuestToken = guestToken;
                string? newlyIssuedToken = null;

                if (!string.IsNullOrWhiteSpace(effectiveGuestToken))
                {
                    string tokenHash = HashGuestToken(effectiveGuestToken);
                    string selectGuestQuery =
                        @"
SELECT TOP 1 ID FROM drs_wishlist_mst WITH (NOLOCK)
WHERE GuestTokenHash = @TokenHash AND IsDeleted = 0";

                    object? existingGuestId = await _db.ExecuteScalarAsync(
                        selectGuestQuery,
                        new[] { new SqlParameter("@TokenHash", tokenHash) }
                    );

                    if (existingGuestId != null && existingGuestId != DBNull.Value)
                    {
                        return (Convert.ToInt64(existingGuestId), null, true);
                    }
                }

                if (!createIfNotFound)
                    return (0, null, true);

                // Create new guest wishlist with high-entropy token and concurrency protection
                newlyIssuedToken = GenerateGuestToken();
                string newHash = HashGuestToken(newlyIssuedToken);

                string insertGuestQuery =
                    @"
DECLARE @GuestWishlistId BIGINT;
BEGIN TRY
INSERT INTO drs_wishlist_mst (customer_id, GuestTokenHash, IsActive, IsDeleted, CreatedDate)
VALUES (NULL, @TokenHash, 1, 0, GETDATE());
SET @GuestWishlistId = SCOPE_IDENTITY();
END TRY
BEGIN CATCH
SELECT TOP 1 @GuestWishlistId = ID FROM drs_wishlist_mst WITH (NOLOCK)
WHERE GuestTokenHash = @TokenHash AND IsDeleted = 0;
END CATCH
SELECT ISNULL(@GuestWishlistId, 0);";

                object? newGuestId = await _db.ExecuteScalarAsync(
                    insertGuestQuery,
                    new[] { new SqlParameter("@TokenHash", newHash) }
                );

                return (Convert.ToInt64(newGuestId), newlyIssuedToken, true);
            }
        }
        #endregion

        #region Public Wishlist Methods
        public async Task<(
            WishlistResponse Wishlist,
            string? IssuedGuestToken
        )> GetOrCreateWishlistAsync(long? customerId, string? guestToken)
        {
            var (wishlistId, issuedToken, isGuest) = await ResolveWishlistIdAsync(
                customerId,
                guestToken,
                createIfNotFound: true
            );

            string itemsQuery =
                @"
SELECT 
wi.ID AS WishlistItemId,
wi.product_id,
p.ProductCode,
p.SKU,
p.ProductName,
c.CategoryName,
p.ShortDescription,
p.MRP,
p.SellingPrice,
p.IsActive AS ProductActive,
ISNULL(s.AvailableQty, 0) AS AvailableQty,
img.ImagePath AS PrimaryImagePath,
wi.CreatedDate AS AddedDate
FROM drs_wishlist_item_mst wi WITH (NOLOCK)
INNER JOIN drs_product_mst p WITH (NOLOCK) ON p.ID = wi.product_id AND p.IsDeleted = 0
LEFT JOIN drs_product_category_mst c WITH (NOLOCK) ON c.ID = p.product_category_id AND c.IsDeleted = 0
LEFT JOIN drs_product_stock_mst s WITH (NOLOCK) ON s.product_id = p.ID AND s.IsDeleted = 0
LEFT JOIN drs_product_image_mst img WITH (NOLOCK) ON img.product_id = p.ID AND img.IsDefault = 1 AND img.IsDeleted = 0
WHERE wi.wishlist_id = @WishlistId AND wi.IsDeleted = 0
ORDER BY wi.CreatedDate DESC";

            DataTable dt = await _db.ExecuteQueryAsync(
                itemsQuery,
                new[] { new SqlParameter("@WishlistId", wishlistId) }
            );

            var response = new WishlistResponse
            {
                EncryptedWishlistId = _encryption.Encrypt(wishlistId),
                IsGuest = isGuest,
                TotalItems = dt.Rows.Count,
                Items = new List<WishlistItemResponse>(),
            };

            foreach (DataRow row in dt.Rows)
            {
                long prodId = Convert.ToInt64(row["product_id"]);
                long itemId = Convert.ToInt64(row["WishlistItemId"]);
                decimal mrp = Convert.ToDecimal(row["MRP"]);
                decimal sp = Convert.ToDecimal(row["SellingPrice"]);
                decimal discount =
                    mrp > 0 && sp < mrp ? Math.Round(((mrp - sp) / mrp) * 100m, 2) : 0m;
                int available = Convert.ToInt32(row["AvailableQty"]);
                bool isProductActive = Convert.ToBoolean(row["ProductActive"]);

                response.Items.Add(
                    new WishlistItemResponse
                    {
                        EncryptedWishlistItemId = _encryption.Encrypt(itemId),
                        EncryptedProductId = _encryption.Encrypt(prodId),
                        ProductCode = row["ProductCode"].ToString() ?? "",
                        SKU = row["SKU"].ToString() ?? "",
                        ProductName = row["ProductName"].ToString() ?? "",
                        CategoryName = row["CategoryName"]?.ToString() ?? "General",
                        ShortDescription = row["ShortDescription"]?.ToString(),
                        MRP = mrp,
                        SellingPrice = sp,
                        DiscountPercentage = discount,
                        PrimaryImagePath = row["PrimaryImagePath"]?.ToString(),
                        AvailableQty = available,
                        IsInStock = isProductActive && available > 0,
                        IsActive = isProductActive,
                        AddedDate = Convert.ToDateTime(row["AddedDate"]),
                    }
                );
            }

            return (response, issuedToken);
        }

        public async Task<(bool Success, string Message, string? IssuedGuestToken)> AddItemAsync(
            long? customerId,
            string? guestToken,
            long productId,
            string? ipAddress,
            string? userAgent
        )
        {
            if (productId <= 0)
                return (false, "INVALID_PRODUCT", null);

            // Verify product existence and active status
            string productCheckQuery =
                @"
SELECT COUNT(1) FROM drs_product_mst WITH (NOLOCK)
WHERE ID = @ProductId AND IsDeleted = 0 AND IsActive = 1";

            int productValid = Convert.ToInt32(
                await _db.ExecuteScalarAsync(
                    productCheckQuery,
                    new[] { new SqlParameter("@ProductId", productId) }
                ) ?? 0
            );

            if (productValid == 0)
            {
                return (false, "PRODUCT_INACTIVE", null);
            }

            var (wishlistId, issuedToken, _) = await ResolveWishlistIdAsync(
                customerId,
                guestToken,
                createIfNotFound: true
            );
            if (wishlistId <= 0)
            {
                return (false, "WISHLIST_NOT_FOUND", null);
            }

            // Atomic idempotent addition: reactivate or insert with concurrency protection
            string upsertSql =
                @"
SET NOCOUNT ON;
IF NOT EXISTS (
SELECT 1 FROM drs_wishlist_item_mst WITH (UPDLOCK, HOLDLOCK)
WHERE wishlist_id = @WishlistId AND product_id = @ProductId AND IsDeleted = 0
)
BEGIN
UPDATE drs_wishlist_item_mst
SET IsDeleted = 0, ModifiedDate = GETDATE()
WHERE wishlist_id = @WishlistId AND product_id = @ProductId AND IsDeleted = 1;

IF @@ROWCOUNT = 0
BEGIN
BEGIN TRY
INSERT INTO drs_wishlist_item_mst (wishlist_id, product_id, IsActive, IsDeleted, CreatedDate)
VALUES (@WishlistId, @ProductId, 1, 0, GETDATE());
END TRY
BEGIN CATCH
-- Concurrently inserted by parallel request; idempotent success
END CATCH
END
END";

            await _db.ExecuteNonQueryAsync(
                upsertSql,
                new[]
                {
                    new SqlParameter("@WishlistId", wishlistId),
                    new SqlParameter("@ProductId", productId),
                }
            );

            await _audit.InsertAuditAsync(
                customerId,
                "Wishlist",
                "AddItem",
                "drs_wishlist_item_mst",
                productId,
                null,
                System.Text.Json.JsonSerializer.Serialize(
                    new { WishlistId = wishlistId, ProductId = productId }
                ),
                ipAddress,
                userAgent
            );

            return (true, "WISHLIST_ITEM_ADDED", issuedToken);
        }

        public async Task<(bool Success, string Message)> RemoveItemAsync(
            long? customerId,
            string? guestToken,
            long productId,
            string? ipAddress,
            string? userAgent
        )
        {
            if (productId <= 0)
                return (false, "INVALID_PRODUCT");

            var (wishlistId, _, _) = await ResolveWishlistIdAsync(
                customerId,
                guestToken,
                createIfNotFound: false
            );
            if (wishlistId <= 0)
                return (false, "WISHLIST_NOT_FOUND");

            string removeQuery =
                @"
UPDATE drs_wishlist_item_mst
SET IsDeleted = 1, ModifiedDate = GETDATE()
WHERE wishlist_id = @WishlistId AND product_id = @ProductId AND IsDeleted = 0";

            int rows = await _db.ExecuteNonQueryAsync(
                removeQuery,
                new[]
                {
                    new SqlParameter("@WishlistId", wishlistId),
                    new SqlParameter("@ProductId", productId),
                }
            );

            if (rows == 0)
            {
                return (false, "WISHLIST_ITEM_NOT_FOUND");
            }

            await _audit.InsertAuditAsync(
                customerId,
                "Wishlist",
                "RemoveItem",
                "drs_wishlist_item_mst",
                productId,
                null,
                System.Text.Json.JsonSerializer.Serialize(
                    new { WishlistId = wishlistId, ProductId = productId }
                ),
                ipAddress,
                userAgent
            );

            return (true, "WISHLIST_ITEM_REMOVED");
        }

        public async Task<(bool Success, string Message)> ClearWishlistAsync(
            long? customerId,
            string? guestToken,
            string? ipAddress,
            string? userAgent
        )
        {
            var (wishlistId, _, _) = await ResolveWishlistIdAsync(
                customerId,
                guestToken,
                createIfNotFound: false
            );
            if (wishlistId <= 0)
                return (true, "WISHLIST_CLEARED");

            string clearQuery =
                @"
UPDATE drs_wishlist_item_mst
SET IsDeleted = 1, ModifiedDate = GETDATE()
WHERE wishlist_id = @WishlistId AND IsDeleted = 0";

            await _db.ExecuteNonQueryAsync(clearQuery, new[] { new SqlParameter("@WishlistId", wishlistId) });

            await _audit.InsertAuditAsync(
                customerId,
                "Wishlist",
                "ClearWishlist",
                "drs_wishlist_item_mst",
                wishlistId,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { WishlistId = wishlistId }),
                ipAddress,
                userAgent
            );

            return (true, "WISHLIST_CLEARED");
        }

        public async Task<(bool Success, string Message, int MergedCount)> MergeGuestWishlistAsync(
            long customerId,
            string? guestToken,
            string? ipAddress,
            string? userAgent
        )
        {
            if (customerId <= 0)
                return (false, "UNAUTHORIZED", 0);

            if (string.IsNullOrWhiteSpace(guestToken))
                return (true, "WISHLIST_MERGED", 0);

            string tokenHash = HashGuestToken(guestToken);

            using SqlConnection con = await _db.GetOpenConnectionAsync();
            using SqlTransaction tx = con.BeginTransaction(IsolationLevel.ReadCommitted);

            try
            {
                // 1. Resolve and lock guest wishlist inside transaction
                string selectGuestQuery =
                    @"
SELECT TOP 1 ID FROM drs_wishlist_mst WITH (UPDLOCK, HOLDLOCK)
WHERE GuestTokenHash = @TokenHash AND IsDeleted = 0";

                long guestWishlistId = 0;
                using (SqlCommand selectGuestCmd = new SqlCommand(selectGuestQuery, con, tx))
                {
                    selectGuestCmd.Parameters.AddWithValue("@TokenHash", tokenHash);
                    object? gObj = await selectGuestCmd.ExecuteScalarAsync();
                    if (gObj != null && gObj != DBNull.Value)
                    {
                        guestWishlistId = Convert.ToInt64(gObj);
                    }
                }

                if (guestWishlistId <= 0)
                {
                    // Nothing to merge (guest wishlist was empty or already migrated)
                    await tx.CommitAsync();
                    return (true, "WISHLIST_MERGED", 0);
                }

                // 2. Resolve or create customer wishlist inside the EXACT SAME transaction
                string resolveCustQuery =
                    @"
DECLARE @CustWishlistId BIGINT;
SELECT TOP 1 @CustWishlistId = ID FROM drs_wishlist_mst WITH (UPDLOCK, HOLDLOCK)
WHERE customer_id = @CustomerId AND IsDeleted = 0;

IF @CustWishlistId IS NULL
BEGIN
BEGIN TRY
INSERT INTO drs_wishlist_mst (customer_id, GuestTokenHash, IsActive, IsDeleted, CreatedDate)
VALUES (@CustomerId, NULL, 1, 0, GETDATE());
SET @CustWishlistId = SCOPE_IDENTITY();
END TRY
BEGIN CATCH
SELECT TOP 1 @CustWishlistId = ID FROM drs_wishlist_mst WITH (NOLOCK)
WHERE customer_id = @CustomerId AND IsDeleted = 0;
END CATCH
END
SELECT ISNULL(@CustWishlistId, 0);";

                long customerWishlistId = 0;
                using (SqlCommand resCmd = new SqlCommand(resolveCustQuery, con, tx))
                {
                    resCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    customerWishlistId = Convert.ToInt64(await resCmd.ExecuteScalarAsync());
                }

                // 3. Fetch guest items that are active and not deleted in product master
                string getItemsSql =
                    @"
SELECT wi.product_id
FROM drs_wishlist_item_mst wi WITH (UPDLOCK, ROWLOCK)
INNER JOIN drs_product_mst p WITH (UPDLOCK, ROWLOCK) ON p.ID = wi.product_id AND p.IsDeleted = 0 AND p.IsActive = 1
WHERE wi.wishlist_id = @GuestWishlistId AND wi.IsDeleted = 0";

                List<long> productIdsToMerge = new List<long>();
                using (SqlCommand getCmd = new SqlCommand(getItemsSql, con, tx))
                {
                    getCmd.Parameters.AddWithValue("@GuestWishlistId", guestWishlistId);
                    using SqlDataReader rdr = await getCmd.ExecuteReaderAsync();
                    while (await rdr.ReadAsync())
                    {
                        productIdsToMerge.Add(rdr.GetInt64(0));
                    }
                }

                int merged = 0;
                foreach (long prodId in productIdsToMerge)
                {
                    // Check if already in customer wishlist
                    string checkCustSql =
                        @"
SELECT COUNT(1) FROM drs_wishlist_item_mst WITH (UPDLOCK, ROWLOCK)
WHERE wishlist_id = @CustomerWishlistId AND product_id = @ProductId AND IsDeleted = 0";

                    int existsInCust = 0;
                    using (SqlCommand checkCmd = new SqlCommand(checkCustSql, con, tx))
                    {
                        checkCmd.Parameters.AddWithValue("@CustomerWishlistId", customerWishlistId);
                        checkCmd.Parameters.AddWithValue("@ProductId", prodId);
                        existsInCust = Convert.ToInt32(await checkCmd.ExecuteScalarAsync() ?? 0);
                    }

                    if (existsInCust == 0)
                    {
                        string reactivateSql =
                            @"
UPDATE drs_wishlist_item_mst
SET IsDeleted = 0, ModifiedDate = GETDATE()
WHERE wishlist_id = @CustomerWishlistId AND product_id = @ProductId AND IsDeleted = 1";

                        int reactivated = 0;
                        using (SqlCommand reactCmd = new SqlCommand(reactivateSql, con, tx))
                        {
                            reactCmd.Parameters.AddWithValue(
                                "@CustomerWishlistId",
                                customerWishlistId
                            );
                            reactCmd.Parameters.AddWithValue("@ProductId", prodId);
                            reactivated = await reactCmd.ExecuteNonQueryAsync();
                        }

                        if (reactivated == 0)
                        {
                            string insertSql =
                                @"
BEGIN TRY
INSERT INTO drs_wishlist_item_mst (wishlist_id, product_id, IsActive, IsDeleted, CreatedDate)
VALUES (@CustomerWishlistId, @ProductId, 1, 0, GETDATE());
END TRY
BEGIN CATCH
-- Handled by parallel merge; idempotent
END CATCH";

                            using SqlCommand insCmd = new SqlCommand(insertSql, con, tx);
                            insCmd.Parameters.AddWithValue(
                                "@CustomerWishlistId",
                                customerWishlistId
                            );
                            insCmd.Parameters.AddWithValue("@ProductId", prodId);
                            await insCmd.ExecuteNonQueryAsync();
                        }

                        merged++;
                    }
                }

                // 4. Soft-delete items from guest wishlist
                string clearGuestItemsSql =
                    @"
UPDATE drs_wishlist_item_mst
SET IsDeleted = 1, ModifiedDate = GETDATE()
WHERE wishlist_id = @GuestWishlistId AND IsDeleted = 0";

                using (SqlCommand clearItemsCmd = new SqlCommand(clearGuestItemsSql, con, tx))
                {
                    clearItemsCmd.Parameters.AddWithValue("@GuestWishlistId", guestWishlistId);
                    await clearItemsCmd.ExecuteNonQueryAsync();
                }

                // 5. Soft-delete the guest wishlist record so it cannot be reused
                string deleteGuestWishlistSql =
                    @"
UPDATE drs_wishlist_mst
SET IsDeleted = 1, ModifiedDate = GETDATE()
WHERE ID = @GuestWishlistId";

                using (SqlCommand delWishlistCmd = new SqlCommand(deleteGuestWishlistSql, con, tx))
                {
                    delWishlistCmd.Parameters.AddWithValue("@GuestWishlistId", guestWishlistId);
                    await delWishlistCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                await _audit.InsertAuditAsync(
                    customerId,
                    "Wishlist",
                    "MergeGuestWishlist",
                    "drs_wishlist_mst",
                    customerWishlistId,
                    null,
                    System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            GuestWishlistId = guestWishlistId,
                            CustomerWishlistId = customerWishlistId,
                            MergedCount = merged,
                        }
                    ),
                    ipAddress,
                    userAgent
                );

                return (true, "WISHLIST_MERGED", merged);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                Logs.Error(
                    $"MergeGuestWishlistAsync transaction failed for CustomerId:{customerId}",
                    ex
                );
                throw;
            }
        }

        public async Task<(bool Success, string Message, CartResponse? Cart)> MoveToCartAsync(
            long customerId,
            long productId,
            int quantity,
            string? ipAddress,
            string? userAgent
        )
        {
            if (customerId <= 0)
                return (false, "UNAUTHORIZED", null);

            if (productId <= 0)
                return (false, "INVALID_PRODUCT", null);

            int qty = quantity > 0 ? quantity : 1;

            using SqlConnection con = await _db.GetOpenConnectionAsync();
            using SqlTransaction tx = con.BeginTransaction(IsolationLevel.ReadCommitted);

            try
            {
                // 1. Verify customer wishlist & presence of product in customer's wishlist
                string getWishlistSql =
                    @"
SELECT TOP 1 w.ID AS WishlistId, wi.ID AS WishlistItemId
FROM drs_wishlist_mst w WITH (UPDLOCK, ROWLOCK)
INNER JOIN drs_wishlist_item_mst wi WITH (UPDLOCK, ROWLOCK) 
ON wi.wishlist_id = w.ID AND wi.product_id = @ProductId AND wi.IsDeleted = 0
WHERE w.customer_id = @CustomerId AND w.IsDeleted = 0";

                long wishlistId = 0;
                long wishlistItemId = 0;

                using (SqlCommand wishCmd = new SqlCommand(getWishlistSql, con, tx))
                {
                    wishCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    wishCmd.Parameters.AddWithValue("@ProductId", productId);
                    using SqlDataReader rdr = await wishCmd.ExecuteReaderAsync();
                    if (await rdr.ReadAsync())
                    {
                        wishlistId = rdr.GetInt64(0);
                        wishlistItemId = rdr.GetInt64(1);
                    }
                }

                if (wishlistItemId <= 0)
                {
                    await tx.RollbackAsync();
                    return (false, "WISHLIST_ITEM_NOT_FOUND", null);
                }

                // 2. Validate product exists, is active, and stock is available
                string checkStockSql =
                    @"
SELECT p.IsActive, p.IsDeleted, ISNULL(s.AvailableQty, 0) AS AvailableQty
FROM drs_product_mst p WITH (UPDLOCK, ROWLOCK)
LEFT JOIN drs_product_stock_mst s WITH (UPDLOCK, ROWLOCK) ON s.product_id = p.ID AND s.IsDeleted = 0
WHERE p.ID = @ProductId";

                int availableQty = 0;
                using (SqlCommand prodCmd = new SqlCommand(checkStockSql, con, tx))
                {
                    prodCmd.Parameters.AddWithValue("@ProductId", productId);
                    using SqlDataReader prodRdr = await prodCmd.ExecuteReaderAsync();
                    if (!await prodRdr.ReadAsync())
                    {
                        await tx.RollbackAsync();
                        return (false, "PRODUCT_NOT_FOUND", null);
                    }

                    bool isActive = prodRdr.GetBoolean(0);
                    bool isDeleted = prodRdr.GetBoolean(1);
                    availableQty = prodRdr.GetInt32(2);

                    if (!isActive || isDeleted)
                    {
                        await tx.RollbackAsync();
                        return (false, "PRODUCT_INACTIVE", null);
                    }

                    if (availableQty < qty)
                    {
                        await tx.RollbackAsync();
                        return (false, "INSUFFICIENT_STOCK", null);
                    }
                }

                // 3. Resolve or create customer active cart with concurrency protection
                string cartSelectSql =
                    @"
DECLARE @CartId BIGINT;
SELECT TOP 1 @CartId = ID FROM drs_cart_mst WITH (UPDLOCK, HOLDLOCK)
WHERE customer_id = @CustomerId AND CartStatus = 'Active' AND IsDeleted = 0;

IF @CartId IS NULL
BEGIN
BEGIN TRY
INSERT INTO drs_cart_mst (customer_id, CartStatus, IsActive, IsDeleted, CreatedDate)
VALUES (@CustomerId, 'Active', 1, 0, GETDATE());
SET @CartId = SCOPE_IDENTITY();
END TRY
BEGIN CATCH
SELECT TOP 1 @CartId = ID FROM drs_cart_mst WITH (UPDLOCK, HOLDLOCK)
WHERE customer_id = @CustomerId AND CartStatus = 'Active' AND IsDeleted = 0;
END CATCH
END
SELECT ISNULL(@CartId, 0);";

                long cartId = 0;
                using (SqlCommand cartSelectCmd = new SqlCommand(cartSelectSql, con, tx))
                {
                    cartSelectCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    object? cObj = await cartSelectCmd.ExecuteScalarAsync();
                    if (cObj != null && cObj != DBNull.Value)
                    {
                        cartId = Convert.ToInt64(cObj);
                    }
                }

                // 4. Add or increment in cart
                string checkCartItemSql =
                    @"
SELECT ID, Quantity FROM drs_cart_item_mst WITH (UPDLOCK, ROWLOCK)
WHERE cart_id = @CartId AND product_id = @ProductId AND IsDeleted = 0";

                long existingCartItemId = 0;
                int existingQty = 0;

                using (SqlCommand ciCmd = new SqlCommand(checkCartItemSql, con, tx))
                {
                    ciCmd.Parameters.AddWithValue("@CartId", cartId);
                    ciCmd.Parameters.AddWithValue("@ProductId", productId);
                    using SqlDataReader ciRdr = await ciCmd.ExecuteReaderAsync();
                    if (await ciRdr.ReadAsync())
                    {
                        existingCartItemId = ciRdr.GetInt64(0);
                        existingQty = ciRdr.GetInt32(1);
                    }
                }

                if (existingCartItemId > 0)
                {
                    // Cumulative stock validation when item already present in cart
                    if (availableQty < (existingQty + qty))
                    {
                        await tx.RollbackAsync();
                        return (false, "INSUFFICIENT_STOCK", null);
                    }

                    string updateCartItemSql =
                        @"
UPDATE drs_cart_item_mst
SET Quantity = Quantity + @Qty, ModifiedDate = GETDATE()
WHERE ID = @CartItemId";

                    using SqlCommand uCmd = new SqlCommand(updateCartItemSql, con, tx);
                    uCmd.Parameters.AddWithValue("@Qty", qty);
                    uCmd.Parameters.AddWithValue("@CartItemId", existingCartItemId);
                    await uCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Check if soft-deleted previously in cart: reactivate or insert new
                    string reactivateCartItemSql =
                        @"
UPDATE drs_cart_item_mst
SET Quantity = @Qty, IsDeleted = 0, ModifiedDate = GETDATE()
WHERE cart_id = @CartId AND product_id = @ProductId AND IsDeleted = 1";

                    using (SqlCommand reactCmd = new SqlCommand(reactivateCartItemSql, con, tx))
                    {
                        reactCmd.Parameters.AddWithValue("@Qty", qty);
                        reactCmd.Parameters.AddWithValue("@CartId", cartId);
                        reactCmd.Parameters.AddWithValue("@ProductId", productId);
                        int reactivated = await reactCmd.ExecuteNonQueryAsync();

                        if (reactivated == 0)
                        {
                            string insertCartItemSql =
                                @"
BEGIN TRY
INSERT INTO drs_cart_item_mst (cart_id, product_id, Quantity, IsActive, IsDeleted, CreatedDate)
VALUES (@CartId, @ProductId, @Qty, 1, 0, GETDATE());
END TRY
BEGIN CATCH
UPDATE drs_cart_item_mst
SET Quantity = Quantity + @Qty, ModifiedDate = GETDATE()
WHERE cart_id = @CartId AND product_id = @ProductId AND IsDeleted = 0;
END CATCH";

                            using SqlCommand iCmd = new SqlCommand(insertCartItemSql, con, tx);
                            iCmd.Parameters.AddWithValue("@CartId", cartId);
                            iCmd.Parameters.AddWithValue("@ProductId", productId);
                            iCmd.Parameters.AddWithValue("@Qty", qty);
                            await iCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 5. Remove item from wishlist atomically
                string removeWishlistSql =
                    @"
UPDATE drs_wishlist_item_mst
SET IsDeleted = 1, ModifiedDate = GETDATE()
WHERE ID = @WishlistItemId";

                using (SqlCommand remCmd = new SqlCommand(removeWishlistSql, con, tx))
                {
                    remCmd.Parameters.AddWithValue("@WishlistItemId", wishlistItemId);
                    await remCmd.ExecuteNonQueryAsync();
                }

                // Commit transaction
                await tx.CommitAsync();

                // Audit
                await _audit.InsertAuditAsync(
                    customerId,
                    "Wishlist",
                    "MoveToCart",
                    "drs_cart_item_mst",
                    productId,
                    null,
                    System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            CustomerId = customerId,
                            ProductId = productId,
                            Quantity = qty,
                            WishlistId = wishlistId,
                            CartId = cartId,
                        }
                    ),
                    ipAddress,
                    userAgent
                );

                // Fetch refreshed cart representation
                var cartResponse = await _cartService.GetCustomerCartAsync(customerId);
                return (true, "ITEM_MOVED_TO_CART", cartResponse);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                Logs.Error(
                    $"MoveToCartAsync transaction failed | CustomerId:{customerId} ProductId:{productId}",
                    ex
                );
                throw;
            }
        }
        #endregion
    }
}
