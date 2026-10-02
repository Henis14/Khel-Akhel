using System.Data;
using System.Diagnostics;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.Models.LocationMaster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using static Khel_Akhel_Server.Models.CommonResponse;

namespace Khel_Akhel_Server.Controllers.LocationMaster
{
    [ApiController]
    [Route("api/location")]
    [AllowAnonymous]
    public class LocationMasterController : ControllerBase
    {
        private readonly DbHelper _db;

        public LocationMasterController(DbHelper db)
        {
            _db = db;
        }

        #region 1. Get Active Countries [GET /api/location/countries]
        [HttpGet("countries")]
        public IActionResult GetCountries()
        {
            Logs.Info("GetCountries API started");
            var sw = Stopwatch.StartNew();

            try
            {
                string query =
                    @"
                    SELECT ID, CountryName, CountryCode, ISNULL(PhoneCode, '') AS PhoneCode
                    FROM dbo.drs_country_mst WITH (NOLOCK)
                    WHERE IsActive = 1 AND IsDeleted = 0
                    ORDER BY CountryName ASC";

                DataTable dt = _db.ExecuteQuery(query);

                var countries = new List<CountryResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    countries.Add(
                        new CountryResponse
                        {
                            Id = Convert.ToInt64(row["ID"]),
                            CountryName = Convert.ToString(row["CountryName"]) ?? string.Empty,
                            CountryCode = Convert.ToString(row["CountryCode"]) ?? string.Empty,
                            PhoneCode = Convert.ToString(row["PhoneCode"]) ?? string.Empty,
                        }
                    );
                }

                sw.Stop();
                Logs.Info($"GetCountries completed successfully | Total:{countries.Count}");

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = countries,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetCountries API", ex);
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

        #region 2. Get States By Country [GET /api/location/states?countryId=1]
        [HttpGet("states")]
        public IActionResult GetStates([FromQuery] long countryId)
        {
            Logs.Info($"GetStates API started | CountryId:{countryId}");
            var sw = Stopwatch.StartNew();

            try
            {
                if (countryId <= 0)
                {
                    sw.Stop();
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_REQUEST",
                            Errors = new List<string> { "Valid countryId is required." },
                        }
                    );
                }

                string query =
                    @"
                    SELECT ID, country_id, StateName, ISNULL(StateCode, '') AS StateCode
                    FROM dbo.drs_state_mst WITH (NOLOCK)
                    WHERE country_id = @CountryId AND IsActive = 1 AND IsDeleted = 0
                    ORDER BY StateName ASC";

                DataTable dt = _db.ExecuteQuery(
                    query,
                    new[] { new SqlParameter("@CountryId", countryId) }
                );

                var states = new List<StateResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    states.Add(
                        new StateResponse
                        {
                            Id = Convert.ToInt64(row["ID"]),
                            CountryId = Convert.ToInt64(row["country_id"]),
                            StateName = Convert.ToString(row["StateName"]) ?? string.Empty,
                            StateCode = Convert.ToString(row["StateCode"]) ?? string.Empty,
                        }
                    );
                }

                sw.Stop();
                Logs.Info(
                    $"GetStates completed successfully | CountryId:{countryId} Total:{states.Count}"
                );

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = states,
                    }
                );
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Exception occurred in GetStates API", ex);
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

        #region 3. Get Country By ID [GET /api/location/countries/{id}]
        [HttpGet("countries/{id:long}")]
        public IActionResult GetCountryById([FromRoute] long id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_ID",
                        }
                    );

                string query =
                    @"
                    SELECT ID, CountryName, CountryCode, ISNULL(PhoneCode, '') AS PhoneCode
                    FROM dbo.drs_country_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(query, new[] { new SqlParameter("@ID", id) });

                if (dt.Rows.Count == 0)
                {
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "COUNTRY_NOT_FOUND",
                        }
                    );
                }

                DataRow row = dt.Rows[0];
                var country = new CountryResponse
                {
                    Id = Convert.ToInt64(row["ID"]),
                    CountryName = Convert.ToString(row["CountryName"]) ?? string.Empty,
                    CountryCode = Convert.ToString(row["CountryCode"]) ?? string.Empty,
                    PhoneCode = Convert.ToString(row["PhoneCode"]) ?? string.Empty,
                };

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = country,
                    }
                );
            }
            catch (Exception ex)
            {
                Logs.Error("Exception in GetCountryById API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                    }
                );
            }
        }
        #endregion

        #region 4. Get State By ID [GET /api/location/states/{id}]
        [HttpGet("states/{id:long}")]
        public IActionResult GetStateById([FromRoute] long id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 400,
                            Message = "INVALID_ID",
                        }
                    );

                string query =
                    @"
                    SELECT ID, country_id, StateName, ISNULL(StateCode, '') AS StateCode
                    FROM dbo.drs_state_mst WITH (NOLOCK)
                    WHERE ID = @ID AND IsDeleted = 0";

                DataTable dt = _db.ExecuteQuery(query, new[] { new SqlParameter("@ID", id) });

                if (dt.Rows.Count == 0)
                {
                    return NotFound(
                        new ApiResponse
                        {
                            Success = false,
                            StatusCode = 404,
                            Message = "STATE_NOT_FOUND",
                        }
                    );
                }

                DataRow row = dt.Rows[0];
                var state = new StateResponse
                {
                    Id = Convert.ToInt64(row["ID"]),
                    CountryId = Convert.ToInt64(row["country_id"]),
                    StateName = Convert.ToString(row["StateName"]) ?? string.Empty,
                    StateCode = Convert.ToString(row["StateCode"]) ?? string.Empty,
                };

                return Ok(
                    new ApiResponse
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "SUCCESS",
                        Data = state,
                    }
                );
            }
            catch (Exception ex)
            {
                Logs.Error("Exception in GetStateById API", ex);
                return StatusCode(
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = "SERVER_ERROR",
                    }
                );
            }
        }
        #endregion
    }
}
