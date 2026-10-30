using System.Data;
using System.Globalization;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.BL.Services;
using Khel_Akhel_Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using static Khel_Akhel_Server.Models.CommonResponse;

namespace Khel_Akhel_Server.Controllers.Statistics
{
    [ApiController]
    [Route("api/website-statistics")]
    public class WebsiteStatisticsController : ControllerBase
    {
        private const int MaxCustomRangeDays = 366;
        private const string DateFormat = "yyyy-MM-dd";
        private static readonly TimeSpan SummaryCacheDuration = TimeSpan.FromSeconds(15);

        private static readonly string[] BotUserAgentMarkers =
        {
            "bot",
            "crawl",
            "spider",
            "slurp",
            "headless",
            "lighthouse",
        };

        private readonly DbHelper _db;
        private readonly IMemoryCache _cache;
        private readonly IPageVisitService _pageVisits;

        public WebsiteStatisticsController(
            DbHelper db,
            IMemoryCache cache,
            IPageVisitService pageVisits
        )
        {
            _db = db;
            _cache = cache;
            _pageVisits = pageVisits;
        }

        #region Helper Methods
        private bool IsLikelyBot()
        {
            string userAgent = Request.Headers.UserAgent.ToString();
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return true;
            }

            return BotUserAgentMarkers.Any(marker =>
                userAgent.Contains(marker, StringComparison.OrdinalIgnoreCase)
            );
        }

        private static bool TryParseDate(string? value, out DateTime date)
        {
            return DateTime.TryParseExact(
                value?.Trim(),
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date
            );
        }

