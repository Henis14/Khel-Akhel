namespace Khel_Akhel_Server.BL.Common
{
    public class FileValidationHelper
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };

        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png" };

        // Standard 8-byte PNG file signature: 89 50 4E 47 0D 0A 1A 0A
        private static readonly byte[] PngHeader =
        {
            0x89,
            0x50,
            0x4E,
            0x47,
            0x0D,
            0x0A,
            0x1A,
            0x0A,
        };

        // WEBP "RIFF" signature header for proactive detection and rejection
        private static readonly byte[] RiffHeader = { 0x52, 0x49, 0x46, 0x46 }; // "RIFF"

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

            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            {
                return (false, "Only JPG, JPEG, and PNG files are allowed.");
            }

            string contentType = (file.ContentType ?? string.Empty).ToLowerInvariant();

            if (!AllowedContentTypes.Contains(contentType))
            {
                return (false, "Invalid image format.");
            }

            // Cross-validate extension and MIME type
            if ((ext == ".jpg" || ext == ".jpeg") && contentType != "image/jpeg")
            {
                return (false, "Content type does not match file extension.");
            }

            if (ext == ".png" && contentType != "image/png")
            {
                return (false, "Content type does not match file extension.");
            }

            if (file.Length > maxFileSizeBytes)
            {
                long maxMb = maxFileSizeBytes / (1024 * 1024);
                return (false, $"Maximum allowed file size is {maxMb} MB per image.");
            }

            // Read the file header to verify magic bytes
            byte[] buffer = new byte[16];
            int bytesRead;

            using (var stream = file.OpenReadStream())
            {
                int totalRead = 0;
                while (totalRead < buffer.Length)
                {
                    int read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead);
                    if (read == 0)
                        break;
                    totalRead += read;
                }
                bytesRead = totalRead;

                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }
            }

            // Proactively detect and reject WEBP files (even if renamed to .jpg or .png)
            bool isWebp =
                bytesRead >= 12
                && RiffHeader.SequenceEqual(buffer.Take(4))
                && buffer[8] == 0x57 // 'W'
                && buffer[9] == 0x45 // 'E'
                && buffer[10] == 0x42 // 'B'
                && buffer[11] == 0x50; // 'P'

            if (isWebp)
            {
                return (
                    false,
                    "WEBP image format is not allowed. Only JPG, JPEG, and PNG files are allowed."
                );
            }

            // Reliable JPEG signature: SOI marker (0xFF, 0xD8) followed by marker prefix (0xFF)
            // Covers JFIF (0xFF 0xD8 0xFF 0xE0), Exif (0xFF 0xD8 0xFF 0xE1), Adobe (0xFF 0xD8 0xFF 0xEE), etc.
            bool isJpg =
                bytesRead >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;

            // Reliable PNG signature: standard 8-byte signature
            bool isPng = bytesRead >= 8 && buffer.Take(8).SequenceEqual(PngHeader);

            if ((ext == ".jpg" || ext == ".jpeg") && !isJpg)
            {
                return (false, "Invalid file signature or corrupted image file.");
            }

            if (ext == ".png" && !isPng)
            {
                return (false, "Invalid file signature or corrupted image file.");
            }

            bool validSignature =
                (isJpg && (ext == ".jpg" || ext == ".jpeg")) || (isPng && ext == ".png");

            if (!validSignature)
            {
                return (false, "Invalid file signature or corrupted image file.");
            }

            return (true, string.Empty);
        }
    }
}
