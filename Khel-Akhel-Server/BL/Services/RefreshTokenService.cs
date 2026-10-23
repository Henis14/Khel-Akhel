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
        Task<IssuedRefreshToken> IssueAsync(long customerId, string? ipAddress, string? userAgent);
        Task<TokenRotationResult> RotateAsync(
            string rawToken,
            string? ipAddress,
            string? userAgent
        );
        Task RevokeAsync(string rawToken, string? ipAddress);
        Task RevokeAllAsync(long customerId, string? ipAddress);

        IssuedRefreshToken Issue(long customerId, string? ipAddress, string? userAgent);
        TokenRotationResult Rotate(string rawToken, string? ipAddress, string? userAgent);
        void Revoke(string rawToken, string? ipAddress);
        void RevokeAll(long customerId, string? ipAddress);
    }

    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly DbHelper _db;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _audit;

        public RefreshTokenService(DbHelper db, IConfiguration configuration, IAuditService audit)
        {
            _db = db;
            _configuration = configuration;
            _audit = audit;
        }

        public static string HashToken(string token)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        public async Task<IssuedRefreshToken> IssueAsync(
            long customerId,
            string? ipAddress,
            string? userAgent
        )
        {
            string rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            string tokenHash = HashToken(rawToken);
            DateTime expiresUtc = DateTime.UtcNow.AddDays(7);

            string query =
                @"
                INSERT INTO drs_refresh_token_mst
                (customer_id, TokenHash, CreatedUtc, ExpiresUtc, CreatedByIp, DeviceName, IsActive, IsDeleted, CreatedDate)
                VALUES
                (@CustomerId, @TokenHash, GETUTCDATE(), @ExpiresUtc, @CreatedByIp, @DeviceName, 1, 0, GETDATE())";

            using var con = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            cmd.Parameters.AddWithValue("@TokenHash", tokenHash);
            cmd.Parameters.AddWithValue("@ExpiresUtc", expiresUtc);
            cmd.Parameters.AddWithValue("@CreatedByIp", (object?)ipAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DeviceName", (object?)userAgent ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();

            return new IssuedRefreshToken { Value = rawToken, ExpiresUtc = expiresUtc };
        }

        public async Task<TokenRotationResult> RotateAsync(
            string rawToken,
            string? ipAddress,
            string? userAgent
        )
        {
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return new TokenRotationResult { Success = false, FailureReason = "TOKEN_MISSING" };
            }

            string tokenHash = HashToken(rawToken);

            // Pre-generate the new raw token and its hash before the claim query
            string newRawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            string newTokenHash = HashToken(newRawToken);
            DateTime newExpiresUtc = DateTime.UtcNow.AddDays(7);

            using var con = await _db.GetOpenConnectionAsync();
            using var tx = con.BeginTransaction(IsolationLevel.ReadCommitted);

            try
            {
                // Atomic token claim
                string claimQuery =
                    @"
                    UPDATE drs_refresh_token_mst
                    SET RevokedUtc = GETUTCDATE(),
                        RevokedByIp = @Ip,
                        IsActive = 0,
                        ReplacedByTokenHash = @NewHash,
                        ModifiedDate = GETDATE()
                    OUTPUT inserted.customer_id
                    WHERE TokenHash = @TokenHash
                      AND RevokedUtc IS NULL
                      AND IsActive = 1
                      AND IsDeleted = 0
                      AND ExpiresUtc > GETUTCDATE();";

                using var claimCmd = new SqlCommand(claimQuery, con, tx);
                claimCmd.Parameters.AddWithValue("@Ip", (object?)ipAddress ?? DBNull.Value);
                claimCmd.Parameters.AddWithValue("@NewHash", newTokenHash);
                claimCmd.Parameters.AddWithValue("@TokenHash", tokenHash);

                object? claimResult = await claimCmd.ExecuteScalarAsync();

                if (claimResult != null && claimResult != DBNull.Value)
                {
                    long customerId = Convert.ToInt64(claimResult);

                    // Insert the new token row in the same transaction
                    string insertQuery =
                        @"
                        INSERT INTO drs_refresh_token_mst
                        (customer_id, TokenHash, CreatedUtc, ExpiresUtc, CreatedByIp, DeviceName, IsActive, IsDeleted, CreatedDate)
                        VALUES
                        (@CustomerId, @TokenHash, GETUTCDATE(), @ExpiresUtc, @CreatedByIp, @DeviceName, 1, 0, GETDATE())";

                    using var insertCmd = new SqlCommand(insertQuery, con, tx);
                    insertCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    insertCmd.Parameters.AddWithValue("@TokenHash", newTokenHash);
                    insertCmd.Parameters.AddWithValue("@ExpiresUtc", newExpiresUtc);
                    insertCmd.Parameters.AddWithValue(
                        "@CreatedByIp",
                        (object?)ipAddress ?? DBNull.Value
                    );
                    insertCmd.Parameters.AddWithValue(
                        "@DeviceName",
                        (object?)userAgent ?? DBNull.Value
                    );
                    await insertCmd.ExecuteNonQueryAsync();

                    // Load customer inside the same transaction without NOLOCK
                    string custQuery =
                        @"
                        SELECT Email, IsAdmin, IsActive
                        FROM drs_customer_mst
                        WHERE ID = @CustomerId AND IsDeleted = 0";

                    using var custCmd = new SqlCommand(custQuery, con, tx);
                    custCmd.Parameters.AddWithValue("@CustomerId", customerId);

                    using var reader = await custCmd.ExecuteReaderAsync();
                    if (!reader.Read())
                    {
                        await reader.CloseAsync();
                        await tx.RollbackAsync();
                        return new TokenRotationResult
                        {
                            Success = false,
                            FailureReason = "CUSTOMER_NOT_FOUND",
                            CustomerId = customerId,
                        };
                    }

                    bool isCustomerActive = Convert.ToBoolean(reader["IsActive"]);
                    if (!isCustomerActive)
                    {
                        await reader.CloseAsync();
                        await tx.RollbackAsync();
                        return new TokenRotationResult
                        {
                            Success = false,
                            FailureReason = "CUSTOMER_DISABLED",
                            CustomerId = customerId,
                        };
                    }

                    string email = reader["Email"]?.ToString() ?? "";
                    bool isAdmin = Convert.ToBoolean(reader["IsAdmin"]);
                    await reader.CloseAsync();

                    await tx.CommitAsync();

                    return new TokenRotationResult
                    {
                        Success = true,
                        Token = new IssuedRefreshToken
                        {
                            Value = newRawToken,
                            ExpiresUtc = newExpiresUtc,
                        },
                        CustomerId = customerId,
                        Email = email,
                        Role = isAdmin ? "Admin" : "Customer",
                    };
                }

                // If claim affected 0 rows, rollback and classify the failure
                await tx.RollbackAsync();
            }
            catch
            {
                try
                {
                    await tx.RollbackAsync();
                }
                catch { }
                throw;
            }

            // Read the token row without NOLOCK to classify the failure
            string inspectQuery =
                @"
                SELECT TOP 1 customer_id, ExpiresUtc, RevokedUtc, ReplacedByTokenHash, IsActive
                FROM drs_refresh_token_mst
                WHERE TokenHash = @TokenHash AND IsDeleted = 0";

            using var inspectCmd = new SqlCommand(inspectQuery, con);
            inspectCmd.Parameters.AddWithValue("@TokenHash", tokenHash);

            using var inspectReader = await inspectCmd.ExecuteReaderAsync();
            if (!inspectReader.Read())
            {
                return new TokenRotationResult
                {
                    Success = false,
                    FailureReason = "TOKEN_NOT_FOUND",
                };
            }

            long targetCustomerId = Convert.ToInt64(inspectReader["customer_id"]);
            DateTime tokenExpiresUtc = Convert.ToDateTime(inspectReader["ExpiresUtc"]);
            bool isRevoked = inspectReader["RevokedUtc"] != DBNull.Value;
            DateTime? revokedUtc = isRevoked
                ? Convert.ToDateTime(inspectReader["RevokedUtc"])
                : null;
            string? replacedByTokenHash =
                inspectReader["ReplacedByTokenHash"] != DBNull.Value
                    ? inspectReader["ReplacedByTokenHash"]?.ToString()
                    : null;

            await inspectReader.CloseAsync();

            if (tokenExpiresUtc <= DateTime.UtcNow)
            {
                return new TokenRotationResult
                {
                    Success = false,
                    FailureReason = "TOKEN_EXPIRED",
                    CustomerId = targetCustomerId,
                };
            }

            if (isRevoked && revokedUtc.HasValue)
            {
                double graceSeconds = _configuration.GetValue<double>(
                    "Security:RefreshReuseGraceSeconds",
                    30.0
                );
                TimeSpan timeSinceRevocation = DateTime.UtcNow - revokedUtc.Value;

                if (
                    timeSinceRevocation.TotalSeconds <= graceSeconds
                    && !string.IsNullOrEmpty(replacedByTokenHash)
                )
                {
                    // Benign concurrent refresh race: do NOT revoke family
                    return new TokenRotationResult
                    {
                        Success = false,
                        FailureReason = "REFRESH_RACE",
                        CustomerId = targetCustomerId,
                    };
                }

                // Stale reuse detected: revoke all active tokens for customer
                await RevokeAllAsync(targetCustomerId, ipAddress);
                await _audit.InsertAuditAsync(
                    customerId: targetCustomerId,
                    moduleName: "Auth",
                    action: "RefreshTokenReuseDetected",
                    tableName: "drs_refresh_token_mst",
                    entityId: targetCustomerId,
                    oldValue: null,
                    newValue: $"Token reuse detected for token revoked {timeSinceRevocation.TotalSeconds:F1}s ago",
                    ipAddress: ipAddress,
                    userAgent: userAgent
                );

                return new TokenRotationResult
                {
                    Success = false,
                    FailureReason = "TOKEN_REVOKED_REUSE_DETECTED",
                    CustomerId = targetCustomerId,
                };
            }

            return new TokenRotationResult
            {
                Success = false,
                FailureReason = "TOKEN_NOT_FOUND",
                CustomerId = targetCustomerId,
            };
        }

        public async Task RevokeAsync(string rawToken, string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return;

            string tokenHash = HashToken(rawToken);

            string query =
                @"
                UPDATE drs_refresh_token_mst
                SET RevokedUtc = GETUTCDATE(),
                    RevokedByIp = @RevokedByIp,
                    IsActive = 0,
                    ModifiedDate = GETDATE()
                WHERE TokenHash = @TokenHash AND RevokedUtc IS NULL";

            using var con = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@RevokedByIp", (object?)ipAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TokenHash", tokenHash);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task RevokeAllAsync(long customerId, string? ipAddress)
        {
            string query =
                @"
                UPDATE drs_refresh_token_mst
                SET RevokedUtc = GETUTCDATE(),
                    RevokedByIp = @RevokedByIp,
                    IsActive = 0,
                    ModifiedDate = GETDATE()
                WHERE customer_id = @CustomerId AND RevokedUtc IS NULL";

            using var con = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@RevokedByIp", (object?)ipAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            await cmd.ExecuteNonQueryAsync();
        }

        public IssuedRefreshToken Issue(long customerId, string? ipAddress, string? userAgent)
        {
            return IssueAsync(customerId, ipAddress, userAgent).GetAwaiter().GetResult();
        }

        public TokenRotationResult Rotate(string rawToken, string? ipAddress, string? userAgent)
        {
            return RotateAsync(rawToken, ipAddress, userAgent).GetAwaiter().GetResult();
        }

        public void Revoke(string rawToken, string? ipAddress)
        {
            RevokeAsync(rawToken, ipAddress).GetAwaiter().GetResult();
        }

        public void RevokeAll(long customerId, string? ipAddress)
        {
            RevokeAllAsync(customerId, ipAddress).GetAwaiter().GetResult();
        }
    }
}
