using System.Diagnostics;
using System.Security.Claims;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.BL.Services;
using Khel_Akhel_Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Khel_Akhel_Server.Models.CommonResponse;

namespace Khel_Akhel_Server.Controllers.Wishlist
{
    [ApiController]
    [Route("api/wishlist")]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;
        private readonly IUrlEncryptionService _encryption;
        private readonly IWebHostEnvironment _env;

        private const string GuestWishlistCookieName = "ka_guest_wishlist";
        private const string GuestWishlistHeaderName = "X-Guest-Wishlist-Token";

        public WishlistController(
            IWishlistService wishlistService,
            IUrlEncryptionService encryption,
            IWebHostEnvironment env
        )
        {
            _wishlistService = wishlistService;
            _encryption = encryption;
            _env = env;
        }

        #region Helper Methods
        private long GetAuthenticatedUserId()
        {
            var claim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && long.TryParse(claim.Value, out long userId))
            {
                return userId;
            }
            return 0;
        }

        private string? GetGuestToken()
        {
            if (
                Request.Cookies.TryGetValue(GuestWishlistCookieName, out string? cookieVal)
                && !string.IsNullOrWhiteSpace(cookieVal)
            )
            {
                return cookieVal;
            }

            if (
                Request.Headers.TryGetValue(GuestWishlistHeaderName, out var headerVal)
                && !string.IsNullOrWhiteSpace(headerVal)
            )
            {
                return headerVal.ToString();
            }

            return null;
        }

        private void SetGuestWishlistCookie(string token)
        {
            bool isDev = _env.IsDevelopment();
            bool isHttps = Request.IsHttps;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDev || isHttps,
                SameSite = isDev
                    ? (isHttps ? SameSiteMode.None : SameSiteMode.Lax)
                    : SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(30),
                IsEssential = true,
                Path = "/api/wishlist",
            };

            Response.Cookies.Append(GuestWishlistCookieName, token, cookieOptions);
        }

        private void DeleteGuestWishlistCookie()
        {
            bool isDev = _env.IsDevelopment();
            bool isHttps = Request.IsHttps;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDev || isHttps,
                SameSite = isDev
                    ? (isHttps ? SameSiteMode.None : SameSiteMode.Lax)
                    : SameSiteMode.Strict,
                Path = "/api/wishlist",
                Expires = DateTime.UtcNow.AddDays(-1),
            };

            Response.Cookies.Delete(GuestWishlistCookieName, cookieOptions);
        }

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return Request.Headers["User-Agent"].ToString() ?? "Unknown";
        }
        #endregion

        #region Get Wishlist API
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetWishlist()
        {
            Logs.Info("GetWishlist API started");
            var sw = Stopwatch.StartNew();

            try
            {
                long customerId = GetAuthenticatedUserId();
                string? guestToken = customerId > 0 ? null : GetGuestToken();

                var (wishlist, issuedToken) = await _wishlistService.GetOrCreateWishlistAsync(
                    customerId > 0 ? customerId : null,
                    guestToken
                );

                if (!string.IsNullOrWhiteSpace(issuedToken))
                {
                    SetGuestWishlistCookie(issuedToken);
                }

                sw.Stop();
                Logs.Info(
                    $"GetWishlist completed | TotalItems: {wishlist.TotalItems} IsGuest: {wishlist.IsGuest} Elapsed: {sw.ElapsedMilliseconds}ms"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "WISHLIST_RETRIEVED",
                        Data = wishlist,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetWishlist API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string> { "An internal server error occurred." },
                    }
                );
            }
        }
        #endregion

        #region Add Wishlist Item API
        [HttpPost("items")]
        [AllowAnonymous]
        public async Task<IActionResult> AddWishlistItem([FromBody] AddWishlistItemRequest? request)
        {
            Logs.Info("AddWishlistItem API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.EncryptedProductId))
                {
                    sw.Stop();
                    Logs.Warning(
                        "AddWishlistItem rejected | Reason: Null request body or missing encrypted product ID"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = new List<string> { "Encrypted product ID is required." },
                        }
                    );
                }

                if (
                    !_encryption.TryDecrypt(request.EncryptedProductId, out long productId)
                    || productId <= 0
                )
                {
                    sw.Stop();
                    Logs.Warning(
                        $"AddWishlistItem rejected | Reason: Invalid or tampered encrypted product ID '{request.EncryptedProductId}'"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string>
                            {
                                "Invalid or tampered encrypted product ID.",
                            },
                        }
                    );
                }

                long customerId = GetAuthenticatedUserId();
                string? guestToken = customerId > 0 ? null : GetGuestToken();

                var (success, message, issuedToken) = await _wishlistService.AddItemAsync(
                    customerId > 0 ? customerId : null,
                    guestToken,
                    productId,
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                if (!string.IsNullOrWhiteSpace(issuedToken))
                {
                    SetGuestWishlistCookie(issuedToken);
                }

                if (!success)
                {
                    sw.Stop();
                    int statusCode =
                        (message == "PRODUCT_INACTIVE" || message == "PRODUCT_NOT_FOUND")
                            ? 404
                            : 400;
                    Logs.Warning(
                        $"AddWishlistItem rejected | Reason: Service returned failure '{message}'"
                    );
                    return StatusCode(
                        statusCode,
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = statusCode,
                            Message = message,
                            Errors = new List<string> { $"Cannot add item to wishlist: {message}" },
                        }
                    );
                }

                var (updatedWishlist, _) = await _wishlistService.GetOrCreateWishlistAsync(
                    customerId > 0 ? customerId : null,
                    guestToken ?? issuedToken
                );

                sw.Stop();
                Logs.Info(
                    $"AddWishlistItem completed | ProductId: {productId} CustomerId: {customerId} Elapsed: {sw.ElapsedMilliseconds}ms"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = message,
                        Data = updatedWishlist,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in AddWishlistItem API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string> { "An internal server error occurred." },
                    }
                );
            }
        }
        #endregion

        #region Remove Wishlist Item API
        [HttpDelete("items/{encryptedProductId}")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveWishlistItem(string encryptedProductId)
        {
            Logs.Info("RemoveWishlistItem API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (
                    string.IsNullOrWhiteSpace(encryptedProductId)
                    || !_encryption.TryDecrypt(encryptedProductId, out long productId)
                    || productId <= 0
                )
                {
                    sw.Stop();
                    Logs.Warning(
                        $"RemoveWishlistItem rejected | Reason: Invalid or tampered encrypted product ID '{encryptedProductId}'"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string>
                            {
                                "Invalid or tampered encrypted product ID.",
                            },
                        }
                    );
                }

                long customerId = GetAuthenticatedUserId();
                string? guestToken = customerId > 0 ? null : GetGuestToken();

                var (success, message) = await _wishlistService.RemoveItemAsync(
                    customerId > 0 ? customerId : null,
                    guestToken,
                    productId,
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                if (!success)
                {
                    sw.Stop();
                    int statusCode =
                        (message == "WISHLIST_NOT_FOUND" || message == "WISHLIST_ITEM_NOT_FOUND")
                            ? 404
                            : 400;
                    Logs.Warning(
                        $"RemoveWishlistItem rejected | Reason: Service returned failure '{message}'"
                    );
                    return StatusCode(
                        statusCode,
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = statusCode,
                            Message = message,
                            Errors = new List<string> { message },
                        }
                    );
                }

                var (updatedWishlist, _) = await _wishlistService.GetOrCreateWishlistAsync(
                    customerId > 0 ? customerId : null,
                    guestToken
                );

                sw.Stop();
                Logs.Info(
                    $"RemoveWishlistItem completed | ProductId: {productId} CustomerId: {customerId} Elapsed: {sw.ElapsedMilliseconds}ms"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = message,
                        Data = updatedWishlist,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in RemoveWishlistItem API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string> { "An internal server error occurred." },
                    }
                );
            }
        }
        #endregion

        #region Clear Wishlist API
        [HttpDelete("items")]
        [AllowAnonymous]
        public async Task<IActionResult> ClearWishlist()
        {
            Logs.Info("ClearWishlist API started");
            var sw = Stopwatch.StartNew();

            try
            {
                long customerId = GetAuthenticatedUserId();
                string? guestToken = customerId > 0 ? null : GetGuestToken();

                var (success, message) = await _wishlistService.ClearWishlistAsync(
                    customerId > 0 ? customerId : null,
                    guestToken,
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info(
                    $"ClearWishlist completed | CustomerId: {customerId} Elapsed: {sw.ElapsedMilliseconds}ms"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = message,
                        Data = new { TotalItems = 0 },
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ClearWishlist API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string> { "An internal server error occurred." },
                    }
                );
            }
        }
        #endregion

        #region Merge Guest Wishlist API
        [HttpPost("merge")]
        [Authorize]
        public async Task<IActionResult> MergeGuestWishlist(
            [FromBody] MergeWishlistRequest? request
        )
        {
            Logs.Info("MergeGuestWishlist API started");
            var sw = Stopwatch.StartNew();

            try
            {
                long customerId = GetAuthenticatedUserId();
                if (customerId <= 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        "MergeGuestWishlist rejected | Reason: Unauthenticated customer access attempt"
                    );
                    return Unauthorized(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 401,
                            Message = "UNAUTHORIZED",
                            Errors = new List<string>
                            {
                                "Authentication required to merge wishlist.",
                            },
                        }
                    );
                }

                // Prefer cookie; fallback to request.GuestToken
                string? guestToken = GetGuestToken();
                if (
                    string.IsNullOrWhiteSpace(guestToken)
                    && request != null
                    && !string.IsNullOrWhiteSpace(request.GuestToken)
                )
                {
                    guestToken = request.GuestToken;
                }

                var (success, message, mergedCount) =
                    await _wishlistService.MergeGuestWishlistAsync(
                        customerId,
                        guestToken,
                        GetClientIpAddress(),
                        GetUserAgent()
                    );

                if (!success)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"MergeGuestWishlist rejected | Reason: Service returned failure '{message}'"
                    );
                    return StatusCode(
                        400,
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = message,
                            Errors = new List<string>
                            {
                                $"Failed to merge guest wishlist: {message}",
                            },
                        }
                    );
                }

                // Delete guest cookie ONLY after confirmed successful merge
                if (!string.IsNullOrWhiteSpace(guestToken))
                {
                    DeleteGuestWishlistCookie();
                }

                var (updatedWishlist, _) = await _wishlistService.GetOrCreateWishlistAsync(
                    customerId,
                    null
                );

                sw.Stop();
                Logs.Info(
                    $"MergeGuestWishlist completed | CustomerId: {customerId} MergedCount: {mergedCount} Elapsed: {sw.ElapsedMilliseconds}ms"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = message,
                        Data = new { MergedCount = mergedCount, Wishlist = updatedWishlist },
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in MergeGuestWishlist API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string> { "An internal server error occurred." },
                    }
                );
            }
        }
        #endregion

        #region Move Wishlist Item To Cart API
        [HttpPost("items/{encryptedProductId}/move-to-cart")]
        [Authorize]
        public async Task<IActionResult> MoveToCart(
            string encryptedProductId,
            [FromBody] MoveToCartRequest? request
        )
        {
            Logs.Info("MoveToCart API started");
            var sw = Stopwatch.StartNew();

            try
            {
                long customerId = GetAuthenticatedUserId();
                if (customerId <= 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        "MoveToCart rejected | Reason: Unauthenticated customer access attempt"
                    );
                    return Unauthorized(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 401,
                            Message = "UNAUTHORIZED",
                            Errors = new List<string>
                            {
                                "Authentication required to move wishlist items to cart.",
                            },
                        }
                    );
                }

                if (
                    string.IsNullOrWhiteSpace(encryptedProductId)
                    || !_encryption.TryDecrypt(encryptedProductId, out long productId)
                    || productId <= 0
                )
                {
                    sw.Stop();
                    Logs.Warning(
                        $"MoveToCart rejected | Reason: Invalid or tampered encrypted product ID '{encryptedProductId}'"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string>
                            {
                                "Invalid or tampered encrypted product ID.",
                            },
                        }
                    );
                }

                int quantity = request != null && request.Quantity > 0 ? request.Quantity : 1;

                var (success, message, cart) = await _wishlistService.MoveToCartAsync(
                    customerId,
                    productId,
                    quantity,
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                if (!success)
                {
                    sw.Stop();
                    int statusCode = message switch
                    {
                        "WISHLIST_ITEM_NOT_FOUND" => 404,
                        "PRODUCT_NOT_FOUND" => 404,
                        "PRODUCT_INACTIVE" => 400,
                        "INSUFFICIENT_STOCK" => 400,
                        _ => 400,
                    };
                    Logs.Warning(
                        $"MoveToCart rejected | Reason: Service returned failure '{message}'"
                    );

                    return StatusCode(
                        statusCode,
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = statusCode,
                            Message = message,
                            Errors = new List<string> { $"Cannot move item to cart: {message}" },
                        }
                    );
                }

                sw.Stop();
                Logs.Info(
                    $"MoveToCart completed | CustomerId: {customerId} ProductId: {productId} Qty: {quantity} Elapsed: {sw.ElapsedMilliseconds}ms"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = message,
                        Data = cart,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in MoveToCart API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                        Errors = new List<string> { "An internal server error occurred." },
                    }
                );
            }
        }
        #endregion
    }
}
