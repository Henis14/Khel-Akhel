using System.ComponentModel.DataAnnotations;

namespace Khel_Akhel_Server.Models
{
    #region Website Statistics Requests

    public class PageVisitTrackRequest
    {
        private string _pageKey = string.Empty;

        [Required(ErrorMessage = "Page key is required.")]
        [StringLength(
            50,
            MinimumLength = 1,
            ErrorMessage = "Page key must be between 1 and 50 characters."
        )]
        public string PageKey
        {
            get => _pageKey;
            set => _pageKey = value?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        public bool IsUnique { get; set; }
    }
    #endregion

    #region Website Statistics Responses
    public class WebsitePageItem
    {
        public long PageId { get; set; }
        public string PageKey { get; set; } = string.Empty;
        public string PageName { get; set; } = string.Empty;
    }

    public class PageVisitStatItem
    {
        public string PageKey { get; set; } = string.Empty;
        public string PageName { get; set; } = string.Empty;
        public int Visits { get; set; }
        public int UniqueVisits { get; set; }

        public decimal SharePercent { get; set; }
    }

    public class DailyVisitStatItem
    {
        public string Date { get; set; } = string.Empty;
        public int Visits { get; set; }
        public int UniqueVisits { get; set; }
    }

    public class WebsiteStatisticsSummaryResponse
    {
        public string Range { get; set; } = string.Empty;

        public string StartDate { get; set; } = string.Empty;

        public string EndDate { get; set; } = string.Empty;
        public int TotalDays { get; set; }
        public int TotalVisits { get; set; }
        public int TotalUniqueVisits { get; set; }
        public decimal AverageDailyVisits { get; set; }

        public int PreviousTotalVisits { get; set; }
        public List<PageVisitStatItem> Pages { get; set; } = new();
        public List<DailyVisitStatItem> Daily { get; set; } = new();
    }
    #endregion
}
