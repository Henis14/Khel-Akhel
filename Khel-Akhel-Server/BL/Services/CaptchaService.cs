using System.Security.Cryptography;
using System.Text;
using Khel_Akhel_Server.Models;
using Microsoft.Extensions.Caching.Memory;
using SkiaSharp;

namespace Khel_Akhel_Server.BL.Services
{
    /// <summary>
    /// Captcha generation and verification service.
    /// NOTE: In-memory cache storage assumes a single server instance.
    /// </summary>
    public interface ICaptchaService
    {
        CaptchaResponseData GenerateCaptcha();
        bool ValidateCaptcha(string? captchaId, string? inputCaptcha);
    }

    public class CaptchaService : ICaptchaService
    {
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CaptchaTtl = TimeSpan.FromMinutes(5);

        // Reduced alphabet without confusing characters (no 0/O, 1/I/L, 5/S, 2/Z)
        private const string AllowedCharacters = "346789ABCDEFGHJKMNPQRTUVWXY";

        public CaptchaService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public CaptchaResponseData GenerateCaptcha()
        {
            char[] chars = new char[5];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = AllowedCharacters[RandomNumberGenerator.GetInt32(AllowedCharacters.Length)];
            }
            string code = new string(chars);

            string captchaId = Guid.NewGuid().ToString("N");

            // Store SHA-256 hash of upper-cased answer rather than the answer itself
            byte[] answerHash = SHA256.HashData(Encoding.UTF8.GetBytes(code.ToUpperInvariant()));
            _cache.Set(captchaId, answerHash, CaptchaTtl);

            string imageBase64 = RenderCaptchaImage(code);

            return new CaptchaResponseData
            {
                CaptchaId = captchaId,
                CaptchaImage = $"data:image/png;base64,{imageBase64}"
            };
        }

        public bool ValidateCaptcha(string? captchaId, string? inputCaptcha)
        {
            if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(inputCaptcha))
            {
                return false;
            }

            // One-time use: retrieve and remove immediately
            if (_cache.TryGetValue(captchaId, out byte[]? storedHash))
            {
                _cache.Remove(captchaId);

                if (storedHash != null)
                {
                    byte[] inputHash = SHA256.HashData(Encoding.UTF8.GetBytes(inputCaptcha.Trim().ToUpperInvariant()));
                    return CryptographicOperations.FixedTimeEquals(storedHash, inputHash);
                }
            }

            return false;
        }

        private static string RenderCaptchaImage(string code)
        {
            const int width = 180;
            const int height = 60;

            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;

            // Background
            canvas.Clear(new SKColor(245, 246, 250));

            // Background noise dots
            using (var dotPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true })
            {
                for (int i = 0; i < 70; i++)
                {
                    dotPaint.Color = new SKColor(
                        (byte)RandomNumberGenerator.GetInt32(180, 220),
                        (byte)RandomNumberGenerator.GetInt32(180, 220),
                        (byte)RandomNumberGenerator.GetInt32(180, 220));
                    float x = RandomNumberGenerator.GetInt32(width);
                    float y = RandomNumberGenerator.GetInt32(height);
                    float r = RandomNumberGenerator.GetInt32(1, 3);
                    canvas.DrawCircle(x, y, r, dotPaint);
                }
            }

            // 3-5 random lines/curves crossing text
            int lineCount = RandomNumberGenerator.GetInt32(3, 6);
            for (int i = 0; i < lineCount; i++)
            {
                using var linePaint = new SKPaint
                {
                    Color = new SKColor(
                        (byte)RandomNumberGenerator.GetInt32(120, 180),
                        (byte)RandomNumberGenerator.GetInt32(120, 180),
                        (byte)RandomNumberGenerator.GetInt32(120, 180)),
                    StrokeWidth = RandomNumberGenerator.GetInt32(1, 3),
                    Style = SKPaintStyle.Stroke,
                    IsAntialias = true
                };

                var pathBuilder = new SKPathBuilder();
                pathBuilder.MoveTo(RandomNumberGenerator.GetInt32(0, 30), RandomNumberGenerator.GetInt32(10, height - 10));
                pathBuilder.QuadTo(
                    RandomNumberGenerator.GetInt32(40, width - 40),
                    RandomNumberGenerator.GetInt32(0, height),
                    RandomNumberGenerator.GetInt32(width - 30, width),
                    RandomNumberGenerator.GetInt32(10, height - 10));
                using var path = pathBuilder.Detach();
                canvas.DrawPath(path, linePaint);
            }

            // Text colors: dark tones
            SKColor[] textColors =
            {
                new SKColor(44, 62, 80),
                new SKColor(39, 60, 117),
                new SKColor(113, 88, 30),
                new SKColor(30, 39, 46),
                new SKColor(47, 53, 66)
            };

            float charSpacing = (width - 30) / (float)code.Length;
            using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default;
            using var font = new SKFont(typeface, 30);

            for (int i = 0; i < code.Length; i++)
            {
                using var textPaint = new SKPaint
                {
                    Color = textColors[RandomNumberGenerator.GetInt32(textColors.Length)],
                    IsAntialias = true
                };

                float x = 20 + i * charSpacing + RandomNumberGenerator.GetInt32(-3, 4);
                float y = height / 2f + 10 + RandomNumberGenerator.GetInt32(-4, 5);
                float rotationAngle = RandomNumberGenerator.GetInt32(-25, 26);

                canvas.Save();
                canvas.RotateDegrees(rotationAngle, x, y);
                canvas.DrawText(code[i].ToString(), x, y, SKTextAlign.Left, font, textPaint);
                canvas.Restore();
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return Convert.ToBase64String(data.ToArray());
        }
    }
}
