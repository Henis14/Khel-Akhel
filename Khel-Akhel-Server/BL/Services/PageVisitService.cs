using System.Collections.Concurrent;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Khel_Akhel_Server.BL.Common;
using Microsoft.Data.SqlClient;

namespace Khel_Akhel_Server.BL.Services
{
    public enum PageVisitTrackResult
    {
        Accepted,
        UnknownPage,
        Throttled,
    }

    public class PendingPageVisit
    {
        public string PageKey { get; set; } = string.Empty;
        public DateTime VisitDate { get; set; }
        public int Visits { get; set; }
        public int UniqueVisits { get; set; }
    }

    public interface IPageVisitService
    {
        PageVisitTrackResult Track(string pageKey, bool isUnique);

        IReadOnlyList<PendingPageVisit> GetPending(DateTime startDate, DateTime endDate);

        Task FlushAsync(CancellationToken cancellationToken);

        Task RefreshPagesAsync(CancellationToken cancellationToken);
    }

    public sealed class PageVisitService : IPageVisitService
    {
        private const int MaxBufferedEntries = 500;
        private const int MaxBatchRows = 400;
        private const int MaxFlushAttempts = 3;
        private const int CommandTimeoutSeconds = 15;
        private static readonly Regex PageKeyPattern = new(
            "^[a-z0-9][a-z0-9-]{0,49}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        private sealed class Counter
        {
            public int Visits;
            public int UniqueVisits;
        }

        private readonly IConfiguration _config;
        private readonly ConcurrentDictionary<(string PageKey, DateTime Date), Counter> _buffer =
            new();
        private readonly SemaphoreSlim _flushLock = new(1, 1);

        private volatile IReadOnlyDictionary<string, long>? _pages;
        private DateTime _lastErrorLogUtc = DateTime.MinValue;
        private int _droppedSinceLastLog;

        public PageVisitService(IConfiguration config)
        {
            _config = config;
        }

        #region Track
        public PageVisitTrackResult Track(string pageKey, bool isUnique)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pageKey) || !PageKeyPattern.IsMatch(pageKey))
                {
                    return PageVisitTrackResult.UnknownPage;
                }

                var pages = _pages;
                if (pages != null && !pages.ContainsKey(pageKey))
                {
                    return PageVisitTrackResult.UnknownPage;
                }

                var key = (pageKey, DateTime.Today);
                if (!_buffer.TryGetValue(key, out Counter? counter))
                {
                    if (_buffer.Count >= MaxBufferedEntries)
                    {
                        Interlocked.Increment(ref _droppedSinceLastLog);
                        return PageVisitTrackResult.Throttled;
                    }
                    counter = _buffer.GetOrAdd(key, _ => new Counter());
                }

                // Increment Visits before UniqueVisits; FlushAsync reads them in reverse order.
                Interlocked.Increment(ref counter.Visits);
                if (isUnique)
                {
                    Interlocked.Increment(ref counter.UniqueVisits);
                }

