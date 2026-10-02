namespace Khel_Akhel_Server.Models
{
    public class ProductImageUploadRequest
    {
        public string EncryptedProductId { get; set; } = string.Empty;
        public List<IFormFile> Images { get; set; } = new();
    }

    public class ProductImageResponse
    {
        public string EncryptedImageId { get; set; } = string.Empty;
        public string EncryptedProductId { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public string CreatedDate { get; set; } = string.Empty;
    }

    public class ProductImageUploadResponse
    {
        public string EncryptedProductId { get; set; } = string.Empty;
        public List<ProductImageResponse> Images { get; set; } = new();
    }

    public class SetDefaultImageRequest
    {
        public string EncryptedImageId { get; set; } = string.Empty;
    }
}
