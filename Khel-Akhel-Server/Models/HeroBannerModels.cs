using Microsoft.AspNetCore.Http;

namespace Khel_Akhel_Server.Models
{
    public class HeroBannerItemResponse
    {
        public long Id { get; set; }
        public string EncryptedId { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string MediaUrl { get; set; } = string.Empty;
        public bool IsImage { get; set; }
        public bool IsVideo { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? UpdatedAt { get; set; }
    }

    public class HeroBannerCreateRequest
    {
        public string? Title { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool ReplaceExisting { get; set; } = false;

        public List<IFormFile>? Images { get; set; }

        public IFormFile? Video { get; set; }

        public bool? IsImage { get; set; }

        public bool? IsVideo { get; set; }
    }

    public class HeroBannerUpdateRequest
    {
        public string? Title { get; set; }

        public int? DisplayOrder { get; set; }

        public bool? IsActive { get; set; }

        public IFormFile? MediaFile { get; set; }
    }

    public class HeroBannerStatusUpdateRequest
    {
        public bool IsActive { get; set; }
    }
}
