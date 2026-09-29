using System.Security.Cryptography;
using System.Text;

namespace Khel_Akhel_Server.BL.Services
{
    public interface IUrlEncryptionService
    {
        string Encrypt(long id);
        bool TryDecrypt(string encryptedId, out long id);
    }

    public class UrlEncryptionService : IUrlEncryptionService
    {
        private readonly byte[] _key;

        public UrlEncryptionService(IConfiguration configuration)
        {
            string rawKey = configuration["Security:UrlEncryptionKey"] 
                         ?? configuration["Jwt:Key"] 
                         ?? "DefaultUrlEncryptionKey2026SecureKey!";
            
            using var sha256 = SHA256.Create();
            _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
        }

        public string Encrypt(long id)
        {
            if (id <= 0)
                return string.Empty;

            try
            {
                byte[] plainBytes = BitConverter.GetBytes(id);
                byte[] iv = new byte[12];
                RandomNumberGenerator.Fill(iv);

                byte[] cipherText = new byte[plainBytes.Length];
                byte[] tag = new byte[16];

                using (var aesGcm = new AesGcm(_key, 16))
                {
                    aesGcm.Encrypt(iv, plainBytes, cipherText, tag);
                }

                byte[] combined = new byte[iv.Length + tag.Length + cipherText.Length];
                Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
                Buffer.BlockCopy(tag, 0, combined, iv.Length, tag.Length);
                Buffer.BlockCopy(cipherText, 0, combined, iv.Length + tag.Length, cipherText.Length);

                return Convert.ToBase64String(combined)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('=');
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool TryDecrypt(string encryptedId, out long id)
        {
            id = 0;
            if (string.IsNullOrWhiteSpace(encryptedId))
                return false;

            try
            {
                string incoming = encryptedId.Replace('-', '+').Replace('_', '/');
                switch (incoming.Length % 4)
                {
                    case 2: incoming += "=="; break;
                    case 3: incoming += "="; break;
                }

                byte[] combined = Convert.FromBase64String(incoming);
                if (combined.Length < 12 + 16 + 8)
                    return false;

                byte[] iv = new byte[12];
                byte[] tag = new byte[16];
                byte[] cipherText = new byte[combined.Length - 12 - 16];

                Buffer.BlockCopy(combined, 0, iv, 0, 12);
                Buffer.BlockCopy(combined, 12, tag, 0, 16);
                Buffer.BlockCopy(combined, 12 + 16, cipherText, 0, cipherText.Length);

                byte[] plainBytes = new byte[cipherText.Length];

                using (var aesGcm = new AesGcm(_key, 16))
                {
                    aesGcm.Decrypt(iv, cipherText, tag, plainBytes);
                }

                id = BitConverter.ToInt64(plainBytes, 0);
                return id > 0;
            }
            catch
            {
                id = 0;
                return false;
            }
        }
    }
}
