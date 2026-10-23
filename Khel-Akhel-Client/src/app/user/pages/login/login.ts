import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../common/services/auth.service';
import { CustomValidators } from '../../../common/validators/custom.validators';
import { PortalLoginRequest } from '../../../common/models/auth.model';

/**
 * Customer sign-in.
 * API: POST /api/auth/login  (AuthController.PortalLogin)
 *   body  { userName: email | mobile, password, captchaId, captcha, deviceInfo }
 *   200   { success, data: { token, expiresIn } } + HttpOnly refresh-token cookie
 *   400   INVALID_CAPTCHA | VALIDATION_FAILED
 *   401   INVALID_CREDENTIALS
 *   403   ACCOUNT_DISABLED
 *   423   ACCOUNT_TEMPORARILY_LOCKED
 *   429   RATE_LIMIT_EXCEEDED
 * The CAPTCHA is one-time use, so a fresh one is loaded after every failed attempt.
 */
@Component({
  selector: 'app-user-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class UserLoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  readonly loginForm: FormGroup = this.fb.group({
    userName: ['', [CustomValidators.requiredNonEmpty]],
    password: ['', [CustomValidators.requiredNonEmpty]],
    captcha: [
      '',
      [
        CustomValidators.requiredNonEmpty,
        Validators.minLength(5),
        Validators.maxLength(5),
        Validators.pattern(/^[a-zA-Z0-9]{5}$/),
      ],
    ],
  });

  readonly loading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal<boolean>(false);

  readonly captchaId = signal<string | null>(null);
  readonly captchaImage = signal<string | null>(null);
  readonly captchaLoading = signal<boolean>(false);
  readonly captchaError = signal<boolean>(false);

  private returnUrl = '/';

  get f() {
    return this.loginForm.controls;
  }

  ngOnInit(): void {
    this.returnUrl = this.safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl'));

    // Already signed in: skip the form
    if (this.authService.isAuthenticated()) {
      this.redirectAfterLogin();
      return;
    }

    this.loadCaptcha();
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((visible) => !visible);
  }

  loadCaptcha(): void {
    this.captchaLoading.set(true);
    this.captchaError.set(false);

    this.authService.getCaptcha().subscribe({
      next: (res) => {
        this.captchaLoading.set(false);
        if (res.success && res.data) {
          this.captchaId.set(res.data.captchaId);
          this.captchaImage.set(res.data.captchaImage);
        } else {
          this.captchaError.set(true);
        }
        this.loginForm.controls['captcha'].reset('');
      },
      error: () => {
        this.captchaLoading.set(false);
        this.captchaError.set(true);
      },
    });
  }

  onSubmit(): void {
    if (this.loading()) return;

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    if (!this.captchaId()) {
      this.errorMessage.set('The security code has not loaded yet. Refresh it and try again.');
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const value = this.loginForm.getRawValue();
    const payload: PortalLoginRequest = {
      userName: String(value.userName).trim(),
      password: String(value.password),
      captchaId: this.captchaId() ?? undefined,
      captcha: String(value.captcha).trim(),
      deviceInfo: navigator.userAgent,
    };

    this.authService.login(payload).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success && res.data?.token) {
          this.redirectAfterLogin();
        } else {
          this.errorMessage.set(this.messageFor(res.message));
          this.loadCaptcha();
        }
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.errorMessage.set(this.messageForError(err));
        this.loginForm.controls['password'].reset('');
        this.loadCaptcha();
      },
    });
  }

  /* ---------- helpers ---------- */

  private redirectAfterLogin(): void {
    // Admin accounts may sign in here too; send them to their dashboard
    const target = this.authService.isAdmin() ? '/admin/dashboard' : this.returnUrl;
    Promise.resolve().then(() => this.router.navigateByUrl(target).catch(() => {}));
  }

  /** Only allow in-app paths; never bounce back to a login page or out to another site */
  private safeReturnUrl(url: string | null): string {
    if (!url || !url.startsWith('/') || url.startsWith('//')) return '/';
    if (url.includes('/login') || url.startsWith('/admin')) return '/';
    return url;
  }

  private messageForError(err: HttpErrorResponse): string {
    if (err.status === 0) {
      return 'We could not reach the server. Check your connection and try again.';
    }
    if (err.status === 429) {
      return 'Too many sign-in attempts. Please wait a minute and try again.';
    }
    return this.messageFor(err.error?.message);
  }

  private messageFor(code?: string): string {
    switch (code) {
      case 'INVALID_CREDENTIALS':
        return 'That email/mobile and password do not match. Please try again.';
      case 'INVALID_CAPTCHA':
        return 'The security code was wrong or has expired. A new one has been loaded.';
      case 'VALIDATION_FAILED':
        return 'Enter your email or mobile number and your password.';
      case 'ACCOUNT_DISABLED':
        return 'This account has been disabled. Please contact us for help.';
      case 'ACCOUNT_TEMPORARILY_LOCKED':
        return 'This account is temporarily locked after several failed attempts. Try again later or reset your password.';
      case 'RATE_LIMIT_EXCEEDED':
        return 'Too many sign-in attempts. Please wait a minute and try again.';
      default:
        return 'Something went wrong while signing in. Please try again.';
    }
  }
}
