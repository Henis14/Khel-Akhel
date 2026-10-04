using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.BL.Services;
using Khel_Akhel_Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using static Khel_Akhel_Server.Models.CommonResponse;

namespace Khel_Akhel_Server.Controllers.Customer
{
    [ApiController]
    [Route("api/customer-address")]
    [Authorize]
    public class CustomerAddressController : ControllerBase
    {
        private readonly DbHelper _db;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService _audit;

        public CustomerAddressController(
            DbHelper db,
            IUrlEncryptionService encryption,
            IAuditService audit
        )
        {
            _db = db;
            _encryption = encryption;
            _audit = audit;
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

        private bool IsCurrentUserAdmin()
        {
            return User.IsInRole("Admin");
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

        #region 3.1 CustomerAddressCreate
        [HttpPost("create")]
        public async Task<IActionResult> CustomerAddressCreate(
            [FromBody] CustomerAddressCreateRequest? request
        )
        {
            Logs.Info("CustomerAddressCreate API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    sw.Stop();
                    Logs.Warning(
                        "CustomerAddressCreate request rejected | Reason: Null request body"
                    );
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

                long customerId = GetAuthenticatedUserId();
                if (customerId <= 0)
                {
                    sw.Stop();
                    return Unauthorized(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 401,
                            Message = "UNAUTHORIZED",
                            Errors = new List<string> { "Authentication required." },
                        }
                    );
                }

                // Trim string properties
                request.AddressTitle = request.AddressTitle?.Trim() ?? "Home";
                request.AddressType = request.AddressType?.Trim() ?? "Shipping";
                request.FullName = request.FullName?.Trim() ?? string.Empty;
                request.MobileNo = request.MobileNo?.Trim() ?? string.Empty;
                request.AddressLine1 = request.AddressLine1?.Trim() ?? string.Empty;
                request.AddressLine2 = request.AddressLine2?.Trim() ?? string.Empty;
                request.Landmark = request.Landmark?.Trim() ?? string.Empty;
                request.City = request.City?.Trim() ?? string.Empty;
                request.State = request.State?.Trim() ?? string.Empty;
                request.Country = request.Country?.Trim() ?? string.Empty;
                request.Pincode = request.Pincode?.Trim() ?? string.Empty;

                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.FullName))
                    errors.Add("Full name is required.");
                else if (!ValidationHelper.IsValidAddressFullName(request.FullName))
                    errors.Add("Full name must be between 2 and 200 characters.");

                if (string.IsNullOrWhiteSpace(request.MobileNo))
                    errors.Add("Mobile number is required.");
                else if (!ValidationHelper.IsValidMobile(request.MobileNo))
                    errors.Add("Invalid mobile number format.");

                if (string.IsNullOrWhiteSpace(request.AddressLine1))
                    errors.Add("Address line 1 is required.");
                else if (!ValidationHelper.IsValidAddressLine1(request.AddressLine1))
                    errors.Add("Address line 1 cannot exceed 255 characters.");

                if (!ValidationHelper.IsValidAddressLine2(request.AddressLine2))
                    errors.Add("Address line 2 cannot exceed 255 characters.");

                if (!ValidationHelper.IsValidLandmark(request.Landmark))
                    errors.Add("Landmark cannot exceed 150 characters.");

                if (string.IsNullOrWhiteSpace(request.City))
                    errors.Add("City is required.");
                else if (!ValidationHelper.IsValidCity(request.City))
                    errors.Add(
                        "City must be 2-100 characters containing only letters, spaces, dots, hyphens, and apostrophes."
                    );

                if (request.CountryId <= 0)
                {
                    errors.Add("Country selection is required.");
                }
                else
                {
                    string checkCountryQuery =
                        "SELECT CountryName FROM dbo.drs_country_mst WITH (NOLOCK) WHERE ID = @ID AND IsActive = 1 AND IsDeleted = 0";
                    object? countryNameObj = _db.ExecuteScalar(
                        checkCountryQuery,
                        new[] { new SqlParameter("@ID", request.CountryId) }
                    );
                    if (countryNameObj == null)
                    {
                        errors.Add("Selected Country is invalid or inactive.");
                    }
                    else
                    {
                        request.Country = Convert.ToString(countryNameObj) ?? string.Empty;
                    }
                }

                if (request.StateId <= 0)
                {
                    errors.Add("State selection is required.");
                }
                else if (request.CountryId > 0)
                {
                    string checkStateQuery =
                        "SELECT StateName FROM dbo.drs_state_mst WITH (NOLOCK) WHERE ID = @StateId AND country_id = @CountryId AND IsActive = 1 AND IsDeleted = 0";
                    object? stateNameObj = _db.ExecuteScalar(
                        checkStateQuery,
                        new[]
                        {
                            new SqlParameter("@StateId", request.StateId),
                            new SqlParameter("@CountryId", request.CountryId),
                        }
                    );
                    if (stateNameObj == null)
                    {
                        errors.Add("Selected State is invalid for the selected Country.");
                    }
                    else
                    {
                        request.State = Convert.ToString(stateNameObj) ?? string.Empty;
                    }
                }

                if (string.IsNullOrWhiteSpace(request.Pincode))
                    errors.Add("Pincode is required.");
                else if (!ValidationHelper.IsValidPincode(request.Pincode))
                    errors.Add("Invalid pincode format.");

                if (errors.Any())
                {
                    sw.Stop();
                    Logs.Warning(
                        $"CustomerAddressCreate validation failed | Errors:{string.Join(", ", errors)}"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = errors,
                        }
                    );
                }

