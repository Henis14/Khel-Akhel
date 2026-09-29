namespace Khel_Akhel_Server.BL.Common
{
    public class FileValidationHelper
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };

        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png" };

        public static async Task<(bool isValid, string errorMessage)> IsValidImageAsync(
            IFormFile file,
            long maxFileSizeBytes = 5 * 1024 * 1024
        )
        {
            if (file == null || file.Length == 0)
            {
                return (false, "File is empty");
            }

            string ext = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();

            if (!AllowedExtensions.Contains(ext))
            {
                return (false, "Only JPG, JPEG and PNG files are allowed");
            }

            string contentType = (file.ContentType ?? string.Empty).ToLowerInvariant();

            if (!AllowedContentTypes.Contains(contentType))
            {
                return (false, "Only image files are allowed");
            }

            if (file.Length > maxFileSizeBytes)
            {
                long maxMb = maxFileSizeBytes / (1024 * 1024);
                return (false, $"Maximum allowed file size is {maxMb} MB");
            }

            byte[] jpg = { 255, 216 };
            byte[] png = { 137, 80, 78, 71 };

            using var stream = file.OpenReadStream();
            byte[] buffer = new byte[4];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            bool validSignature =
                (bytesRead >= 2 && jpg.SequenceEqual(buffer.Take(2)))
                || (bytesRead >= 4 && png.SequenceEqual(buffer));

            if (!validSignature)
            {
                return (false, "Invalid file signature");
            }

            return (true, string.Empty);
        }
    }
}
