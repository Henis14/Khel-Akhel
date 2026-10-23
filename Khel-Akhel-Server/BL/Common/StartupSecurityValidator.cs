using System.Text;

namespace Khel_Akhel_Server.BL.Common
{
    public static class StartupSecurityValidator
    {
        private static readonly string[] CompromisedKeys = new[]
        {
            "KhelAkhelSecretKeyForJWTAuthentication2026SecureKey!",
            "DefaultUrlEncryptionKey2026SecureKey!",
            "DefaultUrlSignatureSecretKey2026!",
        };

        public static void Validate(IConfiguration configuration)
        {
            ValidateSecret(configuration, "Jwt:Key", 32);
            ValidateSecret(configuration, "Security:UrlEncryptionKey", 32);
            ValidateSecret(configuration, "Security:UrlSignatureKey", 32);

            string? issuer = configuration["Jwt:Issuer"];
            if (string.IsNullOrWhiteSpace(issuer))
            {
                throw new InvalidOperationException(
                    "Configuration 'Jwt:Issuer' is required and cannot be empty."
                );
            }

            string? audience = configuration["Jwt:Audience"];
            if (string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException(
                    "Configuration 'Jwt:Audience' is required and cannot be empty."
                );
            }
        }

        public static void ValidateSecret(
            IConfiguration configuration,
            string keyName,
            int minByteLength
        )
        {
            string? value = configuration[keyName];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Required security configuration '{keyName}' is missing or empty."
                );
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length < minByteLength)
            {
                throw new InvalidOperationException(
                    $"Security configuration '{keyName}' must be at least {minByteLength} bytes long (current length is {bytes.Length} bytes)."
                );
            }

            foreach (var compromised in CompromisedKeys)
            {
                if (string.Equals(value, compromised, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Security configuration '{keyName}' matches a known compromised key from git history. A new unique secret must be generated."
                    );
                }
            }
        }
    }
}
