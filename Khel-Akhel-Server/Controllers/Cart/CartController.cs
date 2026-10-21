using System.Diagnostics;
using System.Security.Claims;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.BL.Services;
using Khel_Akhel_Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Khel_Akhel_Server.Models.CommonResponse;

namespace Khel_Akhel_Server.Controllers.Cart
{
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IUrlEncryptionService _encryption;

        public CartController(ICartService cartService, IUrlEncryptionService encryption)
        {
            _cartService = cartService;
            _encryption = encryption;
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

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return Request.Headers["User-Agent"].ToString() ?? "Unknown";
        }
        #endregion

        #region Get Cart API
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            Logs.Info("GetCart API started");
            var sw = Stopwatch.StartNew();

            try
            {
                long customerId = GetAuthenticatedUserId();
                if (customerId <= 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        "GetCart rejected | Reason: Unauthenticated customer access attempt"
                    );
                    return Unauthorized(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 401,
                            Message = "UNAUTHORIZED",
                            Errors = new List<string> { "Customer authentication required." },
                        }
                    );
                }

                var cart = await _cartService.GetCustomerCartAsync(customerId);
                sw.Stop();
                Logs.Info(
                    $"GetCart completed | CustomerId:{customerId} TotalItems:{cart.TotalItems} SubTotal:{cart.SubTotal} Elapsed:{sw.ElapsedMilliseconds}ms"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "CART_RETRIEVED",
                        Data = cart,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetCart API", ex);
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

        #region Add Cart Item API
        [HttpPost("items")]
        public async Task<IActionResult> AddCartItem([FromBody] AddCartItemRequest? request)
        {
            Logs.Info("AddCartItem API started");
            var sw = Stopwatch.StartNew();

            try
            {
                long customerId = GetAuthenticatedUserId();
                if (customerId <= 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        "AddCartItem rejected | Reason: Unauthenticated customer access attempt"
                    );
                    return Unauthorized(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 401,
                            Message = "UNAUTHORIZED",
                            Errors = new List<string> { "Customer authentication required." },
                        }
                    );
                }

                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning("AddCartItem rejected | Reason: Null request body");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "REQUEST_BODY_REQUIRED",
                            Errors = new List<string> { "Request body cannot be null." },
                        }
                    );
                }

                if (string.IsNullOrWhiteSpace(request.EncryptedProductId))
                {
                    sw.Stop();
                    Logs.Warning("AddCartItem rejected | Reason: Missing encrypted product ID");
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
                        $"AddCartItem rejected | Reason: Invalid EncryptedProductId: {request.EncryptedProductId}"
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

                if (request.Quantity <= 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"AddCartItem rejected | Reason: Invalid quantity: {request.Quantity}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_QUANTITY",
                            Errors = new List<string> { "Quantity must be greater than zero." },
                        }
                    );
                }

                var (success, message, cart) = await _cartService.AddItemAsync(
                    customerId,
                    productId,
                    request.Quantity,
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                if (!success)
                {
                    sw.Stop();
                    int statusCode = message switch
                    {
                        "PRODUCT_NOT_FOUND" => 404,
                        "PRODUCT_INACTIVE" => 400,
                        "INSUFFICIENT_STOCK" => 400,
                        _ => 400,
                    };

                    Logs.Warning(
                        $"AddCartItem failed | CustomerId:{customerId} ProductId:{productId} Reason:{message}"
                    );

                    return StatusCode(
                        statusCode,
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = statusCode,
                            Message = message,
                            Errors = new List<string> { $"Failed to add item to cart: {message}" },
                        }
                    );
                }

                sw.Stop();
                Logs.Info(
                    $"AddCartItem completed | CustomerId:{customerId} ProductId:{productId} Qty:{request.Quantity} Elapsed:{sw.ElapsedMilliseconds}ms"
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
                Logs.Error("Exception occurred in AddCartItem API", ex);
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

        #region Update Cart Item Quantity API
        [HttpPatch("items/{encryptedProductId}")]
        public async Task<IActionResult> UpdateCartItemQuantity(
            string encryptedProductId,
            [FromBody] UpdateCartItemQuantityRequest? request
        )
        {
            Logs.Info("UpdateCartItemQuantity API started");
            var sw = Stopwatch.StartNew();

            try
            {
                long customerId = GetAuthenticatedUserId();
                if (customerId <= 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        "UpdateCartItemQuantity rejected | Reason: Unauthenticated customer access attempt"
                    );
                    return Unauthorized(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 401,
                            Message = "UNAUTHORIZED",
                            Errors = new List<string> { "Customer authentication required." },
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
                        $"UpdateCartItemQuantity rejected | Reason: Invalid EncryptedProductId: {encryptedProductId}"
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

                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning("UpdateCartItemQuantity rejected | Reason: Null request body");
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "REQUEST_BODY_REQUIRED",
                            Errors = new List<string> { "Request body cannot be null." },
                        }
                    );
                }

                if (request.Quantity <= 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        $"UpdateCartItemQuantity rejected | Reason: Invalid quantity: {request.Quantity}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_QUANTITY",
                            Errors = new List<string> { "Quantity must be greater than zero." },
                        }
                    );
                }

                var (success, message, cart) = await _cartService.UpdateItemQuantityAsync(
                    customerId,
                    productId,
                    request.Quantity,
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                if (!success)
                {
                    sw.Stop();
                    int statusCode = message switch
                    {
                        "CART_NOT_FOUND" => 404,
                        "CART_ITEM_NOT_FOUND" => 404,
                        "PRODUCT_INACTIVE" => 400,
                        "INSUFFICIENT_STOCK" => 400,
                        _ => 400,
                    };

                    Logs.Warning(
                        $"UpdateCartItemQuantity failed | CustomerId:{customerId} ProductId:{productId} Reason:{message}"
                    );

                    return StatusCode(
                        statusCode,
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = statusCode,
                            Message = message,
                            Errors = new List<string> { $"Cannot update cart item: {message}" },
                        }
                    );
                }

                sw.Stop();
                Logs.Info(
                    $"UpdateCartItemQuantity completed | CustomerId:{customerId} ProductId:{productId} Qty:{request.Quantity} Elapsed:{sw.ElapsedMilliseconds}ms"
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
                Logs.Error("Exception occurred in UpdateCartItemQuantity API", ex);
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

        #region Remove Cart Item API
        [HttpDelete("items/{encryptedProductId}")]
        public async Task<IActionResult> RemoveCartItem(string encryptedProductId)
        {
            Logs.Info("RemoveCartItem API started");
            var sw = Stopwatch.StartNew();

            try
            {
                long customerId = GetAuthenticatedUserId();
                if (customerId <= 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        "RemoveCartItem rejected | Reason: Unauthenticated customer access attempt"
                    );
                    return Unauthorized(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 401,
                            Message = "UNAUTHORIZED",
                            Errors = new List<string> { "Customer authentication required." },
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
                        $"RemoveCartItem rejected | Reason: Invalid EncryptedProductId: {encryptedProductId}"
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

                var (success, message, cart) = await _cartService.RemoveItemAsync(
                    customerId,
                    productId,
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                if (!success)
                {
                    sw.Stop();
                    int statusCode = message switch
                    {
                        "CART_NOT_FOUND" => 404,
                        "CART_ITEM_NOT_FOUND" => 404,
                        _ => 400,
                    };

                    Logs.Warning(
                        $"RemoveCartItem failed | CustomerId:{customerId} ProductId:{productId} Reason:{message}"
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

                sw.Stop();
                Logs.Info(
                    $"RemoveCartItem completed | CustomerId:{customerId} ProductId:{productId} Elapsed:{sw.ElapsedMilliseconds}ms"
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
                Logs.Error("Exception occurred in RemoveCartItem API", ex);
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

        #region Clear Cart API
        [HttpDelete("items")]
        public async Task<IActionResult> ClearCart()
        {
            Logs.Info("ClearCart API started");
            var sw = Stopwatch.StartNew();

            try
            {
                long customerId = GetAuthenticatedUserId();
                if (customerId <= 0)
                {
                    sw.Stop();
                    Logs.Warning(
                        "ClearCart rejected | Reason: Unauthenticated customer access attempt"
                    );
                    return Unauthorized(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 401,
                            Message = "UNAUTHORIZED",
                            Errors = new List<string> { "Customer authentication required." },
                        }
                    );
                }

                var (success, message) = await _cartService.ClearCartAsync(
                    customerId,
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info(
                    $"ClearCart completed | CustomerId:{customerId} Elapsed:{sw.ElapsedMilliseconds}ms"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = message,
                        Data = new CartResponse
                        {
                            CartStatus = "Empty",
                            TotalItems = 0,
                            SubTotal = 0m,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in ClearCart API", ex);
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
