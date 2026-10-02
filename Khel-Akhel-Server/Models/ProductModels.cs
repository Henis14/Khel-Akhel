namespace Khel_Akhel_Server.Models
{
    public class ProductCreateRequest
    {
        public string EncryptedCategoryId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ProductCode { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public decimal MRP { get; set; }
        public decimal SellingPrice { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsNewArrival { get; set; }
        public bool IsBestSeller { get; set; }
        public bool IsTrending { get; set; }
        public bool IsCustomerFavourite { get; set; }
    }

    public class ProductUpdateRequest
    {
        public string EncryptedProductId { get; set; } = string.Empty;
        public string? EncryptedCategoryId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public string? SKU { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public decimal? MRP { get; set; }
        public decimal? SellingPrice { get; set; }
        public bool? IsFeatured { get; set; }
        public bool? IsNewArrival { get; set; }
        public bool? IsBestSeller { get; set; }
        public bool? IsTrending { get; set; }
        public bool? IsCustomerFavourite { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ProductResponse
    {
        public string EncryptedProductId { get; set; } = string.Empty;
        public string EncryptedCategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MRP { get; set; }
        public decimal SellingPrice { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsNewArrival { get; set; }
        public bool IsBestSeller { get; set; }
        public bool IsTrending { get; set; }
        public bool IsCustomerFavourite { get; set; }
        public bool IsActive { get; set; }
        public int AvailableQty { get; set; }
        public List<string> ImagePaths { get; set; } = new();
        public string CreatedDate { get; set; } = string.Empty;
    }

    public class ProductListRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? EncryptedCategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? IsFeatured { get; set; }
        public bool? IsNewArrival { get; set; }
        public bool? IsBestSeller { get; set; }
        public bool? IsTrending { get; set; }
        public bool? IsCustomerFavourite { get; set; }
        public bool? IsActive { get; set; }
        public string? Search { get; set; }
        public string? SortBy { get; set; }
    }

    public class ProductListResponse
    {
        public List<ProductResponse> Products { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }
}
