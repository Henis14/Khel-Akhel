using System.Data;
using System.Security.Cryptography;
using System.Text;
using Khel_Akhel_Server.BL.Common;
using Microsoft.Data.SqlClient;

namespace Khel_Akhel_Server.BL.Services
{
    public class IssuedRefreshToken
    {
        public string Value { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
    }

    public class TokenRotationResult
    {
        public bool Success { get; set; }
        public string? FailureReason { get; set; }
        public IssuedRefreshToken? Token { get; set; }
        public long CustomerId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public interface IRefreshTokenService
    {
        IssuedRefreshToken Issue(long customerId, string? ipAddress, string? userAgent);
        TokenRotationResult Rotate(string rawToken, string? ipAddress, string? userAgent);
        void Revoke(string rawToken, string? ipAddress);
        void RevokeAll(long customerId, string? ipAddress);
    }

    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly DbHelper _db;

        public RefreshTokenService(DbHelper db)
        {
            _db = db;
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        public IssuedRefreshToken Issue(long customerId, string? ipAddress, string? userAgent)
        {
            string rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            string tokenHash = HashToken(rawToken);
            DateTime expiresUtc = DateTime.UtcNow.AddDays(7);

            string query = @"
                INSERT INTO drs_refresh_token_mst
                (customer_id, TokenHash, CreatedUtc, ExpiresUtc, CreatedByIp, DeviceName, IsActive, IsDeleted, CreatedDate)
                VALUES
                (@CustomerId, @TokenHash, GETUTCDATE(), @ExpiresUtc, @CreatedByIp, @DeviceName, 1, 0, GETDATE())";

            _db.ExecuteNonQuery(query, new[]
            {
                new SqlParameter("@CustomerId", customerId),
                new SqlParameter("@TokenHash", tokenHash),
                new SqlParameter("@ExpiresUtc", expiresUtc),
                new SqlParameter("@CreatedByIp", (object?)ipAddress ?? DBNull.Value),
                new SqlParameter("@DeviceName", (object?)userAgent ?? DBNull.Value)
            });

            return new IssuedRefreshToken
            {
                Value = rawToken,
                ExpiresUtc = expiresUtc
            };
        }

        public TokenRotationResult Rotate(string rawToken, string? ipAddress, string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return new TokenRotationResult { Success = false, FailureReason = "TOKEN_MISSING" };
            }

            string tokenHash = HashToken(rawToken);

            string selectQuery = @"
                SELECT TOP 1 r.ID, r.customer_id, r.ExpiresUtc, r.RevokedUtc, r.IsActive, c.Email, c.IsAdmin, c.IsActive AS CustomerActive
                FROM drs_refresh_token_mst r WITH (NOLOCK)
                INNER JOIN drs_customer_mst c WITH (NOLOCK) ON c.ID = r.customer_id AND c.IsDeleted = 0
                WHERE r.TokenHash = @TokenHash AND r.IsDeleted = 0";

            DataTable dt = _db.ExecuteQuery(selectQuery, new[] { new SqlParameter("@TokenHash", tokenHash) });
            if (dt.Rows.Count == 0)
            {
                return new TokenRotationResult { Success = false, FailureReason = "TOKEN_NOT_FOUND" };
            }

            DataRow row = dt.Rows[0];
            long customerId = Convert.ToInt64(row["customer_id"]);
            string email = row["Email"].ToString() ?? "";
            bool isAdmin = Convert.ToBoolean(row["IsAdmin"]);
            bool isCustomerActive = Convert.ToBoolean(row["CustomerActive"]);
            bool isTokenActive = Convert.ToBoolean(row["IsActive"]);
            DateTime expiresUtc = Convert.ToDateTime(row["ExpiresUtc"]);
            bool isRevoked = row["RevokedUtc"] != DBNull.Value;

            if (!isCustomerActive)
            {
                return new TokenRotationResult { Success = false, FailureReason = "CUSTOMER_DISABLED", CustomerId = customerId };
            }

            if (isRevoked || !isTokenActive)
            {
                // Reuse detection: Revoke all tokens for user if revoked token is presented
                RevokeAll(customerId, ipAddress);
                return new TokenRotationResult { Success = false, FailureReason = "TOKEN_REVOKED_REUSE_DETECTED", CustomerId = customerId };
            }

            if (expiresUtc <= DateTime.UtcNow)
            {
                return new TokenRotationResult { Success = false, FailureReason = "TOKEN_EXPIRED", CustomerId = customerId };
            }

            // Issue new token and revoke old token
            IssuedRefreshToken newToken = Issue(customerId, ipAddress, userAgent);
            string newTokenHash = HashToken(newToken.Value);

            string revokeOldQuery = @"
                UPDATE drs_refresh_token_mst
                SET RevokedUtc = GETUTCDATE(),
                    RevokedByIp = @RevokedByIp,
                    ReplacedByTokenHash = @ReplacedByTokenHash,
                    IsActive = 0,
                    ModifiedDate = GETDATE()
                WHERE TokenHash = @TokenHash";

            _db.ExecuteNonQuery(revokeOldQuery, new[]
            {
                new SqlParameter("@RevokedByIp", (object?)ipAddress ?? DBNull.Value),
                new SqlParameter("@ReplacedByTokenHash", newTokenHash),
                new SqlParameter("@TokenHash", tokenHash)
            });

            return new TokenRotationResult
            {
                Success = true,
                Token = newToken,
                CustomerId = customerId,
                Email = email,
                Role = isAdmin ? "Admin" : "Customer"
            };
        }

        public void Revoke(string rawToken, string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return;

            string tokenHash = HashToken(rawToken);

            string query = @"
                UPDATE drs_refresh_token_mst
                SET RevokedUtc = GETUTCDATE(),
                    RevokedByIp = @RevokedByIp,
                    IsActive = 0,
                    ModifiedDate = GETDATE()
                WHERE TokenHash = @TokenHash AND RevokedUtc IS NULL";

            _db.ExecuteNonQuery(query, new[]
            {
                new SqlParameter("@RevokedByIp", (object?)ipAddress ?? DBNull.Value),
                new SqlParameter("@TokenHash", tokenHash)
            });
        }

        public void RevokeAll(long customerId, string? ipAddress)
        {
            string query = @"
                UPDATE drs_refresh_token_mst
                SET RevokedUtc = GETUTCDATE(),
                    RevokedByIp = @RevokedByIp,
                    IsActive = 0,
                    ModifiedDate = GETDATE()
                WHERE customer_id = @CustomerId AND RevokedUtc IS NULL";

            _db.ExecuteNonQuery(query, new[]
            {
                new SqlParameter("@RevokedByIp", (object?)ipAddress ?? DBNull.Value),
                new SqlParameter("@CustomerId", customerId)
            });
        }
    }
}
