using System.ComponentModel.DataAnnotations;

namespace Khel_Akhel_Server.Models
{
    #region Project Image Models
    public class ProjectImageDto
    {
        public string EncryptedImageId { get; set; } = string.Empty;
        public string EncryptedProjectId { get; set; } = string.Empty;
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

    public class ProjectImageOrderDto
    {
        [Required]
        public string EncryptedImageId { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int DisplayOrder { get; set; }
    }

    public class ProjectImageUploadRequest
    {
        [Required]
        public string EncryptedProjectId { get; set; } = string.Empty;

        [Required]
        public List<IFormFile> Images { get; set; } = new();
    }

    public class ProjectImageReorderRequest
    {
        [Required]
        public string EncryptedProjectId { get; set; } = string.Empty;

        [Required]
        public List<ProjectImageOrderDto> ImageOrders { get; set; } = new();
    }
    #endregion

    #region Project Request Models
    public class ProjectCreateRequest
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

    public class ProjectUpdateRequest
    {
        [Required]
        public string EncryptedProjectId { get; set; } = string.Empty;

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

        public List<ProjectImageOrderDto>? ImageOrders { get; set; }
    }

    public class ProjectPublishToggleRequest
    {
        [Required]
        public string EncryptedProjectId { get; set; } = string.Empty;

        public bool IsPublished { get; set; }
    }

    public class ProjectListRequest
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

    #region Project Response Models
    public class ProjectDetailResponse
    {
        public string EncryptedProjectId { get; set; } = string.Empty;
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
        public List<ProjectImageDto> Images { get; set; } = new();
        public string? CreatedDate { get; set; }
        public string? ModifiedDate { get; set; }
    }

    public class ProjectPublicResponse
    {
        public string EncryptedProjectId { get; set; } = string.Empty;
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
        public List<ProjectImageDto> Images { get; set; } = new();
    }

    public class ProjectListResponse
    {
        public List<ProjectDetailResponse> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages =>
            PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;
    }
    #endregion
}
