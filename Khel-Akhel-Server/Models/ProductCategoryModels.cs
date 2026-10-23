namespace Khel_Akhel_Server.Models
{
    #region Category Models
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
    #endregion

    #region SubCategory Models
    public class ProductSubCategoryCreateRequest
    {
        public string EncryptedCategoryId { get; set; } = string.Empty;
        public string SubCategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    public class ProductSubCategoryUpdateRequest
    {
        public string EncryptedSubCategoryId { get; set; } = string.Empty;
        public string? EncryptedCategoryId { get; set; }
        public string? SubCategoryName { get; set; }
        public string? Description { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ProductSubCategoryResponse
    {
        public string EncryptedSubCategoryId { get; set; } = string.Empty;
        public string EncryptedCategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SubCategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public string CreatedDate { get; set; } = string.Empty;
    }
    #endregion
}
