using System.Security.Cryptography;
using Khel_Akhel_Server.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Khel_Akhel_Server.BL.Services
{
    public interface ICaptchaService
    {
        CaptchaResponseData GenerateCaptcha();
        bool ValidateCaptcha(string? captchaId, string? inputCaptcha);
    }

    public class CaptchaService : ICaptchaService
    {
        private readonly IMemoryCache _cache;
        private static readonly char[] Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static readonly char[] Digits = "0123456789".ToCharArray();
        private static readonly TimeSpan CaptchaTtl = TimeSpan.FromMinutes(5);

        public CaptchaService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public CaptchaResponseData GenerateCaptcha()
        {
            // Pick 3 random uppercase letters (A-Z)
            char[] selectedLetters = new char[3];
            for (int i = 0; i < 3; i++)
            {
                selectedLetters[i] = Letters[RandomNumberGenerator.GetInt32(Letters.Length)];
            }

            // Pick 2 random digits (0-9)
            char[] selectedDigits = new char[2];
            for (int i = 0; i < 2; i++)
            {
                selectedDigits[i] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
            }

            // Combine into array of 5 characters
            char[] captchaChars = new char[5];
            captchaChars[0] = selectedLetters[0];
            captchaChars[1] = selectedLetters[1];
            captchaChars[2] = selectedLetters[2];
            captchaChars[3] = selectedDigits[0];
            captchaChars[4] = selectedDigits[1];

            // Fisher-Yates shuffle to randomize positions of letters and digits
            for (int i = captchaChars.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (captchaChars[i], captchaChars[j]) = (captchaChars[j], captchaChars[i]);
            }

            string captchaCode = new string(captchaChars);
            string captchaId = Guid.NewGuid().ToString("N");

            // Cache with 5-minute absolute expiration
            _cache.Set(captchaId, captchaCode, CaptchaTtl);

            return new CaptchaResponseData
            {
                CaptchaId = captchaId,
                Captcha = captchaCode
            };
        }

        public bool ValidateCaptcha(string? captchaId, string? inputCaptcha)
        {
            if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(inputCaptcha))
            {
                return false;
            }

            // One-time use: retrieve and remove immediately
            if (_cache.TryGetValue(captchaId, out string? storedCaptcha))
            {
                _cache.Remove(captchaId);

                if (storedCaptcha != null)
                {
                    // Case-insensitive comparison so both uppercase and lowercase inputs succeed
                    return string.Equals(storedCaptcha.Trim(), inputCaptcha.Trim(), StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }
    }
}
