namespace Khel_Akhel_Server.Models
{
    public class ProductCategoryCreateRequest
    {
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    public class ProductCategoryUpdateRequest
    {
        public string EncryptedCategoryId { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? Description { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ProductCategoryResponse
    {
        public string EncryptedCategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public string CreatedDate { get; set; } = string.Empty;
    }
}
