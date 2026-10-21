export const ALLOWED_IMAGE_EXTENSIONS: readonly string[] = ['.jpg', '.jpeg', '.png'];
export const ALLOWED_IMAGE_MIME_TYPES: readonly string[] = ['image/jpeg', 'image/png'];
export const MAX_IMAGE_FILE_SIZE_BYTES = 5 * 1024 * 1024; // 5 MB

export interface FileValidationResult {
  isValid: boolean;
  errorMessage?: string;
}

/**
 * Validates that an image file strictly conforms to allowed formats (.jpg, .jpeg, .png),
 * MIME types (image/jpeg, image/png), and size constraints.
 */
export function validateImageFile(
  file: File,
  maxFileSizeBytes: number = MAX_IMAGE_FILE_SIZE_BYTES
): FileValidationResult {
  if (!file || file.size === 0) {
    return {
      isValid: false,
      errorMessage: `File '${file?.name || 'Unknown'}' is empty.`,
    };
  }

  const fileName = file.name || '';
  const lastDot = fileName.lastIndexOf('.');
  const ext = lastDot !== -1 ? fileName.substring(lastDot).toLowerCase() : '';

  if (!ALLOWED_IMAGE_EXTENSIONS.includes(ext)) {
    return {
      isValid: false,
      errorMessage: `File '${fileName}' is not a valid image. Only JPG, JPEG, and PNG image files are allowed.`,
    };
  }

  const mime = (file.type || '').toLowerCase();
  if (mime) {
    if (!ALLOWED_IMAGE_MIME_TYPES.includes(mime)) {
      return {
        isValid: false,
        errorMessage: `File '${fileName}' has an unsupported image format. Only JPG, JPEG, and PNG image files are allowed.`,
      };
    }

    if (['.jpg', '.jpeg'].includes(ext) && mime !== 'image/jpeg') {
      return {
        isValid: false,
        errorMessage: `File '${fileName}' content type does not match its file extension. Only JPG, JPEG, and PNG image files are allowed.`,
      };
    }

    if (ext === '.png' && mime !== 'image/png') {
      return {
        isValid: false,
        errorMessage: `File '${fileName}' content type does not match its file extension. Only JPG, JPEG, and PNG image files are allowed.`,
      };
    }
  }

  if (file.size > maxFileSizeBytes) {
    const maxMb = Math.floor(maxFileSizeBytes / (1024 * 1024));
    return {
      isValid: false,
      errorMessage: `File '${fileName}' exceeds maximum allowed size of ${maxMb}MB.`,
    };
  }

  return { isValid: true };
}
