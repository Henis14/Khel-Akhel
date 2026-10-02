import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class UrlSignatureService {
  /**
   * Formats signed URL query params for requests.
   */
  appendSignatureParams(baseUrl: string, signature: string, expiry: number): string {
    const separator = baseUrl.includes('?') ? '&' : '?';
    return `${baseUrl}${separator}signature=${encodeURIComponent(signature)}&expires=${expiry}`;
  }
}
