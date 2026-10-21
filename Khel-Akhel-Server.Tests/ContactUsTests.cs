using System.ComponentModel.DataAnnotations;
using System.Data;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.BL.Services;
using Khel_Akhel_Server.Controllers.Contact;
using Khel_Akhel_Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit;
using static Khel_Akhel_Server.Models.CommonResponse;

namespace Khel_Akhel_Server.Tests
{
    public class ContactUsTests : IDisposable
    {
        private readonly string? _connectionString;
        private readonly DbHelper? _db;
        private readonly IUrlEncryptionService _encryption;
        private readonly IAuditService? _audit;
        private readonly List<long> _createdInquiryIds = new();

        public ContactUsTests()
        {
            _connectionString = CartAndWishlistDatabaseIntegrationTests.GetWorkingConnectionString();

            var inMemorySettings = new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", _connectionString ?? "Server=(localdb)\\MSSQLLocalDB;Database=KhelAkhe_CRM;Trusted_Connection=True;TrustServerCertificate=True;" },
                { "Security:UrlEncryptionKey", "TestUrlEncryptionKey2026_Secure32BytesLength!" },
                { "Jwt:Key", "TestJwtKeyForUnitTesting2026Secure32BytesKey!" }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _encryption = new UrlEncryptionService(configuration);

            if (_connectionString != null)
            {
                _db = new DbHelper(configuration);
                _audit = new AuditService(_db);

                // Ensure migration has been executed against test database
                ApplyMigrationIfMissing();
            }
        }

