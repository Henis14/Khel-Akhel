namespace Khel_Akhel_Server.BL.Common
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
                return false;
            try
            {
                if (BCrypt.Net.BCrypt.Verify(password.Trim(), hash.Trim()))
                {
                    return true;
                }
            }
            catch
            {
                // Fallback for plain-text passwords in database
            }
            return string.Equals(password.Trim(), hash.Trim(), StringComparison.Ordinal);
        }
    }
}
