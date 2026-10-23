using System.Data;
using System.Text;
using Khel_Akhel_Server.BL.Services;
using Microsoft.Data.SqlClient;

namespace Khel_Akhel_Server.BL.Common
{
    public static class SetPasswordCli
    {
        public static async Task<int> RunAsync(
            string[] args,
            IConfiguration configuration,
            Func<string, string>? passwordReader = null
        )
        {
            string? email = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--email", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    email = args[i + 1]?.Trim();
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Missing required argument '--email <email>'.");
                Console.WriteLine("Usage: dotnet run -- set-password --email <email>");
                Console.ResetColor();
                return 1;
            }

            var db = new DbHelper(configuration);
            var dt = db.ExecuteQuery(
                "SELECT TOP 1 ID, Email, FirstName, LastName, IsAdmin FROM drs_customer_mst WHERE LOWER(Email) = LOWER(@Email) AND IsDeleted = 0",
                new[] { new SqlParameter("@Email", email) });

            if (dt.Rows.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: User with email '{email}' was not found.");
                Console.ResetColor();
                return 1;
            }

            var row = dt.Rows[0];
            long customerId = Convert.ToInt64(row["ID"]);
            string userEmail = row["Email"].ToString() ?? email;

            Console.WriteLine($"Setting new password for: {userEmail} (ID: {customerId})");
            Console.WriteLine("Note: Password input will be masked.");

            var reader = passwordReader ?? ReadMaskedPassword;
            string password = reader("Enter new password: ");
            string confirm = reader("Confirm new password: ");

            if (!string.Equals(password, confirm, StringComparison.Ordinal))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Passwords do not match.");
                Console.ResetColor();
                return 1;
            }

            if (!ValidationHelper.IsValidPassword(password))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Password does not meet security requirements.");
                Console.WriteLine("Password must be at least 8 characters long and contain uppercase, lowercase, number, and special character.");
                Console.ResetColor();
                return 1;
            }

            string newHash = PasswordHelper.HashPassword(password);

            // Update password hash, reset failed login attempts, clear lock
            db.ExecuteNonQuery(
                @"UPDATE drs_customer_mst
                  SET PasswordHash = @Hash,
                      FailedLoginAttempts = 0,
                      AccountLockedUntil = NULL,
                      ModifiedDate = GETDATE()
                  WHERE ID = @ID",
                new[]
                {
                    new SqlParameter("@Hash", newHash),
                    new SqlParameter("@ID", customerId)
                });

            // Revoke all refresh tokens
            db.ExecuteNonQuery(
                @"UPDATE drs_refresh_token_mst
                  SET RevokedUtc = GETUTCDATE(),
                      RevokedByIp = 'CLI_TOOL',
                      IsActive = 0,
                      ModifiedDate = GETDATE()
                  WHERE customer_id = @CustomerId AND RevokedUtc IS NULL",
                new[]
                {
                    new SqlParameter("@CustomerId", customerId)
                });

            // Write audit row
            var auditService = new AuditService(db);
            await auditService.InsertAuditAsync(
                customerId: customerId,
                moduleName: "Auth",
                action: "AdminPasswordResetCli",
                tableName: "drs_customer_mst",
                entityId: customerId,
                oldValue: null,
                newValue: $"Password updated via CLI set-password tool for email '{userEmail}'",
                ipAddress: "127.0.0.1",
                userAgent: "KhelAkhel-CLI"
            );

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Success: Password updated and all active sessions revoked for '{userEmail}'.");
            Console.ResetColor();
            return 0;
        }

        public static string ReadMaskedPassword(string prompt)
        {
            Console.Write(prompt);
            var sb = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                    {
                        sb.Length--;
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    sb.Append(keyInfo.KeyChar);
                    Console.Write("*");
                }
            }
            return sb.ToString();
        }
    }
}