                return PageVisitTrackResult.Accepted;
            }
            catch (Exception ex)
            {
                LogThrottled("Page visit buffering failed", ex);
                return PageVisitTrackResult.Throttled;
            }
        }

        public IReadOnlyList<PendingPageVisit> GetPending(DateTime startDate, DateTime endDate)
        {
            var result = new List<PendingPageVisit>();
            foreach (var entry in _buffer)
            {
                if (entry.Key.Date < startDate.Date || entry.Key.Date > endDate.Date)
                {
                    continue;
                }

                int unique = Volatile.Read(ref entry.Value.UniqueVisits);
                int visits = Volatile.Read(ref entry.Value.Visits);
                if (visits <= 0)
                {
                    continue;
                }

                result.Add(
                    new PendingPageVisit
                    {
                        PageKey = entry.Key.PageKey,
                        VisitDate = entry.Key.Date,
                        Visits = visits,
                        UniqueVisits = Math.Min(unique, visits),
                    }
                );
            }
            return result;
        }
        #endregion

        #region Page list
        public async Task RefreshPagesAsync(CancellationToken cancellationToken)
        {
            const string sql =
                @"
                SELECT ID, PageKey
                FROM drs_website_page_mst WITH (NOLOCK)
                WHERE IsActive = 1 AND IsDeleted = 0;";

            try
            {
                var db = new DbHelper(_config);
                await using SqlConnection con = await db.GetOpenConnectionAsync();
                await using var cmd = new SqlCommand(sql, con)
                {
                    CommandTimeout = CommandTimeoutSeconds,
                };
                await using SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken);

                var pages = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                while (await reader.ReadAsync(cancellationToken))
                {
                    string pageKey = reader.GetString(1).Trim().ToLowerInvariant();
                    if (!string.IsNullOrEmpty(pageKey))
                    {
                        pages[pageKey] = reader.GetInt64(0);
                    }
                }

                _pages = pages;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogThrottled("Website page list refresh failed", ex);
            }
        }
        #endregion

        #region Flush
        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (!await _flushLock.WaitAsync(0, cancellationToken))
            {
                return;
            }

            var batch = new List<(string PageKey, DateTime Date, int Visits, int Unique)>();
            try
            {
                LogDroppedVisits();

                foreach (var entry in _buffer)
                {
                    int unique = Interlocked.Exchange(ref entry.Value.UniqueVisits, 0);
                    int visits = Interlocked.Exchange(ref entry.Value.Visits, 0);
                    if (visits > 0)
                    {
                        batch.Add(
                            (entry.Key.PageKey, entry.Key.Date, visits, Math.Min(unique, visits))
                        );
                    }
                    else if (entry.Key.Date < DateTime.Today)
                    {
                        _buffer.TryRemove(entry.Key, out _);
                    }
                }

                if (batch.Count == 0)
                {
                    return;
                }

                if (_pages == null)
                {
                    await RefreshPagesAsync(cancellationToken);
                }

                IReadOnlyDictionary<string, long>? loadedPages = _pages;
                if (loadedPages == null)
                {
                    Restore(batch);
                    return;
                }
                IReadOnlyDictionary<string, long> pages = loadedPages;

                batch.RemoveAll(item => !pages.ContainsKey(item.PageKey));

                while (batch.Count > 0)
                {
                    var chunk = batch.Take(MaxBatchRows).ToList();
                    await WriteWithRetryAsync(
                        chunk
                            .Select(item =>
                                (pages[item.PageKey], item.Date, item.Visits, item.Unique)
                            )
                            .ToList(),
                        cancellationToken
                    );
                    batch.RemoveRange(0, chunk.Count);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Restore(batch);
                throw;
            }
            catch (Exception ex)
            {
                Restore(batch);
                LogThrottled("Page visit flush failed; counts kept for the next attempt", ex);
            }
            finally
            {
                _flushLock.Release();
            }
        }

        private async Task WriteWithRetryAsync(
            List<(long PageId, DateTime Date, int Visits, int Unique)> rows,
            CancellationToken cancellationToken
        )
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    await WriteBatchAsync(rows, cancellationToken);
                    return;
                }
                catch (SqlException ex) when (attempt < MaxFlushAttempts && IsTransient(ex))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
                }
            }
        }

        private async Task WriteBatchAsync(
            List<(long PageId, DateTime Date, int Visits, int Unique)> rows,
            CancellationToken cancellationToken
        )
        {
            var values = new StringBuilder();
            var parameters = new List<SqlParameter>(rows.Count * 4);
            for (int i = 0; i < rows.Count; i++)
            {
                if (i > 0)
                {
                    values.Append(',');
                }
                values.Append($"(@P{i}, @D{i}, @V{i}, @U{i})");
                parameters.Add(
                    new SqlParameter($"@P{i}", SqlDbType.BigInt) { Value = rows[i].PageId }
                );
                parameters.Add(
                    new SqlParameter($"@D{i}", SqlDbType.Date) { Value = rows[i].Date.Date }
                );
                parameters.Add(
                    new SqlParameter($"@V{i}", SqlDbType.Int) { Value = rows[i].Visits }
                );
                parameters.Add(
                    new SqlParameter($"@U{i}", SqlDbType.Int) { Value = rows[i].Unique }
                );
            }

            string sql =
                $@"
                SET XACT_ABORT ON;

                DECLARE @Batch TABLE
                (
                    page_id   BIGINT NOT NULL,
                    VisitDate DATE   NOT NULL,
                    Visits    INT    NOT NULL,
                    Uniques   INT    NOT NULL
                );

                INSERT INTO @Batch (page_id, VisitDate, Visits, Uniques)
                VALUES {values};

                BEGIN TRANSACTION;

                UPDATE t
                SET t.VisitCount = t.VisitCount + b.Visits,
                    t.UniqueVisitCount = t.UniqueVisitCount + b.Uniques,
                    t.ModifiedDate = GETDATE()
                FROM drs_page_visit_daily_mst AS t WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN @Batch AS b
                        ON b.page_id = t.page_id
                       AND b.VisitDate = t.VisitDate
                WHERE t.IsDeleted = 0;

                INSERT INTO drs_page_visit_daily_mst
                (
                    page_id, VisitDate, VisitCount, UniqueVisitCount,
                    IsActive, IsDeleted, CreatedDate
                )
                SELECT b.page_id, b.VisitDate, b.Visits, b.Uniques, 1, 0, GETDATE()
                FROM @Batch AS b
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM drs_page_visit_daily_mst AS t WITH (UPDLOCK, HOLDLOCK)
                    WHERE t.page_id = b.page_id
                      AND t.VisitDate = b.VisitDate
                      AND t.IsDeleted = 0
                );

                COMMIT TRANSACTION;";

            DbHelper.ValidateQuerySafety(sql);

            var db = new DbHelper(_config);
            await using SqlConnection con = await db.GetOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, con)
            {
                CommandTimeout = CommandTimeoutSeconds,
            };
            cmd.Parameters.AddRange(parameters.ToArray());
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private void Restore(List<(string PageKey, DateTime Date, int Visits, int Unique)> batch)
        {
            DateTime oldest = DateTime.Today.AddDays(-2);
            foreach (var item in batch)
            {
                if (item.Date < oldest)
                {
                    continue;
                }

                var counter = _buffer.GetOrAdd((item.PageKey, item.Date), _ => new Counter());
                Interlocked.Add(ref counter.Visits, item.Visits);
                Interlocked.Add(ref counter.UniqueVisits, item.Unique);
            }
            batch.Clear();
        }

        private static bool IsTransient(SqlException ex)
        {
            foreach (SqlError error in ex.Errors)
            {
                switch (error.Number)
                {
                    case -2: // timeout
                    case 1205: // deadlock victim
                    case 1222: // lock request timeout
                    case 4060: // database unavailable
                    case 40197:
                    case 40501:
                    case 40613:
                    case 49918:
                    case 49919:
                    case 49920:
                    case 10928:
                    case 10929:
                    case 233:
                    case 64:
                        return true;
                }
            }
            return false;
        }
        #endregion

        #region Logging
        private void LogDroppedVisits()
        {
            int dropped = Interlocked.Exchange(ref _droppedSinceLastLog, 0);
            if (dropped > 0)
            {
                Logs.Warning($"Page visit buffer full | Visits not counted:{dropped}");
            }
        }

        private void LogThrottled(string message, Exception ex)
        {
            DateTime now = DateTime.UtcNow;
            if ((now - _lastErrorLogUtc).TotalSeconds < 60)
            {
                return;
            }
            _lastErrorLogUtc = now;
            Logs.Error(message, ex);
        }
        #endregion
    }

    public sealed class PageVisitFlushWorker : BackgroundService
    {
        private readonly IPageVisitService _pageVisits;
        private readonly TimeSpan _flushInterval;
        private readonly TimeSpan _pageRefreshInterval = TimeSpan.FromMinutes(10);

        public PageVisitFlushWorker(IPageVisitService pageVisits, IConfiguration config)
        {
            _pageVisits = pageVisits;
            int seconds = config.GetValue<int>("WebsiteStatistics:FlushIntervalSeconds", 10);
            _flushInterval = TimeSpan.FromSeconds(Math.Clamp(seconds, 2, 300));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DateTime lastRefreshUtc = DateTime.MinValue;
            using var timer = new PeriodicTimer(_flushInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (DateTime.UtcNow - lastRefreshUtc >= _pageRefreshInterval)
                    {
                        await _pageVisits.RefreshPagesAsync(stoppingToken);
                        lastRefreshUtc = DateTime.UtcNow;
                    }

                    if (!await timer.WaitForNextTickAsync(stoppingToken))
                    {
                        break;
                    }

                    await _pageVisits.FlushAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logs.Error("Page visit worker iteration failed", ex);
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                await _pageVisits.FlushAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Logs.Error("Final page visit flush failed during shutdown", ex);
            }
        }
    }
}
