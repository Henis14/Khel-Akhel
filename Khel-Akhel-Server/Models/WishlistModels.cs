using System.ComponentModel.DataAnnotations;

namespace Khel_Akhel_Server.Models
{
    #region Wishlist Requests
    public class AddWishlistItemRequest
    {
        [Required(ErrorMessage = "Encrypted product ID is required.")]
        public string EncryptedProductId { get; set; } = string.Empty;
    }

    public class MergeWishlistRequest
    {
        /// <summary>
        /// Optional explicit guest token fallback if cookie is not directly accessible (e.g., cross-origin testing).
        /// When present, backend validates and hashes it using SHA-256 before lookup.
        /// </summary>
        public string? GuestToken { get; set; }
    }

    public class MoveToCartRequest
    {
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
        public int Quantity { get; set; } = 1;
    }
    #endregion

    #region Wishlist Responses
    public class WishlistItemResponse
    {
        public string EncryptedWishlistItemId { get; set; } = string.Empty;
        public string EncryptedProductId { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public decimal MRP { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public string? PrimaryImagePath { get; set; }
        public int AvailableQty { get; set; }
        public bool IsInStock { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime AddedDate { get; set; }
    }

    public class WishlistResponse
    {
        public string EncryptedWishlistId { get; set; } = string.Empty;
        public bool IsGuest { get; set; }
        public int TotalItems { get; set; }
        public List<WishlistItemResponse> Items { get; set; } = new List<WishlistItemResponse>();
    }
    #endregion
}
