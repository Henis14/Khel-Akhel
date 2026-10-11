using System.ComponentModel.DataAnnotations;

namespace Khel_Akhel_Server.Models;

public class FaqCreateRequest
{
    [StringLength(100)]
    public string? Category { get; set; }

    [Required, StringLength(500, MinimumLength = 3)]
    public string Question { get; set; } = string.Empty;

    [Required]
    public string Answer { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; } = true;
    public bool DisplayOnHomePage { get; set; }
    public bool DisplayOnFaqPage { get; set; } = true;
    public bool DisplayOnCheckoutPage { get; set; }
    public bool DisplayOnContactPage { get; set; }
    public bool DisplayOnFooter { get; set; }
}

public class FaqUpdateRequest : FaqCreateRequest
{
    [Required]
    public string EncryptedFaqId { get; set; } = string.Empty;
}

public class FaqResponse
{
    public string EncryptedFaqId { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }
    public bool DisplayOnHomePage { get; set; }
    public bool DisplayOnFaqPage { get; set; }
    public bool DisplayOnCheckoutPage { get; set; }
    public bool DisplayOnContactPage { get; set; }
    public bool DisplayOnFooter { get; set; }
    public string? CreatedDate { get; set; }
    public string? ModifiedDate { get; set; }
}
