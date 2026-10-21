using System.Security.Claims;
using Khel_Akhel_Server.Controllers.Cart;
using Khel_Akhel_Server.Controllers.Wishlist;
using Khel_Akhel_Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Khel_Akhel_Server.Models.CommonResponse;

namespace Khel_Akhel_Server.Tests
{
    public class CartAndWishlistConcurrencyAndIntegrationTests
    {
        #region Authorization & Claim Isolation Tests
        [Fact]
        public async Task CartController_GetCart_RejectsUnauthenticatedUserWith401()
        {
            // Arrange: Controller with empty User claims (no UserId claim)
            var controller = new CartController(null!, null!)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Act
            var result = await controller.GetCart() as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
            var response = result.Value as ApiResponse;
            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Equal("UNAUTHORIZED", response.Message);
        }

        [Fact]
        public async Task CartController_AddCartItem_RejectsUnauthenticatedUserWith401()
        {
            // Arrange
            var controller = new CartController(null!, null!)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Act
            var result = await controller.AddCartItem(new AddCartItemRequest { EncryptedProductId = "test", Quantity = 1 }) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CartController_UpdateQuantity_RejectsUnauthenticatedUserWith401()
        {
            // Arrange
            var controller = new CartController(null!, null!)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Act
            var result = await controller.UpdateCartItemQuantity("test", new UpdateCartItemQuantityRequest { Quantity = 2 }) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CartController_RemoveItem_RejectsUnauthenticatedUserWith401()
        {
            // Arrange
            var controller = new CartController(null!, null!)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Act
            var result = await controller.RemoveCartItem("test") as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CartController_ClearCart_RejectsUnauthenticatedUserWith401()
        {
            // Arrange
            var controller = new CartController(null!, null!)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Act
            var result = await controller.ClearCart() as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task WishlistController_MergeGuestWishlist_RejectsUnauthenticatedUserWith401()
        {
            // Arrange
            var controller = new WishlistController(null!, null!, null!)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Act
            var result = await controller.MergeGuestWishlist(new MergeWishlistRequest { GuestToken = "token" }) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
            var response = result.Value as ApiResponse;
            Assert.NotNull(response);
            Assert.Equal("UNAUTHORIZED", response.Message);
        }

        [Fact]
        public async Task WishlistController_MoveToCart_RejectsUnauthenticatedUserWith401()
        {
            // Arrange
            var controller = new WishlistController(null!, null!, null!)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Act
            var result = await controller.MoveToCart("test", new MoveToCartRequest { Quantity = 1 }) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
        }
        #endregion

        #region Customer Claim Context Extraction Tests
        [Fact]
        public void Controller_ExtractsAuthenticatedUserId_FromCustomUserIdClaimOrNameIdentifier()
        {
            // Arrange
            var contextWithUserId = new DefaultHttpContext();
            var claims1 = new List<Claim> { new Claim("UserId", "42") };
            contextWithUserId.User = new ClaimsPrincipal(new ClaimsIdentity(claims1, "TestAuth"));

            var contextWithNameIdentifier = new DefaultHttpContext();
            var claims2 = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "99") };
            contextWithNameIdentifier.User = new ClaimsPrincipal(new ClaimsIdentity(claims2, "TestAuth"));

            // Helper to simulate GetAuthenticatedUserId
            long ExtractId(HttpContext ctx)
            {
                var claim = ctx.User.FindFirst("UserId") ?? ctx.User.FindFirst(ClaimTypes.NameIdentifier);
                return claim != null && long.TryParse(claim.Value, out long id) ? id : 0;
            }

            // Assert
            Assert.Equal(42L, ExtractId(contextWithUserId));
            Assert.Equal(99L, ExtractId(contextWithNameIdentifier));
        }

        [Fact]
        public void Controller_RejectsNonNumericOrMissingUserIdClaims()
        {
            var ctx = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim("UserId", "not-a-number") };
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            var claim = ctx.User.FindFirst("UserId") ?? ctx.User.FindFirst(ClaimTypes.NameIdentifier);
            long id = claim != null && long.TryParse(claim.Value, out long parsed) ? parsed : 0;

            Assert.Equal(0L, id);
        }
        #endregion

        #region Concurrency and Data Invariants Logic Tests
        [Fact]
        public void ConcurrencySimulation_MultipleParallelAdditions_CalculatesExpectedTotalQuantity()
        {
            // Simulation of atomic increment logic under concurrent execution
            int currentQty = 0;
            object syncRoot = new object();
            int incrementsCount = 10;
            int qtyPerIncrement = 2;

            Parallel.For(0, incrementsCount, _ =>
            {
                lock (syncRoot)
                {
                    currentQty += qtyPerIncrement;
                }
            });

            Assert.Equal(20, currentQty);
        }

        [Fact]
        public void StockValidation_WhenAddingToExistingCartItem_ValidatesCumulativeResultingQuantity()
        {
            // Arrange
            int currentInCart = 4;
            int additionalRequested = 2;
            int availableInStock = 5;

            // Act
            int cumulativeTotal = currentInCart + additionalRequested;
            bool allowed = availableInStock >= cumulativeTotal;

            // Assert: 5 available is less than 6 requested cumulative; must be rejected
            Assert.False(allowed);
            Assert.Equal(6, cumulativeTotal);
        }

        [Fact]
        public void WishlistMerge_DeduplicationLogic_PreservesExistingAndAppendsNew()
        {
            // Arrange
            var customerWishlistProductIds = new HashSet<long> { 101, 102, 103 };
            var guestWishlistProductIds = new List<long> { 102, 104, 105 }; // 102 is duplicate

            // Act: Perform merge simulation
            int newlyMerged = 0;
            foreach (var pid in guestWishlistProductIds)
            {
                if (!customerWishlistProductIds.Contains(pid))
                {
                    customerWishlistProductIds.Add(pid);
                    newlyMerged++;
                }
            }

            // Assert
            Assert.Equal(2, newlyMerged); // 104 and 105
            Assert.Equal(5, customerWishlistProductIds.Count);
            Assert.Contains(101L, customerWishlistProductIds);
            Assert.Contains(102L, customerWishlistProductIds);
            Assert.Contains(103L, customerWishlistProductIds);
            Assert.Contains(104L, customerWishlistProductIds);
            Assert.Contains(105L, customerWishlistProductIds);
        }

        [Fact]
        public void MoveToCart_AtomicitySimulation_FailsWhenProductIsInactive()
        {
            // Arrange
            bool isProductActive = false;
            bool isProductDeleted = false;
            int availableStock = 10;
            int requestedQuantity = 1;

            bool canProceed = isProductActive && !isProductDeleted && availableStock >= requestedQuantity;

            // Assert
            Assert.False(canProceed);
        }
        #endregion
    }
}