                // Check active address count to auto-set default if first address
                string countQuery =
                    @"
                    SELECT COUNT(1)
                    FROM drs_customer_address_mst WITH (NOLOCK)
                    WHERE customer_id = @CustomerId AND IsDeleted = 0";

                int existingCount = Convert.ToInt32(
                    _db.ExecuteScalar(
                        countQuery,
                        new[] { new SqlParameter("@CustomerId", customerId) }
                    ) ?? 0
                );

                bool setAsDefault = request.IsDefault || existingCount == 0;

                using SqlConnection con = _db.GetOpenConnection();
                using SqlTransaction tx = con.BeginTransaction();

                try
                {
                    if (setAsDefault)
                    {
                        string resetDefaultQuery =
                            @"
                            UPDATE drs_customer_address_mst
                            SET IsDefault = 0, ModifiedDate = GETDATE()
                            WHERE customer_id = @CustomerId AND IsDeleted = 0";

                        using SqlCommand resetCmd = new(resetDefaultQuery, con, tx);
                        resetCmd.Parameters.AddWithValue("@CustomerId", customerId);
                        resetCmd.ExecuteNonQuery();
                    }

                    string insertQuery =
                        @"
                        INSERT INTO drs_customer_address_mst
                        (customer_id, AddressTitle, AddressType, FullName, MobileNo, AddressLine1, AddressLine2, Landmark, country_id, state_id, City, State, Country, Pincode, IsDefault, IsActive, IsDeleted, CreatedDate)
                        VALUES
                        (@CustomerId, @AddressTitle, @AddressType, @FullName, @MobileNo, @AddressLine1, @AddressLine2, @Landmark, @CountryId, @StateId, @City, @State, @Country, @Pincode, @IsDefault, 1, 0, GETDATE());
                        SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

                    using SqlCommand insertCmd = new(insertQuery, con, tx);
                    insertCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    insertCmd.Parameters.AddWithValue(
                        "@AddressTitle",
                        (object?)request.AddressTitle ?? DBNull.Value
                    );
                    insertCmd.Parameters.AddWithValue(
                        "@AddressType",
                        (object?)request.AddressType ?? DBNull.Value
                    );
                    insertCmd.Parameters.AddWithValue("@FullName", request.FullName);
                    insertCmd.Parameters.AddWithValue("@MobileNo", request.MobileNo);
                    insertCmd.Parameters.AddWithValue("@AddressLine1", request.AddressLine1);
                    insertCmd.Parameters.AddWithValue(
                        "@AddressLine2",
                        (object?)request.AddressLine2 ?? DBNull.Value
                    );
                    insertCmd.Parameters.AddWithValue(
                        "@Landmark",
                        (object?)request.Landmark ?? DBNull.Value
                    );
                    insertCmd.Parameters.AddWithValue("@CountryId", request.CountryId);
                    insertCmd.Parameters.AddWithValue("@StateId", request.StateId);
                    insertCmd.Parameters.AddWithValue("@City", request.City);
                    insertCmd.Parameters.AddWithValue("@State", request.State);
                    insertCmd.Parameters.AddWithValue("@Country", request.Country);
                    insertCmd.Parameters.AddWithValue("@Pincode", request.Pincode);
                    insertCmd.Parameters.AddWithValue("@IsDefault", setAsDefault);

                    long addressId = Convert.ToInt64(insertCmd.ExecuteScalar());
                    tx.Commit();

                    string encryptedAddressId = _encryption.Encrypt(addressId);
                    var responseData = new CustomerAddressResponse
                    {
                        EncryptedAddressId = encryptedAddressId,
                        AddressTitle = request.AddressTitle ?? string.Empty,
                        AddressType = request.AddressType ?? string.Empty,
                        FullName = request.FullName ?? string.Empty,
                        MobileNo = request.MobileNo ?? string.Empty,
                        AddressLine1 = request.AddressLine1 ?? string.Empty,
                        AddressLine2 = request.AddressLine2 ?? string.Empty,
                        Landmark = request.Landmark ?? string.Empty,
                        City = request.City ?? string.Empty,
                        State = request.State ?? string.Empty,
                        Country = request.Country ?? string.Empty,
                        Pincode = request.Pincode ?? string.Empty,
                        IsDefault = setAsDefault,
                        IsActive = true,
                        CreatedDate = DateTimeFormat.Format(DateTime.Now),
                    };

                    await _audit.InsertAuditAsync(
                        customerId,
                        "CustomerAddress",
                        "CustomerAddressCreate",
                        "drs_customer_address_mst",
                        addressId,
                        null,
                        System.Text.Json.JsonSerializer.Serialize(responseData),
                        GetClientIpAddress(),
                        GetUserAgent()
                    );

                    sw.Stop();
                    Logs.Info(
                        $"CustomerAddressCreate completed | CustomerId:{customerId} AddressId:{addressId}"
                    );

                    return Ok(
                        new ApiResponse
                        {
                            Success = true,
                            StatusCode = 200,
                            Message = "ADDRESS_CREATED",
                            Data = responseData,
                        }
                    );
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in CustomerAddressCreate API", ex);
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

        #region 3.2 CustomerAddressUpdate [PATCH]
        [HttpPatch("update")]
        public async Task<IActionResult> CustomerAddressUpdate(
            [FromBody] CustomerAddressUpdateRequest? request
        )
        {
            Logs.Info("CustomerAddressUpdate API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.EncryptedAddressId))
                {
                    sw.Stop();
                    Logs.Warning(
                        "CustomerAddressUpdate rejected | Missing address ID or request body"
                    );
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = new List<string> { "Address ID is required." },
                        }
                    );
                }

                if (
                    !_encryption.TryDecrypt(request.EncryptedAddressId, out long addressId)
                    || addressId <= 0
                )
                {
                    sw.Stop();
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string>
                            {
                                "Invalid or tampered encrypted address ID.",
                            },
                        }
                    );
                }

