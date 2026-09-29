using System.Security.Cryptography;
using System.Text;

namespace Khel_Akhel_Server.BL.Services
{
    public interface IUrlSignatureService
    {
        string GenerateSignature(string payload, long expiryTimestamp);
        bool ValidateSignature(string payload, long expiryTimestamp, string signature);
    }

    public class UrlSignatureService : IUrlSignatureService
    {
        private readonly byte[] _secretKey;

        public UrlSignatureService(IConfiguration configuration)
        {
            string rawKey = configuration["Security:UrlSignatureKey"]
                         ?? configuration["Jwt:Key"]
                         ?? "DefaultUrlSignatureSecretKey2026!";
            _secretKey = Encoding.UTF8.GetBytes(rawKey);
        }

        public string GenerateSignature(string payload, long expiryTimestamp)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return string.Empty;

            string dataToSign = $"{payload}:{expiryTimestamp}";
            using var hmac = new HMACSHA256(_secretKey);
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));

            return Convert.ToBase64String(hash)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        public bool ValidateSignature(string payload, long expiryTimestamp, string signature)
        {
            if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signature))
                return false;

            // Check signature expiry (UTC Unix Seconds)
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (expiryTimestamp < currentTimestamp)
                return false;

            string expectedSignature = GenerateSignature(payload, expiryTimestamp);

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(expectedSignature)
            );
        }
    }
}
