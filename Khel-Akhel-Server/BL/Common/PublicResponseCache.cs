using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using static Khel_Akhel_Server.Models.CommonResponse;

namespace Khel_Akhel_Server.BL.Common
{
    public static class PublicCacheRegions
    {
        public const string Categories = "categories";
        public const string Products = "products";
        public const string Faqs = "faqs";
        public const string HeroBanners = "hero-banners";
        public const string Projects = "projects";
        public const string Locations = "locations";

        private const int MaxEntriesPerRegion = 300;

        private static readonly ConcurrentDictionary<string, CancellationTokenSource> Tokens =
            new();
        private static readonly ConcurrentDictionary<string, int> EntryCounts = new();

        public static bool TryReserveEntry(string region)
        {
            int count = EntryCounts.AddOrUpdate(region, 1, (_, current) => current + 1);
            if (count <= MaxEntriesPerRegion)
            {
                return true;
            }
            ReleaseEntry(region);
            return false;
        }

        public static void ReleaseEntry(string region)
        {
            EntryCounts.AddOrUpdate(region, 0, (_, current) => Math.Max(0, current - 1));
        }

        public static IChangeToken GetChangeToken(string region)
        {
            var source = Tokens.GetOrAdd(region, _ => new CancellationTokenSource());
            return new CancellationChangeToken(source.Token);
        }

        public static void Invalidate(string region)
        {
            if (Tokens.TryRemove(region, out var source))
            {
                source.Cancel();
                source.Dispose();
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PublicResponseCacheAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _region;
        private readonly int _seconds;

        public PublicResponseCacheAttribute(string region, int seconds)
        {
            _region = region;
            _seconds = seconds;
        }

        public bool SkipForAdmin { get; set; }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next
        )
        {
            HttpContext http = context.HttpContext;
            if (
                !HttpMethods.IsGet(http.Request.Method)
                || (SkipForAdmin && http.User.IsInRole("Admin"))
                || http.Request.QueryString.Value?.Length > 300
            )
            {
                await next();
                return;
            }

            var cache = http.RequestServices.GetRequiredService<IMemoryCache>();
            string query = string.Join(
                "&",
                http.Request.Query.OrderBy(q => q.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(q => $"{q.Key.ToLowerInvariant()}={q.Value}")
            );
            string key =
                $"public-cache:{_region}:{http.Request.Path.Value?.ToLowerInvariant()}?{query}";

            if (cache.TryGetValue(key, out ApiResponse? cached) && cached != null)
            {
                context.Result = new OkObjectResult(cached);
                return;
            }

            ActionExecutedContext executed = await next();
            if (
                executed.Exception == null
                && executed.Result is ObjectResult result
                && (result.StatusCode ?? StatusCodes.Status200OK) == StatusCodes.Status200OK
                && result.Value is ApiResponse response
                && response.Success
            )
            {
                if (!PublicCacheRegions.TryReserveEntry(_region))
                {
                    return;
                }

                var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(_seconds))
                    .AddExpirationToken(PublicCacheRegions.GetChangeToken(_region))
                    .RegisterPostEvictionCallback(
                        (_, _, _, state) => PublicCacheRegions.ReleaseEntry((string)state!),
                        _region
                    );
                cache.Set(key, response, options);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class InvalidatesPublicCacheAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string[] _regions;

        public InvalidatesPublicCacheAttribute(params string[] regions)
        {
            _regions = regions;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next
        )
        {
            ActionExecutedContext executed = await next();
            if (HttpMethods.IsGet(context.HttpContext.Request.Method) || executed.Exception != null)
            {
                return;
            }

            int status = executed.Result switch
            {
                ObjectResult objectResult => objectResult.StatusCode ?? StatusCodes.Status200OK,
                StatusCodeResult statusResult => statusResult.StatusCode,
                _ => StatusCodes.Status200OK,
            };

            if (status < 400)
            {
                foreach (string region in _regions)
                {
                    PublicCacheRegions.Invalidate(region);
                }
            }
        }
    }
}
