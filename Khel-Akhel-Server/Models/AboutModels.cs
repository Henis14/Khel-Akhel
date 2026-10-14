using System.ComponentModel.DataAnnotations;

namespace Khel_Akhel_Server.Models
{
    #region About Image Models
    public class AboutImageDto
    {
        public string EncryptedImageId { get; set; } = string.Empty;
        public string EncryptedAboutId { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedDate { get; set; }
    }

    public class AboutImageOrderDto
    {
        [Required]
        public string EncryptedImageId { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int DisplayOrder { get; set; }
    }

    public class AboutImageUploadRequest
    {
        [Required]
        public string EncryptedAboutId { get; set; } = string.Empty;

        [Required]
        public List<IFormFile> Images { get; set; } = new();
    }

    public class AboutImageReorderRequest
    {
        [Required]
        public string EncryptedAboutId { get; set; } = string.Empty;

        [Required]
        public List<AboutImageOrderDto> ImageOrders { get; set; } = new();
    }
    #endregion

    #region About Request Models
    public class AboutCreateRequest
    {
        [Required, StringLength(200, MinimumLength = 2)]
        public string Heading { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Subheading { get; set; }

        [StringLength(50)]
        public string? ContentType { get; set; }

        [StringLength(200)]
        public string? ProjectOrEventTitle { get; set; }

        [StringLength(50)]
        public string? DisplayYear { get; set; }

        [StringLength(1000)]
        public string? ShortDescription { get; set; }

        public string? DetailedDescription { get; set; }

        [StringLength(1000)]
        public string? Credits { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? ExternalUrl { get; set; }

        [Range(0, int.MaxValue)]
        public int DisplayOrder { get; set; } = 0;

        [StringLength(50)]
        public string? LayoutType { get; set; } = "grid-3";

        public bool IsPublished { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public List<IFormFile>? Images { get; set; }
    }

    public class AboutUpdateRequest
    {
        [Required]
        public string EncryptedAboutId { get; set; } = string.Empty;

        [Required, StringLength(200, MinimumLength = 2)]
        public string Heading { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Subheading { get; set; }

        [StringLength(50)]
        public string? ContentType { get; set; }

        [StringLength(200)]
        public string? ProjectOrEventTitle { get; set; }

        [StringLength(50)]
        public string? DisplayYear { get; set; }

        [StringLength(1000)]
        public string? ShortDescription { get; set; }

        public string? DetailedDescription { get; set; }

        [StringLength(1000)]
        public string? Credits { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? ExternalUrl { get; set; }

        [Range(0, int.MaxValue)]
        public int DisplayOrder { get; set; } = 0;

        [StringLength(50)]
        public string? LayoutType { get; set; }

        public bool? IsPublished { get; set; }

        public bool? IsActive { get; set; }

        public List<IFormFile>? NewImages { get; set; }

        public List<string>? RemoveImageIds { get; set; }

        public List<AboutImageOrderDto>? ImageOrders { get; set; }
    }

    public class AboutPublishToggleRequest
    {
        [Required]
        public string EncryptedAboutId { get; set; } = string.Empty;

        public bool IsPublished { get; set; }
    }

    public class AboutListRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? ContentType { get; set; }
        public bool? IsPublished { get; set; }
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; }
    }
    #endregion

    #region About Response Models
    public class AboutDetailResponse
    {
        public string EncryptedAboutId { get; set; } = string.Empty;
        public string Heading { get; set; } = string.Empty;
        public string? Subheading { get; set; }
        public string? ContentType { get; set; }
        public string? ProjectOrEventTitle { get; set; }
        public string? DisplayYear { get; set; }
        public string? ShortDescription { get; set; }
        public string? DetailedDescription { get; set; }
        public string? Credits { get; set; }
        public string? Location { get; set; }
        public string? ExternalUrl { get; set; }
        public int DisplayOrder { get; set; }
        public string LayoutType { get; set; } = "grid-3";
        public bool IsPublished { get; set; }
        public bool IsActive { get; set; }
        public int ImageCount { get; set; }
        public List<AboutImageDto> Images { get; set; } = new();
        public string? CreatedDate { get; set; }
        public string? ModifiedDate { get; set; }
    }

    public class AboutPublicResponse
    {
        public string EncryptedAboutId { get; set; } = string.Empty;
        public string Heading { get; set; } = string.Empty;
        public string? Subheading { get; set; }
        public string? ContentType { get; set; }
        public string? ProjectOrEventTitle { get; set; }
        public string? DisplayYear { get; set; }
        public string? ShortDescription { get; set; }
        public string? DetailedDescription { get; set; }
        public string? Credits { get; set; }
        public string? Location { get; set; }
        public string? ExternalUrl { get; set; }
        public int DisplayOrder { get; set; }
        public string LayoutType { get; set; } = "grid-3";
        public List<AboutImageDto> Images { get; set; } = new();
    }

    public class AboutListResponse
    {
        public List<AboutDetailResponse> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages =>
            PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;
    }
    #endregion
}
