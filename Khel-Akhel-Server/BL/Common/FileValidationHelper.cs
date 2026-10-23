namespace Khel_Akhel_Server.BL.Common
{
    public class FileValidationHelper
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

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

        // WEBP "RIFF" signature header
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
                return (false, "Only JPG, JPEG, PNG, and WEBP files are allowed.");
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

            if (ext == ".webp" && contentType != "image/webp")
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

            // Reliable JPEG signature: SOI marker (0xFF, 0xD8) followed by marker prefix (0xFF)
            bool isJpg =
                bytesRead >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;

            // Reliable PNG signature: standard 8-byte signature
            bool isPng = bytesRead >= 8 && buffer.Take(8).SequenceEqual(PngHeader);

            // WEBP files: RIFF header + "WEBP" at offset 8
            bool isWebp =
                bytesRead >= 12
                && RiffHeader.SequenceEqual(buffer.Take(4))
                && buffer[8] == 0x57 // 'W'
                && buffer[9] == 0x45 // 'E'
                && buffer[10] == 0x42 // 'B'
                && buffer[11] == 0x50; // 'P'

            if ((ext == ".jpg" || ext == ".jpeg") && !isJpg)
            {
                return (false, "Invalid file signature or corrupted image file.");
            }

            if (ext == ".png" && !isPng)
            {
                return (false, "Invalid file signature or corrupted image file.");
            }

            if (ext == ".webp" && !isWebp)
            {
                return (false, "Invalid file signature or corrupted image file.");
            }

            bool validSignature =
                (isJpg && (ext == ".jpg" || ext == ".jpeg"))
                || (isPng && ext == ".png")
                || (isWebp && ext == ".webp");

            if (!validSignature)
            {
                return (false, "Invalid file signature or corrupted image file.");
            }

            return (true, string.Empty);
        }

        // Standard 4-byte "ftyp" box type signature: 66 74 79 70
        private static readonly byte[] FtypHeader = { 0x66, 0x74, 0x79, 0x70 };

        public static async Task<(bool isValid, string errorMessage)> IsValidMp4VideoAsync(
            IFormFile file,
            long maxFileSizeBytes = 50 * 1024 * 1024 // 50MB
        )
        {
            if (file == null || file.Length == 0)
            {
                return (false, "Video file is empty or missing.");
            }

            string ext = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();

            if (ext != ".mp4")
            {
                return (false, "Only MP4 video files are allowed. Other video formats like MOV, AVI, WebM, and MKV are strictly prohibited.");
            }

            string contentType = (file.ContentType ?? string.Empty).ToLowerInvariant();

            if (contentType != "video/mp4")
            {
                return (false, "Invalid video format. Content-Type must be video/mp4.");
            }

            if (file.Length > maxFileSizeBytes)
            {
                long maxMb = maxFileSizeBytes / (1024 * 1024);
                return (false, $"Maximum allowed video size is {maxMb} MB.");
            }

            // Read the file header to verify ISOBMFF / MP4 container structure
            byte[] buffer = new byte[64];
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

            if (bytesRead < 12)
            {
                return (false, "Corrupted video file: header too short.");
            }

            // In an ISOBMFF file, bytes 4..7 are the box type ("ftyp")
            bool hasFtyp = buffer[4] == 0x66 && buffer[5] == 0x74 && buffer[6] == 0x79 && buffer[7] == 0x70;

            if (!hasFtyp)
            {
                return (false, "Invalid file signature or corrupted MP4 video file. Missing standard MP4 ftyp container box.");
            }

            // Extract major brand (bytes 8..11)
            string majorBrand = System.Text.Encoding.ASCII.GetString(buffer, 8, 4);

            // Disallow Quicktime-only if major brand is "qt  " without mp4 compatible brand
            if (majorBrand == "qt  ")
            {
                // Check if any compatible brand in ftyp box is mp4/isom
                bool hasMp4Compatible = false;
                for (int offset = 16; offset + 4 <= bytesRead; offset += 4)
                {
                    string compat = System.Text.Encoding.ASCII.GetString(buffer, offset, 4);
                    if (compat.StartsWith("mp4", StringComparison.OrdinalIgnoreCase) ||
                        compat.StartsWith("iso", StringComparison.OrdinalIgnoreCase) ||
                        compat.StartsWith("avc", StringComparison.OrdinalIgnoreCase))
                    {
                        hasMp4Compatible = true;
                        break;
                    }
                }

                if (!hasMp4Compatible)
                {
                    return (false, "QuickTime MOV containers (.mov / qt) are not allowed. Please provide a standard MP4 file.");
                }
            }

            return (true, string.Empty);
        }
    }
}
