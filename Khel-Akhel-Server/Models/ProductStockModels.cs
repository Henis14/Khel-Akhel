namespace Khel_Akhel_Server.Models
{
    public class ProductStockAddRequest
    {
        public string EncryptedProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class ProductStockUpdateRequest
    {
        public string EncryptedProductId { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
        public int ReservedQuantity { get; set; } = 0;
    }

    public class ProductStockResponse
    {
        public string EncryptedStockId { get; set; } = string.Empty;
        public string EncryptedProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int AvailableQty { get; set; }
        public int ReservedQty { get; set; }
        public bool IsActive { get; set; }
        public string CreatedDate { get; set; } = string.Empty;
    }

    public class ProductStockListRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public bool? LowStockOnly { get; set; }
    }

    public class ProductStockListResponse
    {
        public List<ProductStockResponse> Stocks { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }
}
