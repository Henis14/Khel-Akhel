using Khel_Akhel_Server.BL.Common;
using Microsoft.Data.SqlClient;

namespace Khel_Akhel_Server.BL.Services
{
    public interface IAuditService
    {
        Task InsertAuditAsync(
            long? customerId,
            string moduleName,
            string action,
            string? tableName = null,
            long? entityId = null,
            string? oldValue = null,
            string? newValue = null,
            string? ipAddress = null,
            string? userAgent = null
        );
    }

    public class AuditService : IAuditService
    {
        private readonly DbHelper _db;

        public AuditService(DbHelper db)
        {
            _db = db;
        }

        public async Task InsertAuditAsync(
            long? customerId,
            string moduleName,
            string action,
            string? tableName = null,
            long? entityId = null,
            string? oldValue = null,
            string? newValue = null,
            string? ipAddress = null,
            string? userAgent = null
        )
        {
            try
            {
                string query = @"
                    INSERT INTO drs_audit_mst
                    (customer_id, ModuleName, Action, TableName, EntityId, OldValue, NewValue, IPAddress, UserAgent, CreatedDate)
                    VALUES
                    (@CustomerId, @ModuleName, @Action, @TableName, @EntityId, @OldValue, @NewValue, @IPAddress, @UserAgent, GETDATE())";

                SqlParameter[] parameters = new[]
                {
                    new SqlParameter("@CustomerId", (object?)customerId ?? DBNull.Value),
                    new SqlParameter("@ModuleName", moduleName ?? "System"),
                    new SqlParameter("@Action", action ?? "Action"),
                    new SqlParameter("@TableName", (object?)tableName ?? DBNull.Value),
                    new SqlParameter("@EntityId", (object?)entityId ?? DBNull.Value),
                    new SqlParameter("@OldValue", (object?)SanitizeAuditValue(oldValue) ?? DBNull.Value),
                    new SqlParameter("@NewValue", (object?)SanitizeAuditValue(newValue) ?? DBNull.Value),
                    new SqlParameter("@IPAddress", (object?)ipAddress ?? DBNull.Value),
                    new SqlParameter("@UserAgent", (object?)userAgent ?? DBNull.Value)
                };

                await Task.Run(() => _db.ExecuteNonQuery(query, parameters));
            }
            catch (Exception ex)
            {
                Logs.Error("Failed to insert audit entry into drs_audit_mst", ex);
            }
        }

        private static string? SanitizeAuditValue(string? rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
                return rawJson;

            // Simple precaution against password leakage in JSON string representations
            string sanitized = rawJson;
            string[] sensitiveKeys = { "password", "passwordhash", "jwt", "refreshtoken", "tokenhash", "otp", "cvv" };
            foreach (var key in sensitiveKeys)
            {
                if (sanitized.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    sanitized = System.Text.RegularExpressions.Regex.Replace(
                        sanitized,
                        $@"\""{key}\""\s*:\s*\""[^\""]*\""",
                        $"\"{key}\":\"[REDACTED]\"",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );
                }
            }
            return sanitized;
        }
    }
}
