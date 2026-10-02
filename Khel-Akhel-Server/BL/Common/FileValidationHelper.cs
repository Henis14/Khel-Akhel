namespace Khel_Akhel_Server.BL.Common
{
    public class FileValidationHelper
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        private static readonly string[] AllowedContentTypes =
        {
            "image/jpeg",
            "image/png",
            "image/webp",
        };

        public static async Task<(bool isValid, string errorMessage)> IsValidImageAsync(
            IFormFile file,
            long maxFileSizeBytes = 5 * 1024 * 1024
        )
        {
            if (file == null || file.Length == 0)
            {
                return (false, "File is empty or missing.");
            }

            string ext = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();

            if (!AllowedExtensions.Contains(ext))
            {
                return (false, "Only JPG, JPEG, PNG, and WEBP files are allowed.");
            }

            string contentType = (file.ContentType ?? string.Empty).ToLowerInvariant();

            if (!AllowedContentTypes.Contains(contentType))
            {
                return (false, "Invalid image format.");
            }

            if (file.Length > maxFileSizeBytes)
            {
                long maxMb = maxFileSizeBytes / (1024 * 1024);
                return (false, $"Maximum allowed file size is {maxMb} MB per image.");
            }

            byte[] jpg = { 255, 216 };
            byte[] png = { 137, 80, 78, 71 };
            byte[] riff = { 82, 73, 70, 70 }; // "RIFF" for WEBP

            using var stream = file.OpenReadStream();
            byte[] buffer = new byte[12];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            bool isJpg = bytesRead >= 2 && jpg.SequenceEqual(buffer.Take(2));
            bool isPng = bytesRead >= 4 && png.SequenceEqual(buffer.Take(4));
            bool isWebp =
                bytesRead >= 12
                && riff.SequenceEqual(buffer.Take(4))
                && buffer[8] == 87
                && buffer[9] == 69
                && buffer[10] == 66
                && buffer[11] == 80; // "WEBP"

            bool validSignature = isJpg || isPng || isWebp;

            if (!validSignature)
            {
                return (false, "Invalid file signature or corrupted image file.");
            }

            return (true, string.Empty);
        }
    }
}
