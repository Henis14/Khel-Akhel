using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.BL.Services;
using Khel_Akhel_Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using static Khel_Akhel_Server.Models.CommonResponse;

[ApiController]
[Route("api/faq")]
public class FAQController : ControllerBase
{
    private readonly DbHelper _db;
    private readonly IUrlEncryptionService _encryption;
    private readonly IAuditService _audit;

    public FAQController(DbHelper db, IUrlEncryptionService encryption, IAuditService audit)
    {
        _db = db;
        _encryption = encryption;
        _audit = audit;
    }

    #region Helper Methods
    private long GetAuthenticatedUserId()
    {
        var claim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);

        return claim != null && long.TryParse(claim.Value, out long userId) ? userId : 0;
    }

    private FaqResponse MapFaq(DataRow row)
    {
        return new FaqResponse
        {
            EncryptedFaqId = _encryption.Encrypt(Convert.ToInt64(row["ID"])),

            Category = row["Category"] == DBNull.Value ? null : row["Category"].ToString(),

            Question = row["Question"].ToString() ?? "",
            Answer = row["Answer"].ToString() ?? "",
            DisplayOrder = Convert.ToInt32(row["DisplayOrder"]),
            IsPublished = Convert.ToBoolean(row["IsPublished"]),
            IsActive = Convert.ToBoolean(row["IsActive"]),

            DisplayOnHomePage = Convert.ToBoolean(row["DisplayOnHomePage"]),
            DisplayOnFaqPage = Convert.ToBoolean(row["DisplayOnFaqPage"]),
            DisplayOnCheckoutPage = Convert.ToBoolean(row["DisplayOnCheckoutPage"]),
            DisplayOnContactPage = Convert.ToBoolean(row["DisplayOnContactPage"]),
            DisplayOnFooter = Convert.ToBoolean(row["DisplayOnFooter"]),

            CreatedDate =
                row["CreatedDate"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(row["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss"),

            ModifiedDate =
                row["ModifiedDate"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(row["ModifiedDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
        };
    }

    private static List<SqlParameter> GetFaqParameters(FaqCreateRequest request)
    {
        return new List<SqlParameter>
        {
            new("@Category", (object?)request.Category?.Trim() ?? DBNull.Value),
            new("@Question", request.Question.Trim()),
            new("@Answer", request.Answer.Trim()),
            new("@DisplayOrder", request.DisplayOrder),
            new("@IsPublished", request.IsPublished),
            new("@IsActive", request.IsActive),
            new("@DisplayOnHomePage", request.DisplayOnHomePage),
            new("@DisplayOnFaqPage", request.DisplayOnFaqPage),
            new("@DisplayOnCheckoutPage", request.DisplayOnCheckoutPage),
            new("@DisplayOnContactPage", request.DisplayOnContactPage),
            new("@DisplayOnFooter", request.DisplayOnFooter),
        };
    }
    #endregion

    #region Create FAQ API
    [Authorize(Roles = "Admin")]
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] FaqCreateRequest? request)
    {
        Logs.Info("FAQ Create API started");
        try
        {
            if (request == null)
            {
                Logs.Warning("FAQ Create rejected | Request body is null");
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "REQUEST_BODY_REQUIRED",
                    }
                );
            }

            request.Question = request.Question?.Trim() ?? "";
            request.Answer = request.Answer?.Trim() ?? "";

            if (
                request.Question.Length < 3
                || request.Question.Length > 500
                || string.IsNullOrWhiteSpace(request.Answer)
                || request.DisplayOrder < 0
            )
            {
                Logs.Warning("FAQ Create validation failed | Invalid question, answer, or display order");
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "VALIDATION_FAILED",
                        Errors = new List<string>
                        {
                            "Question, answer, and display order are invalid.",
                        },
                    }
                );
            }

            const string sql =
                @"
                INSERT INTO dbo.drs_faq_mst
                (
                    Category, Question, Answer, DisplayOrder,
                    IsPublished, IsActive,
                    DisplayOnHomePage, DisplayOnFaqPage,
                    DisplayOnCheckoutPage, DisplayOnContactPage,
                    DisplayOnFooter, IsDeleted, CreatedDate
                )
                OUTPUT INSERTED.ID
                VALUES
                (
                    @Category, @Question, @Answer, @DisplayOrder,
                    @IsPublished, @IsActive,
                    @DisplayOnHomePage, @DisplayOnFaqPage,
                    @DisplayOnCheckoutPage, @DisplayOnContactPage,
                    @DisplayOnFooter, 0, GETDATE()
                );";

            long id = Convert.ToInt64(_db.ExecuteScalar(sql, GetFaqParameters(request).ToArray()));

            await _audit.InsertAuditAsync(
                GetAuthenticatedUserId(),
                "FAQ",
                "Create",
                "drs_faq_mst",
                id,
                null,
                JsonSerializer.Serialize(request),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString()
            );

            Logs.Info($"FAQ created successfully | FaqId:{id}");
            return Ok(
                new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "FAQ_CREATED",
                    Data = new { EncryptedFaqId = _encryption.Encrypt(id) },
                }
            );
        }
        catch (Exception ex)
        {
            Logs.Error("FAQ Create API failed", ex);

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

    #region Get Admin FAQ List API
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/list")]
    public IActionResult AdminList(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isPublished = null
    )
    {
        Logs.Info($"FAQ AdminList API started | Search:{search} IsActive:{isActive} IsPublished:{isPublished}");
        try
        {
            var conditions = new List<string> { "IsDeleted = 0" };

            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                conditions.Add(
                    "(Question LIKE @Search OR Answer LIKE @Search " + "OR Category LIKE @Search)"
                );

                parameters.Add(new SqlParameter("@Search", $"%{search.Trim()}%"));
            }

            if (isActive.HasValue)
            {
                conditions.Add("IsActive = @IsActive");
                parameters.Add(new SqlParameter("@IsActive", isActive.Value));
            }

            if (isPublished.HasValue)
            {
                conditions.Add("IsPublished = @IsPublished");
                parameters.Add(new SqlParameter("@IsPublished", isPublished.Value));
            }

            string sql =
                $@"
                SELECT *
                FROM dbo.drs_faq_mst
                WHERE {string.Join(" AND ", conditions)}
                ORDER BY DisplayOrder ASC, ID DESC;";

            DataTable table = _db.ExecuteQuery(sql, parameters.ToArray());

            var items = table.AsEnumerable().Select(row => MapFaq(row)).ToList();

            Logs.Info($"FAQ AdminList completed successfully | Count:{items.Count}");
            return Ok(
                new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = items,
                }
            );
        }
        catch (Exception ex)
        {
            Logs.Error("FAQ AdminList API failed", ex);

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

    #region Get FAQ By Id API
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/{encryptedFaqId}")]
    public IActionResult GetById(string encryptedFaqId)
    {
        Logs.Info($"FAQ GetById API started | EncryptedFaqId:{encryptedFaqId}");
        try
        {
            if (!_encryption.TryDecrypt(encryptedFaqId, out long id) || id <= 0)
            {
                Logs.Warning($"FAQ GetById rejected | Invalid EncryptedFaqId:{encryptedFaqId}");
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_FAQ_ID",
                    }
                );
            }

            const string sql =
                @"
                SELECT *
                FROM dbo.drs_faq_mst
                WHERE ID = @ID AND IsDeleted = 0;";

            DataTable table = _db.ExecuteQuery(sql, new[] { new SqlParameter("@ID", id) });

            if (table.Rows.Count == 0)
            {
                Logs.Warning($"FAQ GetById failed | FAQ not found | FaqId:{id}");
                return NotFound(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "FAQ_NOT_FOUND",
                    }
                );
            }

            Logs.Info($"FAQ GetById completed successfully | FaqId:{id}");
            return Ok(
                new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = MapFaq(table.Rows[0]),
                }
            );
        }
        catch (Exception ex)
        {
            Logs.Error("FAQ GetById API failed", ex);

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

    #region Update FAQ API
    [Authorize(Roles = "Admin")]
    [HttpPatch("update")]
    public async Task<IActionResult> Update([FromBody] FaqUpdateRequest? request)
    {
        Logs.Info("FAQ Update API started");
        try
        {
            if (
                request == null
                || !_encryption.TryDecrypt(request.EncryptedFaqId, out long id)
                || id <= 0
            )
            {
                Logs.Warning($"FAQ Update rejected | Invalid request or EncryptedFaqId:{request?.EncryptedFaqId}");
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_REQUEST",
                    }
                );
            }

            request.Question = request.Question?.Trim() ?? "";
            request.Answer = request.Answer?.Trim() ?? "";

            if (
                request.Question.Length < 3
                || request.Question.Length > 500
                || string.IsNullOrWhiteSpace(request.Answer)
                || request.DisplayOrder < 0
            )
            {
                Logs.Warning($"FAQ Update validation failed | FaqId:{id}");
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "VALIDATION_FAILED",
                    }
                );
            }

            const string sql =
                @"
                UPDATE dbo.drs_faq_mst
                SET Category = @Category,
                    Question = @Question,
                    Answer = @Answer,
                    DisplayOrder = @DisplayOrder,
                    IsPublished = @IsPublished,
                    IsActive = @IsActive,
                    DisplayOnHomePage = @DisplayOnHomePage,
                    DisplayOnFaqPage = @DisplayOnFaqPage,
                    DisplayOnCheckoutPage = @DisplayOnCheckoutPage,
                    DisplayOnContactPage = @DisplayOnContactPage,
                    DisplayOnFooter = @DisplayOnFooter,
                    ModifiedDate = GETDATE()
                WHERE ID = @ID AND IsDeleted = 0;

                SELECT @@ROWCOUNT;";

            var parameters = GetFaqParameters(request);
            parameters.Add(new SqlParameter("@ID", id));

            int affected = Convert.ToInt32(_db.ExecuteScalar(sql, parameters.ToArray()) ?? 0);

            if (affected == 0)
            {
                Logs.Warning($"FAQ Update failed | FAQ not found | FaqId:{id}");
                return NotFound(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "FAQ_NOT_FOUND",
                    }
                );
            }

            await _audit.InsertAuditAsync(
                GetAuthenticatedUserId(),
                "FAQ",
                "Update",
                "drs_faq_mst",
                id,
                null,
                JsonSerializer.Serialize(request),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString()
            );

            Logs.Info($"FAQ updated successfully | FaqId:{id}");
            return Ok(
                new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "FAQ_UPDATED",
                }
            );
        }
        catch (Exception ex)
        {
            Logs.Error("FAQ Update API failed", ex);

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

    #region Delete FAQ API
    [Authorize(Roles = "Admin")]
    [HttpDelete("delete/{encryptedFaqId}")]
    public async Task<IActionResult> Delete(string encryptedFaqId)
    {
        Logs.Info($"FAQ Delete API started | EncryptedFaqId:{encryptedFaqId}");
        try
        {
            if (!_encryption.TryDecrypt(encryptedFaqId, out long id) || id <= 0)
            {
                Logs.Warning($"FAQ Delete rejected | Invalid EncryptedFaqId:{encryptedFaqId}");
                return BadRequest(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "INVALID_FAQ_ID",
                    }
                );
            }

            const string sql =
                @"
                UPDATE dbo.drs_faq_mst
                SET IsDeleted = 1,
                    IsActive = 0,
                    ModifiedDate = GETDATE()
                WHERE ID = @ID AND IsDeleted = 0;

                SELECT @@ROWCOUNT;";

            int affected = Convert.ToInt32(
                _db.ExecuteScalar(sql, new[] { new SqlParameter("@ID", id) }) ?? 0
            );

            if (affected == 0)
            {
                Logs.Warning($"FAQ Delete failed | FAQ not found | FaqId:{id}");
                return NotFound(
                    new ApiResponse
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "FAQ_NOT_FOUND",
                    }
                );
            }

            await _audit.InsertAuditAsync(
                GetAuthenticatedUserId(),
                "FAQ",
                "Delete",
                "drs_faq_mst",
                id,
                null,
                JsonSerializer.Serialize(new { IsDeleted = true, IsActive = false }),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString()
            );

            Logs.Info($"FAQ deleted successfully | FaqId:{id}");
            return Ok(
                new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "FAQ_DELETED",
                }
            );
        }
        catch (Exception ex)
        {
            Logs.Error("FAQ Delete API failed", ex);

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

    #region Get Public FAQs API
    [AllowAnonymous]
    [HttpGet("public")]
    public IActionResult GetPublicFaqs([FromQuery] string location = "faq")
    {
        Logs.Info($"FAQ GetPublicFaqs API started | Location:{location}");
        // Column names are selected from this fixed allowlist.
        string? displayColumn = location.Trim().ToLowerInvariant() switch
        {
            "home" => "DisplayOnHomePage",
            "faq" => "DisplayOnFaqPage",
            "checkout" => "DisplayOnCheckoutPage",
            "contact" => "DisplayOnContactPage",
            "footer" => "DisplayOnFooter",
            _ => null,
        };

        if (displayColumn == null)
        {
            Logs.Warning($"FAQ GetPublicFaqs rejected | Invalid location:{location}");
            return BadRequest(
                new ApiResponse
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "INVALID_FAQ_LOCATION",
                    Errors = new List<string> { "Use home, faq, checkout, contact, or footer." },
                }
            );
        }

        try
        {
            string sql =
                $@"
                SELECT *
                FROM dbo.drs_faq_mst
                WHERE IsDeleted = 0
                  AND IsActive = 1
                  AND IsPublished = 1
                  AND {displayColumn} = 1
                ORDER BY DisplayOrder ASC, ID DESC;";

            DataTable table = _db.ExecuteQuery(sql, Array.Empty<SqlParameter>());

            var items = table.AsEnumerable().Select(row => MapFaq(row)).ToList();

            Logs.Info($"FAQ GetPublicFaqs completed successfully | Location:{location} Count:{items.Count}");
            return Ok(
                new ApiResponse
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = items,
                }
            );
        }
        catch (Exception ex)
        {
            Logs.Error("FAQ Public API failed", ex);

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
