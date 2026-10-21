using System.Data;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.BL.Services;
using Khel_Akhel_Server.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Khel_Akhel_Server.Tests
{
    public class CartAndWishlistDatabaseIntegrationTests : IDisposable
    {
        private readonly string? _connectionString;
        private readonly DbHelper? _db;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService _audit;
        private readonly ICartService? _cartService;
        private readonly IWishlistService? _wishlistService;

        private readonly List<long> _createdCustomerIds = new();
        private readonly List<long> _createdProductIds = new();
        private readonly List<long> _createdCategoryIds = new();

        public CartAndWishlistDatabaseIntegrationTests()
        {
            _connectionString = GetWorkingConnectionString();

            var inMemorySettings = new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", _connectionString ?? "Server=(localdb)\\MSSQLLocalDB;Database=KhelAkhe_CRM;Trusted_Connection=True;TrustServerCertificate=True;" },
                { "Security:UrlEncryptionKey", "TestUrlEncryptionKey2026_Secure32BytesLength!" },
                { "Jwt:Key", "TestJwtKeyForUnitTesting2026Secure32BytesKey!" }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _encryption = new UrlEncryptionService(configuration);

            if (_connectionString != null)
            {
                _db = new DbHelper(configuration);
                _audit = new AuditService(_db);
                _cartService = new CartService(_db, _encryption, _audit);
                _wishlistService = new WishlistService(_db, _encryption, _audit, _cartService);
            }
            else
            {
                _audit = null!;
            }
        }

        public static string? GetWorkingConnectionString()
        {
            var candidates = new[]
            {
                "Server=(localdb)\\MSSQLLocalDB;Database=KhelAkhe_CRM;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;",
                "Server=.;Database=KhelAkhe_CRM;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;",
                "Server=localhost;Database=KhelAkhe_CRM;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;"
            };

            foreach (var connStr in candidates)
            {
                try
                {
                    using var conn = new SqlConnection(connStr);
                    conn.Open();
                    return connStr;
                }
                catch
                {
                    // Continue to next candidate
                }
            }

            return null;
        }

        #region Helper: Create Test Entities
        private async Task<(long CustomerId, long ProductId)> SetupTestCustomerAndProductAsync(int availableQty = 10, decimal price = 100m)
        {
            Assert.NotNull(_db);

            // 1. Create unique test category
            string catQuery = @"
                INSERT INTO drs_product_category_mst (CategoryName, Description, DisplayOrder, IsActive, IsDeleted, CreatedDate)
                OUTPUT INSERTED.ID
                VALUES (@CatName, 'Test Category', 1, 1, 0, GETDATE())";

            string catName = "TestCat_" + Guid.NewGuid().ToString("N")[..8];
            long catId = Convert.ToInt64(await _db.ExecuteScalarAsync(catQuery, new[]
            {
                new SqlParameter("@CatName", catName)
            }));
            _createdCategoryIds.Add(catId);

            // 2. Create unique test product
            string prodCode = "TC_" + Guid.NewGuid().ToString("N")[..8];
            string prodQuery = @"
                INSERT INTO drs_product_mst 
                (product_category_id, ProductName, ProductCode, SKU, ShortDescription, Description, MRP, SellingPrice, IsActive, IsDeleted, CreatedDate)
                OUTPUT INSERTED.ID
                VALUES (@CatId, @ProdName, @ProdCode, @SKU, 'Short Desc', 'Full Desc', @MRP, @SP, 1, 0, GETDATE())";

            long prodId = Convert.ToInt64(await _db.ExecuteScalarAsync(prodQuery, new[]
            {
                new SqlParameter("@CatId", catId),
                new SqlParameter("@ProdName", "Product_" + prodCode),
                new SqlParameter("@ProdCode", prodCode),
                new SqlParameter("@SKU", "SKU_" + prodCode),
                new SqlParameter("@MRP", price * 1.25m),
                new SqlParameter("@SP", price)
            }));
            _createdProductIds.Add(prodId);

            // 3. Create stock
            string stockQuery = @"
                INSERT INTO drs_product_stock_mst (product_id, AvailableQty, ReservedQty, IsActive, IsDeleted, CreatedDate)
                VALUES (@ProdId, @Qty, 0, 1, 0, GETDATE())";

            _db.ExecuteNonQuery(stockQuery, new[]
            {
                new SqlParameter("@ProdId", prodId),
                new SqlParameter("@Qty", availableQty)
            });

            // 4. Create unique test customer
            string accNo = Guid.NewGuid().ToString("N")[..9].ToUpper();
            string email = $"test_{Guid.NewGuid():N}@khelakhel.com";
            string mobileNo = "9" + Random.Shared.Next(100000000, 999999999).ToString();
            string custQuery = @"
                INSERT INTO drs_customer_mst 
                (Account_no, FirstName, LastName, Email, MobileNo, PasswordHash, IsActive, IsDeleted, CreatedDate)
                OUTPUT INSERTED.ID
                VALUES (@AccNo, 'TestFirst', 'TestLast', @Email, @MobileNo, 'hashedpwd', 1, 0, GETDATE())";

            long custId = Convert.ToInt64(await _db.ExecuteScalarAsync(custQuery, new[]
            {
                new SqlParameter("@AccNo", accNo),
                new SqlParameter("@Email", email),
                new SqlParameter("@MobileNo", mobileNo)
            }));
            _createdCustomerIds.Add(custId);

            return (custId, prodId);
        }
        #endregion

        #region Clean Up
        public void Dispose()
        {
            if (_db == null) return;

            try
            {
                foreach (long custId in _createdCustomerIds)
                {
                    _db.ExecuteNonQuery("DELETE FROM drs_cart_item_mst WHERE cart_id IN (SELECT ID FROM drs_cart_mst WHERE customer_id = @Id)", new[] { new SqlParameter("@Id", custId) });
                    _db.ExecuteNonQuery("DELETE FROM drs_cart_mst WHERE customer_id = @Id", new[] { new SqlParameter("@Id", custId) });
                    _db.ExecuteNonQuery("DELETE FROM drs_wishlist_item_mst WHERE wishlist_id IN (SELECT ID FROM drs_wishlist_mst WHERE customer_id = @Id)", new[] { new SqlParameter("@Id", custId) });
                    _db.ExecuteNonQuery("DELETE FROM drs_wishlist_mst WHERE customer_id = @Id", new[] { new SqlParameter("@Id", custId) });
                    _db.ExecuteNonQuery("DELETE FROM drs_customer_mst WHERE ID = @Id", new[] { new SqlParameter("@Id", custId) });
                }

                foreach (long prodId in _createdProductIds)
                {
                    _db.ExecuteNonQuery("DELETE FROM drs_product_stock_mst WHERE product_id = @Id", new[] { new SqlParameter("@Id", prodId) });
                    _db.ExecuteNonQuery("DELETE FROM drs_cart_item_mst WHERE product_id = @Id", new[] { new SqlParameter("@Id", prodId) });
                    _db.ExecuteNonQuery("DELETE FROM drs_wishlist_item_mst WHERE product_id = @Id", new[] { new SqlParameter("@Id", prodId) });
                    _db.ExecuteNonQuery("DELETE FROM drs_product_mst WHERE ID = @Id", new[] { new SqlParameter("@Id", prodId) });
                }

                foreach (long catId in _createdCategoryIds)
                {
                    _db.ExecuteNonQuery("DELETE FROM drs_product_category_mst WHERE ID = @Id", new[] { new SqlParameter("@Id", catId) });
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        #endregion

        #region Integration Tests
        [Fact]
        public async Task SQL_Migration_CanRerunMultipleTimes_WithoutErrors()
        {
            Assert.NotNull(_connectionString);

            // Read the real migration script
            string migrationPath = @"F:\Projects\Khel-Akhel\DBScripts\Migration_Cart_And_Wishlist.sql";
            Assert.True(File.Exists(migrationPath));
            string script = await File.ReadAllTextAsync(migrationPath);

            // Split on GO batches
            var batches = System.Text.RegularExpressions.Regex.Split(
                script,
                @"^\s*GO\s*$",
                System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            foreach (var batch in batches)
            {
                string trimmed = batch.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                // Execute DDL batch directly via SqlConnection to verify migration idempotency
                using var cmd = new SqlCommand(trimmed, conn);
                cmd.ExecuteNonQuery();
            }
        }

        [Fact]
        public async Task GuestWishlist_FullLifecycle_TokenHashing_And_SecurityIsolation()
        {
            Assert.NotNull(_wishlistService);
            Assert.NotNull(_db);

            var (_, prodId) = await SetupTestCustomerAndProductAsync();

            // 1. Guest creates wishlist by adding an item without prior token
            var (addSuccess, addMsg, issuedToken) = await _wishlistService.AddItemAsync(null, null, prodId, "127.0.0.1", "TestAgent");
            Assert.True(addSuccess);
            Assert.Equal("WISHLIST_ITEM_ADDED", addMsg);
            Assert.NotNull(issuedToken);

            // 2. Verify token is stored ONLY as SHA-256 hash in database
            string tokenHash = WishlistService.HashGuestToken(issuedToken);
            object? rawCheck = await _db.ExecuteScalarAsync(
                "SELECT COUNT(1) FROM drs_wishlist_mst WHERE GuestTokenHash = @Hash AND IsDeleted = 0",
                new[] { new SqlParameter("@Hash", tokenHash) }
            );
            Assert.Equal(1, Convert.ToInt32(rawCheck));

            // Verify raw token itself is NOT stored in plain text anywhere in drs_wishlist_mst
            object? plainCheck = await _db.ExecuteScalarAsync(
                "SELECT COUNT(1) FROM drs_wishlist_mst WHERE GuestTokenHash = @Token",
                new[] { new SqlParameter("@Token", issuedToken) }
            );
            Assert.Equal(0, Convert.ToInt32(plainCheck));

            // 3. Guest retrieves wishlist using the valid issued token
            var (wishlist, _) = await _wishlistService.GetOrCreateWishlistAsync(null, issuedToken);
            Assert.True(wishlist.IsGuest);
            Assert.Equal(1, wishlist.TotalItems);
            Assert.Contains(wishlist.Items, i => _encryption.TryDecrypt(i.EncryptedProductId, out long id) && id == prodId);

            // 4. Guest with tampered/different token cannot see the other guest's items
            string foreignToken = WishlistService.GenerateGuestToken();
            var (foreignWishlist, _) = await _wishlistService.GetOrCreateWishlistAsync(null, foreignToken);
            Assert.Equal(0, foreignWishlist.TotalItems);

            // 5. Remove item
            var (remSuccess, remMsg) = await _wishlistService.RemoveItemAsync(null, issuedToken, prodId, "127.0.0.1", "TestAgent");
            Assert.True(remSuccess);
            Assert.Equal("WISHLIST_ITEM_REMOVED", remMsg);

            // 6. Clear wishlist
            var (clrSuccess, clrMsg) = await _wishlistService.ClearWishlistAsync(null, issuedToken, "127.0.0.1", "TestAgent");
            Assert.True(clrSuccess);
            Assert.Equal("WISHLIST_CLEARED", clrMsg);
        }

        [Fact]
        public async Task WishlistMerge_AtomicTransaction_TransfersAndDeduplicates()
        {
            Assert.NotNull(_wishlistService);
            Assert.NotNull(_db);

            var (custId, prod1) = await SetupTestCustomerAndProductAsync();
            var (_, prod2) = await SetupTestCustomerAndProductAsync();

            // Guest adds prod1 and prod2
            var (_, _, guestToken) = await _wishlistService.AddItemAsync(null, null, prod1, "127.0.0.1", "TestAgent");
            Assert.NotNull(guestToken);
            await _wishlistService.AddItemAsync(null, guestToken, prod2, "127.0.0.1", "TestAgent");

            // Customer already has prod2 in customer wishlist
            await _wishlistService.AddItemAsync(custId, null, prod2, "127.0.0.1", "TestAgent");

            // Merge guest wishlist into customer wishlist
            var (mergeSuccess, mergeMsg, mergedCount) = await _wishlistService.MergeGuestWishlistAsync(custId, guestToken, "127.0.0.1", "TestAgent");
            Assert.True(mergeSuccess);
            Assert.Equal("WISHLIST_MERGED", mergeMsg);
            Assert.Equal(1, mergedCount); // Only prod1 was newly merged; prod2 was already present and deduplicated

            // Verify customer wishlist now contains both prod1 and prod2
            var (custWishlist, _) = await _wishlistService.GetOrCreateWishlistAsync(custId, null);
            Assert.Equal(2, custWishlist.TotalItems);

            // Verify guest wishlist record is soft-deleted
            string guestHash = WishlistService.HashGuestToken(guestToken);
            int guestActiveCount = Convert.ToInt32(await _db.ExecuteScalarAsync(
                "SELECT COUNT(1) FROM drs_wishlist_mst WHERE GuestTokenHash = @Hash AND IsDeleted = 0",
                new[] { new SqlParameter("@Hash", guestHash) }
            ));
            Assert.Equal(0, guestActiveCount);
        }

        [Fact]
        public async Task Cart_CRUD_And_SessionPersistence_AcrossLogout()
        {
            Assert.NotNull(_cartService);

            var (custId, prodId) = await SetupTestCustomerAndProductAsync(availableQty: 10, price: 200m);

            // 1. Add item to cart with quantity 2
            var (addSuccess, addMsg, cartAfterAdd) = await _cartService.AddItemAsync(custId, prodId, 2, "127.0.0.1", "TestAgent");
            Assert.True(addSuccess);
            Assert.NotNull(cartAfterAdd);
            Assert.Equal(2, cartAfterAdd.TotalItems);
            Assert.Equal(400m, cartAfterAdd.SubTotal);

            // 2. Update quantity to 4
            var (updSuccess, updMsg, cartAfterUpd) = await _cartService.UpdateItemQuantityAsync(custId, prodId, 4, "127.0.0.1", "TestAgent");
            Assert.True(updSuccess);
            Assert.NotNull(cartAfterUpd);
            Assert.Equal(4, cartAfterUpd.TotalItems);
            Assert.Equal(800m, cartAfterUpd.SubTotal);

            // 3. Simulate customer logout and subsequent login (fresh CartService instance)
            var freshCartService = new CartService(_db!, _encryption, _audit);
            var reloadedCart = await freshCartService.GetCustomerCartAsync(custId);
            Assert.Equal("Active", reloadedCart.CartStatus);
            Assert.Equal(4, reloadedCart.TotalItems);
            Assert.Equal(800m, reloadedCart.SubTotal);

            // 4. Remove item
            var (remSuccess, remMsg, cartAfterRem) = await _cartService.RemoveItemAsync(custId, prodId, "127.0.0.1", "TestAgent");
            Assert.True(remSuccess);
            Assert.Equal(0, cartAfterRem!.TotalItems);

            // 5. Clear cart
            var (clrSuccess, clrMsg) = await _cartService.ClearCartAsync(custId, "127.0.0.1", "TestAgent");
            Assert.True(clrSuccess);
        }

        [Fact]
        public async Task Cart_CumulativeStockValidation_RejectsWhenQuantityExceedsAvailable()
        {
            Assert.NotNull(_cartService);

            // Product with only 5 items in stock
            var (custId, prodId) = await SetupTestCustomerAndProductAsync(availableQty: 5);

            // Add 3 -> succeeds (3 <= 5)
            var (s1, m1, _) = await _cartService.AddItemAsync(custId, prodId, 3, "127.0.0.1", "TestAgent");
            Assert.True(s1);

            // Add 3 more -> rejected because cumulative 6 > 5
            var (s2, m2, _) = await _cartService.AddItemAsync(custId, prodId, 3, "127.0.0.1", "TestAgent");
            Assert.False(s2);
            Assert.Equal("INSUFFICIENT_STOCK", m2);

            // Update quantity directly to 6 -> rejected (6 > 5)
            var (s3, m3, _) = await _cartService.UpdateItemQuantityAsync(custId, prodId, 6, "127.0.0.1", "TestAgent");
            Assert.False(s3);
            Assert.Equal("INSUFFICIENT_STOCK", m3);

            // Update quantity to 5 -> succeeds (5 <= 5)
            var (s4, m4, cart) = await _cartService.UpdateItemQuantityAsync(custId, prodId, 5, "127.0.0.1", "TestAgent");
            Assert.True(s4);
            Assert.Equal(5, cart!.TotalItems);
        }

        [Fact]
        public async Task WishlistMoveToCart_TransactionRollback_WhenStockInsufficient()
        {
            Assert.NotNull(_wishlistService);
            Assert.NotNull(_cartService);

            // Product with 3 items in stock
            var (custId, prodId) = await SetupTestCustomerAndProductAsync(availableQty: 3);

            // Put item in customer wishlist
            await _wishlistService.AddItemAsync(custId, null, prodId, "127.0.0.1", "TestAgent");

            // Customer already has 2 of this product in cart
            await _cartService.AddItemAsync(custId, prodId, 2, "127.0.0.1", "TestAgent");

            // Move 2 from wishlist to cart -> cumulative 4 > 3 -> must be rejected with INSUFFICIENT_STOCK
            var (failSuccess, failMsg, _) = await _wishlistService.MoveToCartAsync(custId, prodId, 2, "127.0.0.1", "TestAgent");
            Assert.False(failSuccess);
            Assert.Equal("INSUFFICIENT_STOCK", failMsg);

            // Verify transaction rolled back: item is still in wishlist and cart still has 2
            var (wishlistCheck, _) = await _wishlistService.GetOrCreateWishlistAsync(custId, null);
            Assert.Equal(1, wishlistCheck.TotalItems);

            var cartCheck = await _cartService.GetCustomerCartAsync(custId);
            Assert.Equal(2, cartCheck.TotalItems);

            // Now move 1 from wishlist to cart -> cumulative 3 == 3 -> succeeds
            var (okSuccess, okMsg, cartAfterMove) = await _wishlistService.MoveToCartAsync(custId, prodId, 1, "127.0.0.1", "TestAgent");
            Assert.True(okSuccess);
            Assert.Equal("ITEM_MOVED_TO_CART", okMsg);
            Assert.Equal(3, cartAfterMove!.TotalItems);

            // Wishlist is now empty
            var (wishlistAfter, _) = await _wishlistService.GetOrCreateWishlistAsync(custId, null);
            Assert.Equal(0, wishlistAfter.TotalItems);
        }

        [Fact]
        public async Task Cart_ConcurrentCartCreation_And_ParallelItemAdditions()
        {
            Assert.NotNull(_cartService);
            Assert.NotNull(_db);

            var (custId, prodId) = await SetupTestCustomerAndProductAsync(availableQty: 100);

            // Simulate 5 parallel tasks creating active cart and adding items simultaneously
            int threadCount = 5;
            var tasks = new Task<(bool Success, string Message, CartResponse? Cart)>[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Run(() => _cartService.AddItemAsync(custId, prodId, 1, "127.0.0.1", "TestAgent"));
            }

            var results = await Task.WhenAll(tasks);

            // All should succeed
            foreach (var r in results)
            {
                Assert.True(r.Success, $"Parallel cart addition failed with message: {r.Message}");
            }

            // Exactly ONE active cart must exist in drs_cart_mst
            int activeCartCount = Convert.ToInt32(await _db.ExecuteScalarAsync(
                "SELECT COUNT(1) FROM drs_cart_mst WHERE customer_id = @CustomerId AND CartStatus = 'Active' AND IsDeleted = 0",
                new[] { new SqlParameter("@CustomerId", custId) }
            ));
            Assert.Equal(1, activeCartCount);

            // Exactly ONE cart item row must exist in drs_cart_item_mst with cumulative quantity = 5
            int activeItemRowCount = Convert.ToInt32(await _db.ExecuteScalarAsync(
                "SELECT COUNT(1) FROM drs_cart_item_mst ci INNER JOIN drs_cart_mst c ON c.ID = ci.cart_id WHERE c.customer_id = @CustomerId AND ci.product_id = @ProductId AND ci.IsDeleted = 0",
                new[] { new SqlParameter("@CustomerId", custId), new SqlParameter("@ProductId", prodId) }
            ));
            Assert.Equal(1, activeItemRowCount);

            var finalCart = await _cartService.GetCustomerCartAsync(custId);
            Assert.Equal(threadCount, finalCart.TotalItems);
        }

        [Fact]
        public async Task Cart_CrossCustomerIsolation_GuaranteesDataSeparation()
        {
            Assert.NotNull(_cartService);

            var (customerA, prodIdA) = await SetupTestCustomerAndProductAsync();
            var (customerB, prodIdB) = await SetupTestCustomerAndProductAsync();

            // Customer A adds item to cart
            await _cartService.AddItemAsync(customerA, prodIdA, 2, "127.0.0.1", "TestAgent");

            // Customer B views cart -> should be empty
            var cartB = await _cartService.GetCustomerCartAsync(customerB);
            Assert.Equal(0, cartB.TotalItems);
            Assert.Empty(cartB.Items);

            // Customer B attempts to remove customer A's item -> returns CART_NOT_FOUND or CART_ITEM_NOT_FOUND
            var (remSuccess, remMsg, _) = await _cartService.RemoveItemAsync(customerB, prodIdA, "127.0.0.1", "TestAgent");
            Assert.False(remSuccess);
            Assert.Contains(remMsg, new[] { "CART_NOT_FOUND", "CART_ITEM_NOT_FOUND" });

            // Customer A's cart is undisturbed
            var cartA = await _cartService.GetCustomerCartAsync(customerA);
            Assert.Equal(2, cartA.TotalItems);
        }

        [Fact]
        public async Task Cart_InactiveAndUnavailableProduct_HandledGracefully()
        {
            Assert.NotNull(_cartService);
            Assert.NotNull(_db);

            var (custId, prodId) = await SetupTestCustomerAndProductAsync(availableQty: 10);

            // 1. Add item while active
            await _cartService.AddItemAsync(custId, prodId, 1, "127.0.0.1", "TestAgent");

            // 2. Deactivate product in product master (admin sets IsActive = 0)
            _db.ExecuteNonQuery("UPDATE drs_product_mst SET IsActive = 0 WHERE ID = @Id", new[] { new SqlParameter("@Id", prodId) });

            // 3. New additions should be rejected with PRODUCT_INACTIVE
            var (failAdd, msgAdd, _) = await _cartService.AddItemAsync(custId, prodId, 1, "127.0.0.1", "TestAgent");
            Assert.False(failAdd);
            Assert.Equal("PRODUCT_INACTIVE", msgAdd);

            // 4. Cart retrieval shows item as inactive and not in stock
            var cart = await _cartService.GetCustomerCartAsync(custId);
            Assert.Equal(1, cart.TotalItems);
            var item = cart.Items.First();
            Assert.False(item.IsActive);
            Assert.False(item.IsInStock);
        }
        #endregion
    }
}
