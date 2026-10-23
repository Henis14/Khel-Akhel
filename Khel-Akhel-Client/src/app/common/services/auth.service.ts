import { Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, BehaviorSubject, of, throwError, timer } from 'rxjs';
import { catchError, tap, switchMap } from 'rxjs/operators';
import { ApiService } from './api.service';
import {
  PortalLoginRequest,
  AuthResponseData,
  UserSession,
  CaptchaResponseData,
  ChangePasswordRequest,
} from '../models/auth.model';
import { CustomerCreateRequest } from '../../admin/models/customer.model';
import { ApiResponse } from '../models/api-response.model';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private accessTokenSubject = new BehaviorSubject<string | null>(null);
  public accessToken$ = this.accessTokenSubject.asObservable();

  // Reactive signals for modern Angular architecture
  public currentUser = signal<UserSession | null>(null);
  public isAuthenticated = computed(() => !!this.currentUser()?.token);

  public isAdmin = computed(() => {
    const user = this.currentUser();
    if (!user) return false;
    return AuthService.parseIsAdmin(user.isAdmin) || AuthService.parseIsAdmin(user.role);
  });

  constructor(
    private apiService: ApiService,
    private router: Router,
  ) {}

  /**
   * Robust parser for IsAdmin flag across primitive representations (1, "1", true, "true", "Admin").
   */
  public static parseIsAdmin(value: any): boolean {
    if (value === true || value === 1 || value === '1') {
      return true;
    }
    if (typeof value === 'string') {
      const lower = value.trim().toLowerCase();
      return lower === 'true' || lower === 'admin';
    }
    return false;
  }

  public get token(): string | null {
    return this.currentUser()?.token || this.accessTokenSubject.value;
  }

  public get role(): string | null {
    return this.currentUser()?.role ?? null;
  }

  getCaptcha(): Observable<ApiResponse<CaptchaResponseData>> {
    return this.apiService.get<CaptchaResponseData>('auth/captcha');
  }

  register(request: CustomerCreateRequest): Observable<ApiResponse<any>> {
    return this.apiService.post<any>('customer/create', request);
  }

  changePassword(request: ChangePasswordRequest): Observable<ApiResponse<null>> {
    return this.apiService.patch<null>('auth/change-password', request);
  }

  login(credentials: PortalLoginRequest): Observable<ApiResponse<AuthResponseData>> {
    return this.apiService.post<AuthResponseData>('auth/login', credentials).pipe(
      tap((res) => {
        if (res.success && res.data?.token) {
          this.setSession(res.data.token, res.data);
        }
      }),
    );
  }

  refreshToken(): Observable<ApiResponse<AuthResponseData>> {
    return this.apiService.post<AuthResponseData>('auth/refresh', {}).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 409) {
          // Concurrent refresh race: wait 300ms and retry once
          return timer(300).pipe(
            switchMap(() => this.apiService.post<AuthResponseData>('auth/refresh', {})),
          );
        }
        return throwError(() => err);
      }),
      tap((res) => {
        if (res.success && res.data?.token) {
          this.setSession(res.data.token, res.data);
        } else {
          this.clearSession();
        }
      }),
      catchError((err) => {
        this.clearSession();
        return throwError(() => err);
      }),
    );
  }

  forgotPassword(
    userName: string,
    captchaId?: string,
    captcha?: string,
  ): Observable<ApiResponse<string>> {
    return this.apiService.post<string>('auth/forgot-password', {
      userName,
      captchaId,
      captcha,
    });
  }

  validateResetToken(token: string): Observable<ApiResponse<{ valid: boolean }>> {
    return this.apiService.get<{ valid: boolean }>('auth/reset-password/validate', { token });
  }

  resetPassword(
    token: string,
    newPassword: string,
    confirmPassword: string,
  ): Observable<ApiResponse<string>> {
    return this.apiService.post<string>('auth/reset-password', {
      token,
      newPassword,
      confirmPassword,
    });
  }

  getPasswordResetRequests(
    status = 'PENDING',
    pageIndex = 1,
    pageSize = 10,
    search?: string,
  ): Observable<ApiResponse<any>> {
    const params: Record<string, string | number> = {
      status,
      pageIndex,
      pageSize,
    };
    if (search) {
      params['search'] = search;
    }
    return this.apiService.get<any>('admin/password-reset/requests', params);
  }

  issueResetLink(
    encryptedRequestId: string,
    adminNotes: string,
  ): Observable<ApiResponse<{ resetUrl: string; expiresUtc: string }>> {
    return this.apiService.post<{ resetUrl: string; expiresUtc: string }>(
      `admin/password-reset/requests/${encodeURIComponent(encryptedRequestId)}/issue-link`,
      { adminNotes },
    );
  }

  rejectResetRequest(
    encryptedRequestId: string,
    adminNotes: string,
  ): Observable<ApiResponse<string>> {
    return this.apiService.post<string>(
      `admin/password-reset/requests/${encodeURIComponent(encryptedRequestId)}/reject`,
      { adminNotes },
    );
  }

  issueResetLinkForCustomer(
    encryptedCustomerId: string,
    adminNotes: string,
  ): Observable<ApiResponse<{ resetUrl: string; expiresUtc: string }>> {
    return this.apiService.post<{ resetUrl: string; expiresUtc: string }>(
      `admin/password-reset/customers/${encodeURIComponent(encryptedCustomerId)}/issue-link`,
      { adminNotes },
    );
  }

  logout(): Observable<ApiResponse<null>> {
    return this.apiService.post<null>('auth/logout', {}).pipe(
      tap(() => {
        this.clearSessionAndRedirect();
      }),
      catchError(() => {
        this.clearSessionAndRedirect();
        return of({ success: true, statusCode: 200, message: 'LOGOUT_SUCCESS', data: null });
      }),
    );
  }

  public clearSessionAndRedirect(targetUrl?: string): void {
    const wasAdmin = this.isAdmin();
    this.clearSession();
    if (targetUrl) {
      this.router.navigateByUrl(targetUrl);
    } else if (wasAdmin) {
      this.router.navigate(['/admin/login']);
    } else {
      // Customers return to the storefront sign-in page
      this.router.navigate(['/login']);
    }
  }

  public clearSession(): void {
    this.accessTokenSubject.next(null);
    this.currentUser.set(null);
  }

  private setSession(token: string, responseData?: any): void {
    this.accessTokenSubject.next(token);
    const decoded = this.decodeJwtPayload(token);

    const roleStr = this.extractRole(decoded, responseData);
    const isAdminBool = this.extractIsAdmin(decoded, responseData, roleStr);
    const role = isAdminBool ? 'Admin' : roleStr;

    const email =
      decoded?.email ||
      decoded?.Email ||
      decoded?.unique_name ||
      decoded?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
      responseData?.email ||
      '';

    const userIdStr =
      decoded?.UserId ||
      decoded?.userId ||
      decoded?.sub ||
      decoded?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
      responseData?.userId ||
      '0';

    const session: UserSession = {
      userId: parseInt(String(userIdStr), 10) || 0,
      email: email,
      role: role,
      isAdmin: isAdminBool,
      token: token,
    };

    this.currentUser.set(session);
  }

  private extractRole(decoded: any, responseData?: any): string {
    const candidates = [
      responseData?.role,
      responseData?.Role,
      responseData?.data?.role,
      responseData?.data?.Role,
      decoded?.role,
      decoded?.Role,
      decoded?.roles,
      decoded?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
      decoded?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'],
    ];

    for (const cand of candidates) {
      if (cand !== undefined && cand !== null && cand !== '') {
        if (Array.isArray(cand)) {
          return String(cand[0]);
        }
        return String(cand);
      }
    }

    return 'Customer';
  }

  private extractIsAdmin(decoded: any, responseData?: any, roleStr?: string): boolean {
    const candidates = [
      responseData?.isAdmin,
      responseData?.IsAdmin,
      responseData?.is_admin,
      responseData?.data?.isAdmin,
      responseData?.data?.IsAdmin,
      decoded?.isAdmin,
      decoded?.IsAdmin,
      decoded?.is_admin,
      roleStr,
    ];

    for (const cand of candidates) {
      if (cand !== undefined && cand !== null && cand !== '') {
        if (AuthService.parseIsAdmin(cand)) {
          return true;
        }
      }
    }

    return false;
  }

  private decodeJwtPayload(token: string): any {
    try {
      if (!token) return null;
      const parts = token.split('.');
      if (parts.length !== 3) return null;

      let base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      while (base64.length % 4 !== 0) {
        base64 += '=';
      }

      const binaryStr = atob(base64);
      let jsonString: string;

      if (typeof TextDecoder !== 'undefined') {
        const bytes = new Uint8Array(binaryStr.length);
        for (let i = 0; i < binaryStr.length; i++) {
          bytes[i] = binaryStr.charCodeAt(i);
        }
        jsonString = new TextDecoder('utf-8').decode(bytes);
      } else {
        jsonString = decodeURIComponent(
          binaryStr
            .split('')
            .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
            .join(''),
        );
      }

      return JSON.parse(jsonString);
    } catch {
      return null;
    }
  }
}
