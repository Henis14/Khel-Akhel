export class StringUtils {
  static trim(value: any): string {
    if (value === null || value === undefined) return '';
    return String(value).trim();
  }

  static trimOrNull(value: any): string | null {
    if (value === null || value === undefined) return null;
    const trimmed = String(value).trim();
    return trimmed.length > 0 ? trimmed : null;
  }
}
