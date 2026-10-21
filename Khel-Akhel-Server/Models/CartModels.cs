using System.ComponentModel.DataAnnotations;

namespace Khel_Akhel_Server.Models
{
    #region Cart Requests
    public class AddCartItemRequest
    {
        [Required(ErrorMessage = "Encrypted product ID is required.")]
        public string EncryptedProductId { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemQuantityRequest
    {
        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
        public int Quantity { get; set; }
    }
    #endregion

    #region Cart Responses
    public class CartItemResponse
    {
        public string EncryptedCartItemId { get; set; } = string.Empty;
        public string EncryptedProductId { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public decimal MRP { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal ItemTotal { get; set; }
        public int Quantity { get; set; }
        public string? PrimaryImagePath { get; set; }
        public int AvailableQty { get; set; }
        public bool IsInStock { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime AddedDate { get; set; }
    }

    public class CartResponse
    {
        public string EncryptedCartId { get; set; } = string.Empty;
        public string CartStatus { get; set; } = "Active";
        public int TotalItems { get; set; }
        public int DistinctItemsCount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalMRP { get; set; }
        public decimal TotalDiscount { get; set; }
        public List<CartItemResponse> Items { get; set; } = new List<CartItemResponse>();
    }
    #endregion
}
