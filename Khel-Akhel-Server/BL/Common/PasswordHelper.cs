namespace Khel_Akhel_Server.BL.Common
{
    public static class PasswordHelper
    {
        public const int WorkFactor = 12;

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;
            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public static bool VerifyPassword(string password, string hash, long? customerId = null)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            // BCrypt hashes must start with $2a$, $2b$, or $2y$
            if (!hash.StartsWith("$2a$", StringComparison.Ordinal) &&
                !hash.StartsWith("$2b$", StringComparison.Ordinal) &&
                !hash.StartsWith("$2y$", StringComparison.Ordinal))
            {
                Logs.Warning($"Password verification failed: Non-BCrypt hash format for customer ID: {customerId?.ToString() ?? "unknown"}");
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (Exception ex)
            {
                Logs.Warning($"Password verification exception for customer ID: {customerId?.ToString() ?? "unknown"}. Reason: {ex.Message}");
                return false;
            }
        }
    }
}
