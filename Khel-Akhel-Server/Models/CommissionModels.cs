using Microsoft.AspNetCore.Http;

namespace Khel_Akhel_Server.Models
{
    public class CommissionCategoryResponse
    {
        public long Id { get; set; }
        public string EncryptedCategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CommissionRequestCreateDto
    {
        public string FullName { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long CategoryId { get; set; }
        public string? EncryptedCategoryId { get; set; }
        public string? OtherCategoryDescription { get; set; }
        public string ExpectedTimeline { get; set; } = string.Empty;
        public string ProjectLocation { get; set; } = string.Empty;
        public string ProjectDescription { get; set; } = string.Empty;
        public string? Keywords { get; set; }
        public List<IFormFile>? Images { get; set; }
        public string? CaptchaId { get; set; }
        public string? Captcha { get; set; }
        public string? Website { get; set; }
    }

    public class CommissionImageDto
    {
        public string EncryptedImageId { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class CommissionRequestDetailResponse
    {
        public string EncryptedRequestId { get; set; } = string.Empty;
        public string RequestNo { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long CategoryId { get; set; }
        public string EncryptedCategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? OtherCategoryDescription { get; set; }
        public string ExpectedTimeline { get; set; } = string.Empty;
        public string ProjectLocation { get; set; } = string.Empty;
        public string ProjectDescription { get; set; } = string.Empty;
        public int WordCount { get; set; }
        public string? Keywords { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AdminNotes { get; set; }
        public List<CommissionImageDto> Images { get; set; } = new();
        public string CreatedDate { get; set; } = string.Empty;
        public string? ModifiedDate { get; set; }
    }

    public class CommissionRequestListItem
    {
        public string EncryptedRequestId { get; set; } = string.Empty;
        public string RequestNo { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long CategoryId { get; set; }
        public string EncryptedCategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? OtherCategoryDescription { get; set; }
        public string ExpectedTimeline { get; set; } = string.Empty;
        public string ProjectLocation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ImageCount { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string CreatedDate { get; set; } = string.Empty;
    }

    public class CommissionListFilterRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public long? CategoryId { get; set; }
        public string? EncryptedCategoryId { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SortBy { get; set; }
    }

    public class CommissionListResponse
    {
        public List<CommissionRequestListItem> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;
    }

    public class CommissionStatusUpdateRequest
    {
        public string EncryptedRequestId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AdminNotes { get; set; }
    }

    public class CommissionCreateResponse
    {
        public string EncryptedRequestId { get; set; } = string.Empty;
        public string RequestNo { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ExpectedTimeline { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int UploadedImagesCount { get; set; }
        public string CreatedDate { get; set; } = string.Empty;
    }
}