        private void ApplyMigrationIfMissing()
        {
            if (_connectionString == null) return;

            string migrationPath = @"F:\Projects\Khel-Akhel\DBScripts\Migration_ContactUs.sql";
            if (!File.Exists(migrationPath)) return;

            string script = File.ReadAllText(migrationPath);
            var batches = System.Text.RegularExpressions.Regex.Split(
                script,
                @"^\s*GO\s*$",
                System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            foreach (var batch in batches)
            {
                string trimmed = batch.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                using var cmd = new SqlCommand(trimmed, conn);
                cmd.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            if (_db == null || _createdInquiryIds.Count == 0) return;

            try
            {
                foreach (long id in _createdInquiryIds)
                {
                    _db.ExecuteNonQuery("DELETE FROM drs_contact_inquiry_mst WHERE ID = @Id", new[] { new SqlParameter("@Id", id) });
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        #region Validation Unit Tests
        [Fact]
        public void ModelValidation_ValidRequest_PassesValidation()
        {
            var model = new ContactInquiryCreateRequest
            {
                FullName = "Aarav Sharma",
                MobileNumber = "+919876543210",
                Email = "aarav@example.com",
                CompanyName = "Example Events",
                ServiceDescription = "I am looking for event design and decoration services."
            };

            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(model, context, results, true);

            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Fact]
        public void ModelValidation_OptionalCompanyNameOmitted_PassesValidation()
        {
            var model = new ContactInquiryCreateRequest
            {
                FullName = "Riya Patel",
                MobileNumber = "+919876543210",
                Email = "riya@example.com",
                CompanyName = null, // Optional
                ServiceDescription = "Inquiry regarding custom interior branding."
            };

            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(model, context, results, true);

            Assert.True(isValid);
            Assert.Null(model.CompanyName);
        }

        [Fact]
        public void ModelValidation_MissingFullName_FailsValidation()
        {
            var model = new ContactInquiryCreateRequest
            {
                FullName = "", // Missing
                MobileNumber = "+919876543210",
                Email = "aarav@example.com",
                ServiceDescription = "Looking for design services."
            };

            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(model, context, results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("FullName"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("123")]
        [InlineData("abcdefghij")]
        [InlineData("+123456789012345678901")] // > 20 chars
        public void MobileNumber_InvalidFormats_AreRejectedByValidator(string invalidMobile)
        {
            bool isValid = ValidationHelper.IsValidInternationalMobile(invalidMobile);
            Assert.False(isValid);
        }

        [Theory]
        [InlineData("+919876543210")]
        [InlineData("+1 (555) 123-4567")]
        [InlineData("020 7946 0999")]
        [InlineData("9876543210")]
        [InlineData("+44-20-7946-0999")]
        public void MobileNumber_ValidInternationalFormats_AreAccepted(string validMobile)
        {
            bool isValid = ValidationHelper.IsValidInternationalMobile(validMobile);
            Assert.True(isValid);
        }

        [Theory]
        [InlineData("plainaddress")]
        [InlineData("@missingusername.com")]
        [InlineData("missingdomain@")]
        [InlineData("missing.dot@domaincom")]
        [InlineData("two@@signs.com")]
        public void EmailAddress_InvalidFormats_AreRejected(string invalidEmail)
        {
            bool isValid = ValidationHelper.IsValidEmail(invalidEmail);
            Assert.False(isValid);
        }

        [Theory]
        [InlineData("user@example.com")]
        [InlineData("first.last@khelakhel.studio")]
        [InlineData("contact+tag@domain.co.in")]
        public void EmailAddress_ValidFormats_AreAccepted(string validEmail)
        {
            bool isValid = ValidationHelper.IsValidEmail(validEmail);
            Assert.True(isValid);
        }

        [Fact]
        public void ServiceDescription_Exactly300Characters_PassesValidation()
        {
            string exact300Chars = new string('A', 300);

            var model = new ContactInquiryCreateRequest
            {
                FullName = "Test User",
                MobileNumber = "+919876543210",
                Email = "test@example.com",
                ServiceDescription = exact300Chars
            };

            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(model, context, results, true);

            Assert.True(isValid);
            Assert.Equal(300, model.ServiceDescription.Length);
        }

        [Fact]
        public void ServiceDescription_MoreThan300Characters_FailsValidation()
        {
            string overlong301Chars = new string('A', 301);

            var model = new ContactInquiryCreateRequest
            {
                FullName = "Test User",
                MobileNumber = "+919876543210",
                Email = "test@example.com",
                ServiceDescription = overlong301Chars
            };

            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(model, context, results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("ServiceDescription"));
        }

        [Fact]
        public void ModelValidation_OverlongFields_AreRejected()
        {
            var model = new ContactInquiryCreateRequest
            {
                FullName = new string('A', 151), // > 150
                MobileNumber = "+919876543210",
                Email = new string('a', 250) + "@domain.com", // > 254
                CompanyName = new string('C', 201), // > 200
                ServiceDescription = "Valid description here."
            };

            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(model, context, results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("FullName"));
            Assert.Contains(results, r => r.MemberNames.Contains("CompanyName"));
            Assert.Contains(results, r => r.MemberNames.Contains("Email"));
        }
        #endregion

        #region Controller Unit Tests
        [Fact]
        public async Task Controller_NullRequest_ReturnsBadRequest()
        {
            var controller = new ContactUsController(null!, null!, null!)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            var result = await controller.SubmitInquiry(null) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            var response = result.Value as ApiResponse;
            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Equal("REQUEST_BODY_REQUIRED", response.Message);
        }

        [Fact]
        public async Task Controller_InvalidPayload_ReturnsValidationFailedWithDetails()
        {
            var controller = new ContactUsController(null!, null!, null!)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            var invalidRequest = new ContactInquiryCreateRequest
            {
                FullName = "", // Invalid
                MobileNumber = "invalid", // Invalid
                Email = "bad-email", // Invalid
                ServiceDescription = "123" // Too short (< 5)
            };

            var result = await controller.SubmitInquiry(invalidRequest) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            var response = result.Value as ApiResponse;
            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Equal("VALIDATION_FAILED", response.Message);
            Assert.NotNull(response.Errors);
            Assert.True(response.Errors.Count >= 4);
        }
        #endregion

        #region Real SQL Server Integration Tests
        [Fact]
        public async Task Integration_SubmitValidInquiry_InsertsIntoDatabase_AndReturnsEncryptedId()
        {
            Assert.NotNull(_db);
            Assert.NotNull(_audit);

            var controller = new ContactUsController(_db, _encryption, _audit)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            var request = new ContactInquiryCreateRequest
            {
                FullName = "Devendra Joshi",
                MobileNumber = "+91 98765 43210",
                Email = "devendra.joshi@example.com",
                CompanyName = "Joshi Design Studios",
                ServiceDescription = "We are interested in commissioned artwork for our office headquarters."
            };

            // Act: Submit inquiry
            var actionResult = await controller.SubmitInquiry(request) as ObjectResult;

            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);

            var response = actionResult.Value as ApiResponse;
            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("CONTACT_INQUIRY_SUBMITTED", response.Message);

            var submitResult = response.Data as ContactInquirySubmitResult;
            Assert.NotNull(submitResult);
            Assert.True(_encryption.TryDecrypt(submitResult.EncryptedInquiryId, out long inquiryId));
            Assert.True(inquiryId > 0);
            _createdInquiryIds.Add(inquiryId);

            // Assert: Verify row exists in real database with exact fields
            string query = @"
                SELECT FullName, MobileNo, Email, CompanyName, ServiceDescription, IsActive, IsDeleted, CreatedDate
                FROM drs_contact_inquiry_mst WITH (NOLOCK)
                WHERE ID = @Id";

            DataTable dt = await _db.ExecuteQueryAsync(query, new[] { new SqlParameter("@Id", inquiryId) });
            Assert.Single(dt.Rows);

            DataRow row = dt.Rows[0];
            Assert.Equal("Devendra Joshi", row["FullName"].ToString());
            Assert.Equal("+91 98765 43210", row["MobileNo"].ToString());
            Assert.Equal("devendra.joshi@example.com", row["Email"].ToString());
            Assert.Equal("Joshi Design Studios", row["CompanyName"].ToString());
            Assert.Equal("We are interested in commissioned artwork for our office headquarters.", row["ServiceDescription"].ToString());
            Assert.True(Convert.ToBoolean(row["IsActive"]));
            Assert.False(Convert.ToBoolean(row["IsDeleted"]));
        }

        [Fact]
        public async Task Integration_SubmitInquiry_WithSqlInjectionPayload_IsStoredSafelyAsData()
        {
            Assert.NotNull(_db);
            Assert.NotNull(_audit);

            var controller = new ContactUsController(_db, _encryption, _audit)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // SQL Injection attempt in text fields
            string injectionPayload = "'; DROP TABLE drs_contact_inquiry_mst; --";

            var request = new ContactInquiryCreateRequest
            {
                FullName = "Security Tester",
                MobileNumber = "+919876543210",
                Email = "security@example.com",
                CompanyName = injectionPayload,
                ServiceDescription = "Looking for security review services " + injectionPayload
            };

            var actionResult = await controller.SubmitInquiry(request) as ObjectResult;

            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);

            var response = actionResult.Value as ApiResponse;
            Assert.NotNull(response);
            Assert.True(response.Success);

            var submitResult = response.Data as ContactInquirySubmitResult;
            Assert.NotNull(submitResult);
            Assert.True(_encryption.TryDecrypt(submitResult.EncryptedInquiryId, out long inquiryId));
            Assert.True(inquiryId > 0);
            _createdInquiryIds.Add(inquiryId);

            // Verify table still exists and data was stored safely without executing
            string checkTableSql = "SELECT COUNT(1) FROM sys.tables WHERE name = 'drs_contact_inquiry_mst'";
            int tableCount = Convert.ToInt32(await _db.ExecuteScalarAsync(checkTableSql));
            Assert.Equal(1, tableCount);

            string query = "SELECT CompanyName FROM drs_contact_inquiry_mst WHERE ID = @Id";
            object? val = await _db.ExecuteScalarAsync(query, new[] { new SqlParameter("@Id", inquiryId) });
            Assert.Equal(injectionPayload, val?.ToString());
        }

        [Fact]
        public async Task Integration_Controller_EndToEndSubmission_ReturnsStandardApiResponse()
        {
            Assert.NotNull(_db);
            Assert.NotNull(_audit);

            var controller = new ContactUsController(_db, _encryption, _audit)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            var request = new ContactInquiryCreateRequest
            {
                FullName = "Maya Lin",
                MobileNumber = "+1-555-019-2834",
                Email = "maya.lin@sculpture.org",
                CompanyName = null, // Optional
                ServiceDescription = "Inquiry regarding architectural design collaboration."
            };

            var actionResult = await controller.SubmitInquiry(request) as ObjectResult;

            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);

            var response = actionResult.Value as ApiResponse;
            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("CONTACT_INQUIRY_SUBMITTED", response.Message);

            // Verify EncryptedInquiryId can be decrypted
            var submitResult = response.Data as ContactInquirySubmitResult;
            Assert.NotNull(submitResult);
            string encryptedId = submitResult.EncryptedInquiryId;
            Assert.False(string.IsNullOrWhiteSpace(encryptedId));
            Assert.True(_encryption.TryDecrypt(encryptedId, out long inquiryId));
            Assert.True(inquiryId > 0);
            _createdInquiryIds.Add(inquiryId);
        }
        #endregion
    }
}