        private static IActionResult BadRequestResponse(string message, string error)
        {
            return new BadRequestObjectResult(
                new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = message,
                    Errors = new List<string> { error },
                }
            );
        }

        private static IActionResult AcceptedResponse(string message)
        {
            return new ObjectResult(
                new ApiResponse
                {
                    Success = true,
                    StatusCode = 202,
                    Message = message,
                }
            )
            {
                StatusCode = StatusCodes.Status202Accepted,
            };
        }

        private static SqlParameter DateParameter(string name, DateTime value)
        {
            return new SqlParameter(name, SqlDbType.Date) { Value = value.Date };
        }
        #endregion

        #region Track Page Visit API
        [AllowAnonymous]
        [EnableRateLimiting("page-visit")]
        [RequestSizeLimit(2048)]
        [HttpPost("track")]
        public IActionResult TrackPageVisit([FromBody] PageVisitTrackRequest? request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.PageKey))
                {
                    return BadRequestResponse("VALIDATION_FAILED", "Page key is required.");
                }

                if (IsLikelyBot())
                {
                    return AcceptedResponse("PAGE_VISIT_IGNORED");
                }

                PageVisitTrackResult result = _pageVisits.Track(
                    request.PageKey,
                    request.IsUnique
                );

                if (result == PageVisitTrackResult.UnknownPage)
                {
                    return BadRequestResponse("INVALID_PAGE_KEY", "The page is not tracked.");
                }

                return AcceptedResponse("PAGE_VISIT_TRACKED");
            }
            catch (Exception ex)
            {
                Logs.Error("Page visit track API failed", ex);
                return AcceptedResponse("PAGE_VISIT_IGNORED");
            }
        }
        #endregion

        #region Admin Statistics Summary API
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/summary")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] string? range = "today",
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null
        )
        {
            try
            {
                string normalizedRange = string.IsNullOrWhiteSpace(range)
                    ? "today"
                    : range.Trim().ToLowerInvariant();

                DateTime today = DateTime.Today;
                DateTime start;
                DateTime end;

                switch (normalizedRange)
                {
                    case "today":
                    case "daily":
                        normalizedRange = "today";
                        start = today;
                        end = today;
                        break;
                    case "week":
                    case "weekly":
                        normalizedRange = "week";
                        start = today.AddDays(-6);
                        end = today;
                        break;
                    case "month":
                    case "monthly":
                        normalizedRange = "month";
                        start = today.AddDays(-29);
                        end = today;
                        break;
                    case "custom":
                        if (
                            string.IsNullOrWhiteSpace(startDate)
                            || string.IsNullOrWhiteSpace(endDate)
                        )
                        {
                            Logs.Warning(
                                "Website statistics rejected | Custom range without dates"
                            );
                            return BadRequestResponse(
                                "DATE_RANGE_REQUIRED",
                                "Start date and end date are required for a custom range."
                            );
                        }

                        if (!TryParseDate(startDate, out start) || !TryParseDate(endDate, out end))
                        {
                            Logs.Warning(
                                "Website statistics rejected | Invalid date format"
                            );
                            return BadRequestResponse(
                                "INVALID_DATE",
                                "Dates must be in yyyy-MM-dd format."
                            );
                        }

                        if (start > end)
                        {
                            return BadRequestResponse(
                                "INVALID_DATE_RANGE",
                                "Start date cannot be after end date."
                            );
                        }

                        if (start > today)
                        {
                            return BadRequestResponse(
                                "INVALID_DATE_RANGE",
                                "Start date cannot be in the future."
                            );
                        }

                        if (end > today)
                        {
                            end = today;
                        }

                        if ((end - start).TotalDays + 1 > MaxCustomRangeDays)
                        {
                            return BadRequestResponse(
                                "INVALID_DATE_RANGE",
                                $"A custom range can cover at most {MaxCustomRangeDays} days."
                            );
                        }
                        break;
                    default:
                        Logs.Warning("Website statistics rejected | Invalid range");
                        return BadRequestResponse(
                            "INVALID_RANGE",
                            "Range must be today, week, month or custom."
                        );
                }

                string cacheKey =
                    $"website-statistics:summary:{start:yyyyMMdd}:{end:yyyyMMdd}:{normalizedRange}";
                if (
                    !_cache.TryGetValue(cacheKey, out WebsiteStatisticsSummaryResponse? summary)
                    || summary == null
                )
                {
                    summary = await BuildSummaryAsync(normalizedRange, start, end);
                    _cache.Set(cacheKey, summary, SummaryCacheDuration);
                }

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "WEBSITE_STATISTICS_FETCHED",
                        Data = summary,
                    }
                );
            }
            catch (SqlException ex)
            {
                Logs.Error("Website statistics summary API failed | Database error", ex);
                return StatusCode(
                    503,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 503,
                        Message = "SERVICE_UNAVAILABLE",
                        Errors = new List<string>
                        {
                            "Website statistics are temporarily unavailable. Please try again shortly.",
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                Logs.Error("Website statistics summary API failed", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string>
                        {
                            "An internal server error occurred while retrieving website statistics.",
                        },
                    }
                );
            }
        }

        private async Task<WebsiteStatisticsSummaryResponse> BuildSummaryAsync(
            string normalizedRange,
            DateTime start,
            DateTime end
        )
        {
            int totalDays = (int)(end - start).TotalDays + 1;
            DateTime previousEnd = start.AddDays(-1);
            DateTime previousStart = previousEnd.AddDays(-(totalDays - 1));

            const string pagesSql =
                @"
                SELECT p.PageKey,
                       p.PageName,
                       p.DisplayOrder,
                       ISNULL(SUM(v.VisitCount), 0) AS Visits,
                       ISNULL(SUM(v.UniqueVisitCount), 0) AS UniqueVisits
                FROM drs_website_page_mst AS p WITH (NOLOCK)
                LEFT JOIN drs_page_visit_daily_mst AS v WITH (NOLOCK)
                       ON v.page_id = p.ID
                      AND v.IsDeleted = 0
                      AND v.VisitDate BETWEEN @StartDate AND @EndDate
                WHERE p.IsActive = 1
                  AND p.IsDeleted = 0
                GROUP BY p.PageKey, p.PageName, p.DisplayOrder;";

            DataTable pagesTable = await _db.ExecuteQueryAsync(
                pagesSql,
                new[] { DateParameter("@StartDate", start), DateParameter("@EndDate", end) }
            );

            const string dailySql =
                @"
                SELECT v.VisitDate,
                       SUM(v.VisitCount) AS Visits,
                       SUM(v.UniqueVisitCount) AS UniqueVisits
                FROM drs_page_visit_daily_mst AS v WITH (NOLOCK)
                INNER JOIN drs_website_page_mst AS p WITH (NOLOCK)
                        ON p.ID = v.page_id
                       AND p.IsActive = 1
                       AND p.IsDeleted = 0
                WHERE v.IsDeleted = 0
                  AND v.VisitDate BETWEEN @StartDate AND @EndDate
                GROUP BY v.VisitDate;";

            DataTable dailyTable = await _db.ExecuteQueryAsync(
                dailySql,
                new[] { DateParameter("@StartDate", start), DateParameter("@EndDate", end) }
            );

            const string previousSql =
                @"
                SELECT ISNULL(SUM(v.VisitCount), 0)
                FROM drs_page_visit_daily_mst AS v WITH (NOLOCK)
                INNER JOIN drs_website_page_mst AS p WITH (NOLOCK)
                        ON p.ID = v.page_id
                       AND p.IsActive = 1
                       AND p.IsDeleted = 0
                WHERE v.IsDeleted = 0
                  AND v.VisitDate BETWEEN @StartDate AND @EndDate;";

            object? previousResult = await _db.ExecuteScalarAsync(
                previousSql,
                new[]
                {
                    DateParameter("@StartDate", previousStart),
                    DateParameter("@EndDate", previousEnd),
                }
            );

            var summary = new WebsiteStatisticsSummaryResponse
            {
                Range = normalizedRange,
                StartDate = start.ToString(DateFormat, CultureInfo.InvariantCulture),
                EndDate = end.ToString(DateFormat, CultureInfo.InvariantCulture),
                TotalDays = totalDays,
                PreviousTotalVisits =
                    previousResult == null || previousResult == DBNull.Value
                        ? 0
                        : Convert.ToInt32(previousResult),
            };

            var pageOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in pagesTable.Rows)
            {
                string pageKey = row["PageKey"]?.ToString() ?? string.Empty;
                pageOrder[pageKey] =
                    row["DisplayOrder"] == DBNull.Value ? 0 : Convert.ToInt32(row["DisplayOrder"]);
                summary.Pages.Add(
                    new PageVisitStatItem
                    {
                        PageKey = pageKey,
                        PageName = row["PageName"]?.ToString() ?? string.Empty,
                        Visits = Convert.ToInt32(row["Visits"]),
                        UniqueVisits = Convert.ToInt32(row["UniqueVisits"]),
                    }
                );
            }

            var visitsByDate = new Dictionary<DateTime, (int Visits, int UniqueVisits)>();
            foreach (DataRow row in dailyTable.Rows)
            {
                if (row["VisitDate"] == DBNull.Value)
                {
                    continue;
                }

                visitsByDate[Convert.ToDateTime(row["VisitDate"]).Date] = (
                    Convert.ToInt32(row["Visits"]),
                    Convert.ToInt32(row["UniqueVisits"])
                );
            }

            foreach (PendingPageVisit pending in _pageVisits.GetPending(start, end))
            {
                PageVisitStatItem? page = summary.Pages.FirstOrDefault(p =>
                    string.Equals(p.PageKey, pending.PageKey, StringComparison.OrdinalIgnoreCase)
                );
                if (page == null)
                {
                    continue;
                }

                page.Visits += pending.Visits;
                page.UniqueVisits += pending.UniqueVisits;

                visitsByDate.TryGetValue(pending.VisitDate.Date, out var dayCounts);
                visitsByDate[pending.VisitDate.Date] = (
                    dayCounts.Visits + pending.Visits,
                    dayCounts.UniqueVisits + pending.UniqueVisits
                );
            }

            summary.Pages = summary
                .Pages.OrderByDescending(p => p.Visits)
                .ThenBy(p => pageOrder.TryGetValue(p.PageKey, out int order) ? order : 0)
                .ToList();

            summary.TotalVisits = summary.Pages.Sum(p => p.Visits);
            summary.TotalUniqueVisits = summary.Pages.Sum(p => p.UniqueVisits);
            summary.AverageDailyVisits = Math.Round(
                (decimal)summary.TotalVisits / totalDays,
                1,
                MidpointRounding.AwayFromZero
            );

            foreach (var page in summary.Pages)
            {
                page.SharePercent =
                    summary.TotalVisits == 0
                        ? 0
                        : Math.Round(
                            page.Visits * 100m / summary.TotalVisits,
                            1,
                            MidpointRounding.AwayFromZero
                        );
            }

            for (DateTime day = start; day <= end; day = day.AddDays(1))
            {
                visitsByDate.TryGetValue(day, out var counts);
                summary.Daily.Add(
                    new DailyVisitStatItem
                    {
                        Date = day.ToString(DateFormat, CultureInfo.InvariantCulture),
                        Visits = counts.Visits,
                        UniqueVisits = counts.UniqueVisits,
                    }
                );
            }

            return summary;
        }
        #endregion
    }
}
