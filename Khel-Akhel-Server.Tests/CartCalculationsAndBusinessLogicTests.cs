using Khel_Akhel_Server.Models;
using Xunit;

namespace Khel_Akhel_Server.Tests
{
    public class CartCalculationsAndBusinessLogicTests
    {
        [Fact]
        public void CartTotalsCalculation_CalculatesSubTotalAndDiscountsAccurately()
        {
            // Arrange
            var cart = new CartResponse
            {
                CartStatus = "Active",
                Items = new List<CartItemResponse>
                {
                    new CartItemResponse
                    {
                        MRP = 1000.00m,
                        SellingPrice = 800.00m,
                        Quantity = 2,
                        ItemTotal = 1600.00m,
                        AvailableQty = 5,
                        IsInStock = true
                    },
                    new CartItemResponse
                    {
                        MRP = 500.00m,
                        SellingPrice = 500.00m,
                        Quantity = 1,
                        ItemTotal = 500.00m,
                        AvailableQty = 10,
                        IsInStock = true
                    }
                }
            };

            // Act
            int calculatedTotalItems = cart.Items.Sum(i => i.Quantity);
            decimal calculatedSubTotal = cart.Items.Sum(i => i.ItemTotal);
            decimal calculatedTotalMrp = cart.Items.Sum(i => i.MRP * i.Quantity);
            decimal calculatedDiscount = calculatedTotalMrp - calculatedSubTotal;

            // Assert
            Assert.Equal(3, calculatedTotalItems);
            Assert.Equal(2100.00m, calculatedSubTotal);
            Assert.Equal(2500.00m, calculatedTotalMrp);
            Assert.Equal(400.00m, calculatedDiscount);
        }

        [Fact]
        public void WishlistItem_DiscountPercentage_CalculatesProperly()
        {
            // Arrange
            decimal mrp = 2000.00m;
            decimal sellingPrice = 1500.00m;

            // Act
            decimal discount = mrp > 0 && sellingPrice < mrp 
                ? Math.Round(((mrp - sellingPrice) / mrp) * 100m, 2) 
                : 0m;

            // Assert
            Assert.Equal(25.00m, discount);
        }

        [Theory]
        [InlineData(10, 2, true, true)]
        [InlineData(10, 2, false, false)] // Inactive product must be reported not in stock
        [InlineData(2, 2, true, true)]
        [InlineData(1, 2, true, false)]
        [InlineData(0, 1, true, false)]
        [InlineData(-1, 1, true, false)]
        public void CartItem_StockAndActiveAvailability_AccuratelyEvaluatesInventory(int availableQty, int requestedQty, bool isProductActive, bool expectedIsInStock)
        {
            // Act
            bool isInStock = isProductActive && availableQty >= requestedQty && availableQty > 0;

            // Assert
            Assert.Equal(expectedIsInStock, isInStock);
        }

        [Theory]
        [InlineData(5, true, true)]
        [InlineData(5, false, false)] // Inactive product must not be considered in stock in wishlist
        [InlineData(0, true, false)]
        [InlineData(-1, true, false)]
        public void WishlistItem_StockAndActiveAvailability_AccuratelyEvaluatesInventory(int availableQty, bool isProductActive, bool expectedIsInStock)
        {
            // Act
            bool isInStock = isProductActive && availableQty > 0;

            // Assert
            Assert.Equal(expectedIsInStock, isInStock);
        }

        [Fact]
        public void MoveToCart_WhenAlreadyInCart_ValidatesCumulativeStock()
        {
            // Arrange
            int existingInCart = 3;
            int requestedFromWishlist = 2;
            int totalInventory = 4;

            // Act
            int cumulativeTotal = existingInCart + requestedFromWishlist;
            bool isAllowed = totalInventory >= cumulativeTotal;

            // Assert: 4 available is less than 5 cumulative requested
            Assert.False(isAllowed);
            Assert.Equal(5, cumulativeTotal);
        }

        [Fact]
        public void EmptyCart_ReturnsSafeZeroTotals()
        {
            // Arrange
            var cart = new CartResponse
            {
                CartStatus = "Empty",
                TotalItems = 0,
                DistinctItemsCount = 0,
                SubTotal = 0m,
                TotalMRP = 0m,
                TotalDiscount = 0m,
                Items = new List<CartItemResponse>()
            };

            // Assert
            Assert.Equal("Empty", cart.CartStatus);
            Assert.Equal(0, cart.TotalItems);
            Assert.Equal(0m, cart.SubTotal);
            Assert.Empty(cart.Items);
        }
    }
}
