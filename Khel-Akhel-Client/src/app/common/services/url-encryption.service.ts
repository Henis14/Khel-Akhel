import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class UrlEncryptionService {
  /**
   * Validates if a string looks like a valid base64url encrypted ID string from ASP.NET backend.
   */
  isValidEncryptedId(encryptedId: string | null | undefined): boolean {
    if (!encryptedId) return false;
    const trimmed = encryptedId.trim();
    if (trimmed.length < 10) return false;
    // Base64Url pattern (letters, numbers, -, _)
    return /^[a-zA-Z0-9_-]+$/.test(trimmed);
  }

  /**
   * Encodes URI component safely for route paths.
   */
  encodeParam(param: string): string {
    return encodeURIComponent(param.trim());
  }

  /**
   * Decodes URI component safely.
   */
  decodeParam(param: string): string {
    try {
      return decodeURIComponent(param);
    } catch {
      return param;
    }
  }
}