                long customerId = GetAuthenticatedUserId();

                // Fetch existing address record and verify ownership
                string fetchQuery =
                    @"
                    SELECT TOP 1 ID, customer_id, AddressTitle, AddressType, FullName, MobileNo, AddressLine1, AddressLine2, Landmark, City, State, Country, Pincode, IsDefault, IsActive
                    FROM drs_customer_address_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(
                    fetchQuery,
                    new[] { new SqlParameter("@ID", addressId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "ADDRESS_NOT_FOUND",
                            Errors = new List<string> { "Address record not found." },
                        }
                    );
                }

                DataRow existing = dt.Rows[0];
                long ownerCustomerId = Convert.ToInt64(existing["customer_id"]);

                if (ownerCustomerId != customerId && !IsCurrentUserAdmin())
                {
                    sw.Stop();
                    Logs.Warning(
                        $"CustomerAddressUpdate forbidden access | Caller:{customerId} AddressOwner:{ownerCustomerId}"
                    );
                    return StatusCode(
                        403,
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 403,
                            Message = "FORBIDDEN",
                            Errors = new List<string>
                            {
                                "You are not authorized to update this address.",
                            },
                        }
                    );
                }

                // Determine updated values
                string newTitle =
                    request.AddressTitle != null
                        ? request.AddressTitle.Trim()
                        : existing["AddressTitle"].ToString() ?? "";
                string newType =
                    request.AddressType != null
                        ? request.AddressType.Trim()
                        : existing["AddressType"].ToString() ?? "";
                string newFullName =
                    request.FullName != null
                        ? request.FullName.Trim()
                        : existing["FullName"].ToString() ?? "";
                string newMobileNo =
                    request.MobileNo != null
                        ? request.MobileNo.Trim()
                        : existing["MobileNo"].ToString() ?? "";
                string newLine1 =
                    request.AddressLine1 != null
                        ? request.AddressLine1.Trim()
                        : existing["AddressLine1"].ToString() ?? "";
                string newLine2 =
                    request.AddressLine2 != null
                        ? request.AddressLine2.Trim()
                        : existing["AddressLine2"].ToString() ?? "";
                string newLandmark =
                    request.Landmark != null
                        ? request.Landmark.Trim()
                        : existing["Landmark"].ToString() ?? "";
                string newCity =
                    request.City != null ? request.City.Trim() : existing["City"].ToString() ?? "";
                string newState =
                    request.State != null
                        ? request.State.Trim()
                        : existing["State"].ToString() ?? "";
                string newCountry =
                    request.Country != null
                        ? request.Country.Trim()
                        : existing["Country"].ToString() ?? "";
                string newPincode =
                    request.Pincode != null
                        ? request.Pincode.Trim()
                        : existing["Pincode"].ToString() ?? "";
                bool newIsDefault = request.IsDefault ?? Convert.ToBoolean(existing["IsDefault"]);

                var errors = new List<string>();
                if (request.MobileNo != null && !ValidationHelper.IsValidMobile(newMobileNo))
                    errors.Add("Invalid mobile number format.");
                if (request.Pincode != null && !ValidationHelper.IsValidPincode(newPincode))
                    errors.Add("Invalid pincode format.");

                if (errors.Any())
                {
                    sw.Stop();
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "VALIDATION_FAILED",
                            Errors = errors,
                        }
                    );
                }

                using SqlConnection con = _db.GetOpenConnection();
                using SqlTransaction tx = con.BeginTransaction();

                try
                {
                    if (newIsDefault && !Convert.ToBoolean(existing["IsDefault"]))
                    {
                        string resetDefaultQuery =
                            @"
                            UPDATE drs_customer_address_mst
                            SET IsDefault = 0, ModifiedDate = GETDATE()
                            WHERE customer_id = @CustomerId AND IsDeleted = 0";

                        using SqlCommand resetCmd = new(resetDefaultQuery, con, tx);
                        resetCmd.Parameters.AddWithValue("@CustomerId", ownerCustomerId);
                        resetCmd.ExecuteNonQuery();
                    }

                    string updateQuery =
                        @"
                        UPDATE drs_customer_address_mst
                        SET AddressTitle = @AddressTitle,
                            AddressType = @AddressType,
                            FullName = @FullName,
                            MobileNo = @MobileNo,
                            AddressLine1 = @AddressLine1,
                            AddressLine2 = @AddressLine2,
                            Landmark = @Landmark,
                            City = @City,
                            State = @State,
                            Country = @Country,
                            Pincode = @Pincode,
                            IsDefault = @IsDefault,
                            ModifiedDate = GETDATE()
                        WHERE ID = @ID AND IsDeleted = 0";

                    using SqlCommand updateCmd = new(updateQuery, con, tx);
                    updateCmd.Parameters.AddWithValue("@AddressTitle", newTitle);
                    updateCmd.Parameters.AddWithValue("@AddressType", newType);
                    updateCmd.Parameters.AddWithValue("@FullName", newFullName);
                    updateCmd.Parameters.AddWithValue("@MobileNo", newMobileNo);
                    updateCmd.Parameters.AddWithValue("@AddressLine1", newLine1);
                    updateCmd.Parameters.AddWithValue("@AddressLine2", newLine2);
                    updateCmd.Parameters.AddWithValue("@Landmark", newLandmark);
                    updateCmd.Parameters.AddWithValue("@City", newCity);
                    updateCmd.Parameters.AddWithValue("@State", newState);
                    updateCmd.Parameters.AddWithValue("@Country", newCountry);
                    updateCmd.Parameters.AddWithValue("@Pincode", newPincode);
                    updateCmd.Parameters.AddWithValue("@IsDefault", newIsDefault);
                    updateCmd.Parameters.AddWithValue("@ID", addressId);
                    updateCmd.ExecuteNonQuery();

                    tx.Commit();

                    var responseData = new CustomerAddressResponse
                    {
                        EncryptedAddressId = request.EncryptedAddressId,
                        AddressTitle = newTitle,
                        AddressType = newType,
                        FullName = newFullName,
                        MobileNo = newMobileNo,
                        AddressLine1 = newLine1,
                        AddressLine2 = newLine2,
                        Landmark = newLandmark,
                        City = newCity,
                        State = newState,
                        Country = newCountry,
                        Pincode = newPincode,
                        IsDefault = newIsDefault,
                        IsActive = Convert.ToBoolean(existing["IsActive"]),
                    };

                    await _audit.InsertAuditAsync(
                        customerId,
                        "CustomerAddress",
                        "CustomerAddressUpdate",
                        "drs_customer_address_mst",
                        addressId,
                        System.Text.Json.JsonSerializer.Serialize(
                            new
                            {
                                ID = addressId,
                                FullName = existing["FullName"]?.ToString() ?? "",
                            }
                        ),
                        System.Text.Json.JsonSerializer.Serialize(responseData),
                        GetClientIpAddress(),
                        GetUserAgent()
                    );

                    sw.Stop();
                    Logs.Info($"CustomerAddressUpdate completed | AddressId:{addressId}");

                    return Ok(
                        new ApiResponse
                        {
                            Success = true,
                            StatusCode = 200,
                            Message = "ADDRESS_UPDATED",
                            Data = responseData,
                        }
                    );
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in CustomerAddressUpdate API", ex);
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

        #region 3.3 GetCustomerAddressById [GET]
        [HttpGet("{encryptedId}")]
        public IActionResult GetCustomerAddressById([FromRoute] string encryptedId)
        {
            Logs.Info("GetCustomerAddressById API started");
            var sw = Stopwatch.StartNew();

            try
            {
                if (!_encryption.TryDecrypt(encryptedId, out long addressId) || addressId <= 0)
                {
                    sw.Stop();
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string>
                            {
                                "Invalid or tampered encrypted address ID.",
                            },
                        }
                    );
                }

                long customerId = GetAuthenticatedUserId();

                string query =
                    @"
                    SELECT TOP 1 ID, customer_id, AddressTitle, AddressType, FullName, MobileNo, AddressLine1, AddressLine2, Landmark, City, State, Country, Pincode, IsDefault, IsActive, CreatedDate
                    FROM drs_customer_address_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(
                    query,
                    new[] { new SqlParameter("@ID", addressId) }
                );

                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "ADDRESS_NOT_FOUND",
                            Errors = new List<string> { "Customer address record not found." },
                        }
                    );
                }

                DataRow row = dt.Rows[0];
                long ownerCustomerId = Convert.ToInt64(row["customer_id"]);

                if (ownerCustomerId != customerId && !IsCurrentUserAdmin())
                {
                    sw.Stop();
                    Logs.Warning(
                        $"GetCustomerAddressById forbidden | Caller:{customerId} Owner:{ownerCustomerId}"
                    );
                    return StatusCode(
                        403,
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 403,
                            Message = "FORBIDDEN",
                            Errors = new List<string>
                            {
                                "You are not authorized to view this address.",
                            },
                        }
                    );
                }

                var addressResponse = new CustomerAddressResponse
                {
                    EncryptedAddressId = _encryption.Encrypt(Convert.ToInt64(row["ID"])),
                    AddressTitle = row["AddressTitle"].ToString() ?? "",
                    AddressType = row["AddressType"].ToString() ?? "",
                    FullName = row["FullName"].ToString() ?? "",
                    MobileNo = row["MobileNo"].ToString() ?? "",
                    AddressLine1 = row["AddressLine1"].ToString() ?? "",
                    AddressLine2 = row["AddressLine2"].ToString() ?? "",
                    Landmark = row["Landmark"].ToString() ?? "",
                    City = row["City"].ToString() ?? "",
                    State = row["State"].ToString() ?? "",
                    Country = row["Country"].ToString() ?? "",
                    Pincode = row["Pincode"].ToString() ?? "",
                    IsDefault = Convert.ToBoolean(row["IsDefault"]),
                    IsActive = Convert.ToBoolean(row["IsActive"]),
                    CreatedDate = DateTimeFormat.Format(Convert.ToDateTime(row["CreatedDate"])),
                };

                sw.Stop();
                Logs.Info($"GetCustomerAddressById completed | AddressId:{addressId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = addressResponse,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetCustomerAddressById API", ex);
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

        #region 3.4 GetCustomerAddresses [GET]
        [HttpGet("list")]
        public IActionResult GetCustomerAddresses([FromQuery] string? encryptedCustomerId = null)
        {
            Logs.Info("GetCustomerAddresses API started");
            var sw = Stopwatch.StartNew();

            try
            {
                long customerId = GetAuthenticatedUserId();

                if (IsCurrentUserAdmin() && !string.IsNullOrWhiteSpace(encryptedCustomerId))
                {
                    if (
                        _encryption.TryDecrypt(encryptedCustomerId, out long adminTargetId)
                        && adminTargetId > 0
                    )
                    {
                        customerId = adminTargetId;
                    }
                }

                string query =
                    @"
                    SELECT ID, customer_id, AddressTitle, AddressType, FullName, MobileNo, AddressLine1, AddressLine2, Landmark, City, State, Country, Pincode, IsDefault, IsActive, CreatedDate
                    FROM drs_customer_address_mst WITH (NOLOCK)
                    WHERE customer_id = @CustomerId AND IsDeleted = 0
                    ORDER BY IsDefault DESC, ID DESC";

                DataTable dt = _db.ExecuteQuery(
                    query,
                    new[] { new SqlParameter("@CustomerId", customerId) }
                );

                var addressList = new List<CustomerAddressResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    addressList.Add(
                        new CustomerAddressResponse
                        {
                            EncryptedAddressId = _encryption.Encrypt(Convert.ToInt64(row["ID"])),
                            AddressTitle = row["AddressTitle"].ToString() ?? "",
                            AddressType = row["AddressType"].ToString() ?? "",
                            FullName = row["FullName"].ToString() ?? "",
                            MobileNo = row["MobileNo"].ToString() ?? "",
                            AddressLine1 = row["AddressLine1"].ToString() ?? "",
                            AddressLine2 = row["AddressLine2"].ToString() ?? "",
                            Landmark = row["Landmark"].ToString() ?? "",
                            City = row["City"].ToString() ?? "",
                            State = row["State"].ToString() ?? "",
                            Country = row["Country"].ToString() ?? "",
                            Pincode = row["Pincode"].ToString() ?? "",
                            IsDefault = Convert.ToBoolean(row["IsDefault"]),
                            IsActive = Convert.ToBoolean(row["IsActive"]),
                            CreatedDate = DateTimeFormat.Format(
                                Convert.ToDateTime(row["CreatedDate"])
                            ),
                        }
                    );
                }

                sw.Stop();
                Logs.Info(
                    $"GetCustomerAddresses completed | CustomerId:{customerId} Count:{addressList.Count}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = addressList,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetCustomerAddresses API", ex);
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

        #region 3.5 CustomerAddressSoftDelete
        [HttpDelete("delete/{encryptedId?}")]
        [HttpPatch("delete/{encryptedId?}")]
        [HttpDelete("delete")]
        [HttpPatch("delete")]
        public async Task<IActionResult> CustomerAddressSoftDelete(
            [FromRoute] string? encryptedId = null,
            [FromQuery] string? encryptedIdQuery = null,
            [FromQuery] string? id = null
        )
        {
            Logs.Info("CustomerAddressSoftDelete API started");
            var sw = Stopwatch.StartNew();

            try
            {
                string? targetEncryptedId = !string.IsNullOrWhiteSpace(encryptedId)
                    ? encryptedId
                    : (!string.IsNullOrWhiteSpace(encryptedIdQuery) ? encryptedIdQuery : id);

                if (
                    string.IsNullOrWhiteSpace(targetEncryptedId)
                    || !_encryption.TryDecrypt(targetEncryptedId, out long addressId)
                    || addressId <= 0
                )
                {
                    sw.Stop();
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string> { "Encrypted address ID is required." },
                        }
                    );
                }

                long customerId = GetAuthenticatedUserId();

                string fetchQuery =
                    @"SELECT TOP 1 customer_id FROM drs_customer_address_mst WITH (NOLOCK) WHERE ID = @ID AND IsDeleted = 0";
                DataTable dt = _db.ExecuteQuery(
                    fetchQuery,
                    new[] { new SqlParameter("@ID", addressId) }
                );
                if (dt.Rows.Count == 0)
                {
                    sw.Stop();
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "ADDRESS_NOT_FOUND",
                            Errors = new List<string>
                            {
                                "Customer address record not found or already deleted.",
                            },
                        }
                    );
                }

                long ownerCustomerId = Convert.ToInt64(dt.Rows[0]["customer_id"]);
                if (ownerCustomerId != customerId && !IsCurrentUserAdmin())
                {
                    sw.Stop();
                    return StatusCode(
                        403,
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 403,
                            Message = "FORBIDDEN",
                            Errors = new List<string>
                            {
                                "You are not authorized to delete this address.",
                            },
                        }
                    );
                }

                string deleteQuery =
                    @"UPDATE drs_customer_address_mst SET IsDeleted = 1, IsActive = 0, ModifiedDate = GETDATE() WHERE ID = @ID";
                _db.ExecuteNonQuery(deleteQuery, new[] { new SqlParameter("@ID", addressId) });

                await _audit.InsertAuditAsync(
                    customerId,
                    "CustomerAddress",
                    "CustomerAddressSoftDelete",
                    "drs_customer_address_mst",
                    addressId,
                    System.Text.Json.JsonSerializer.Serialize(new { IsActive = 1, IsDeleted = 0 }),
                    System.Text.Json.JsonSerializer.Serialize(new { IsActive = 0, IsDeleted = 1 }),
                    GetClientIpAddress(),
                    GetUserAgent()
                );

                sw.Stop();
                Logs.Info($"CustomerAddressSoftDelete completed | AddressId:{addressId}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "ADDRESS_DELETED",
                        Data = null,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in CustomerAddressSoftDelete API", ex);
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
