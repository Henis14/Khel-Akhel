import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../common/services/auth.service';
import { CustomValidators } from '../../../common/validators/custom.validators';

@Component({
  selector: 'app-admin-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class AdminLoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  loginForm!: FormGroup;
  forgotForm!: FormGroup;

  readonly loading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly showPassword = signal<boolean>(false);

  readonly captchaId = signal<string | null>(null);
  readonly captchaCode = signal<string | null>(null);
  readonly captchaLoading = signal<boolean>(false);

  readonly isForgotPasswordMode = signal<boolean>(false);
  readonly forgotLoading = signal<boolean>(false);
  readonly forgotSuccessMessage = signal<string | null>(null);
  readonly forgotErrorMessage = signal<string | null>(null);

  returnUrl = '/admin/dashboard';

  ngOnInit(): void {
    if (this.authService.isAuthenticated() && this.authService.isAdmin()) {
      Promise.resolve().then(() => {
        this.router.navigate(['/admin/dashboard']);
      });
      return;
    }

    const queryReturnUrl = this.route.snapshot.queryParams['returnUrl'];
    if (queryReturnUrl && !queryReturnUrl.includes('/login')) {
      this.returnUrl = queryReturnUrl;
    } else {
      this.returnUrl = '/admin/dashboard';
    }

    this.loginForm = this.fb.group({
      userName: ['', [CustomValidators.requiredNonEmpty]],
      password: ['', [CustomValidators.requiredNonEmpty, Validators.minLength(6)]],
      captcha: [
        '',
        [
          CustomValidators.requiredNonEmpty,
          Validators.minLength(5),
          Validators.maxLength(5),
          Validators.pattern(/^[a-zA-Z0-9]{5}$/)
        ]
      ]
    });

    this.forgotForm = this.fb.group({
      userName: ['', [CustomValidators.requiredNonEmpty]]
    });

    this.loadCaptcha();
  }

  get f() {
    return this.loginForm.controls;
  }

  get ff() {
    return this.forgotForm.controls;
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((visible) => !visible);
  }

  loadCaptcha(): void {
    this.captchaLoading.set(true);
    this.authService.getCaptcha().subscribe({
      next: (res) => {
        this.captchaLoading.set(false);
        if (res.success && res.data) {
          this.captchaId.set(res.data.captchaId);
          this.captchaCode.set(res.data.captcha);
          this.loginForm.patchValue({ captcha: '' });
          this.loginForm.controls['captcha'].markAsUntouched();
        }
      },
      error: () => {
        this.captchaLoading.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const payload = {
      userName: this.loginForm.value.userName.trim(),
      password: this.loginForm.value.password.trim(),
      captchaId: this.captchaId() || undefined,
      captcha: this.loginForm.value.captcha.trim(),
      deviceInfo: navigator.userAgent
    };

    this.authService.login(payload).subscribe({
      next: (response) => {
        this.loading.set(false);

        if (response.success && response.data?.token) {
          const isUserAdmin = this.authService.isAdmin();

          if (isUserAdmin) {
            this.errorMessage.set(null);
            let destination = this.returnUrl;
            if (!destination || destination.includes('/login')) {
              destination = '/admin/dashboard';
            }

            Promise.resolve().then(() => {
              this.router.navigateByUrl(destination).catch(() => {});
            });
          } else {
            this.errorMessage.set('Access denied. Account is not an administrator.');
            this.authService.clearSession();
            this.loadCaptcha();
          }
        } else {
          this.errorMessage.set(response.message || 'Login failed. Please check credentials.');
          this.loadCaptcha();
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.loadCaptcha();

        if (err.error && err.error.message) {
          if (err.error.message === 'INVALID_CREDENTIALS') {
            this.errorMessage.set('Invalid username or password.');
          } else if (err.error.message === 'INVALID_CAPTCHA') {
            this.errorMessage.set('Invalid or expired CAPTCHA. Please try again.');
          } else if (err.error.message === 'ACCOUNT_TEMPORARILY_LOCKED') {
            this.errorMessage.set('Account is temporarily locked due to failed attempts.');
          } else if (err.error.message === 'ACCOUNT_DISABLED') {
            this.errorMessage.set('Your account has been disabled.');
          } else {
            this.errorMessage.set(err.error.message);
          }
        } else {
          this.errorMessage.set('An unexpected error occurred during login. Please try again.');
        }
      }
    });
  }

  toggleForgotPasswordMode(show: boolean): void {
    this.isForgotPasswordMode.set(show);
    this.forgotSuccessMessage.set(null);
    this.forgotErrorMessage.set(null);

    if (show && this.loginForm.value.userName) {
      this.forgotForm.patchValue({ userName: this.loginForm.value.userName.trim() });
    }
  }

  onForgotPasswordSubmit(): void {
    if (this.forgotForm.invalid) {
      this.forgotForm.markAllAsTouched();
      return;
    }

    this.forgotLoading.set(true);
    this.forgotSuccessMessage.set(null);
    this.forgotErrorMessage.set(null);

    const userName = this.forgotForm.value.userName.trim();

    this.authService.forgotPassword(userName).subscribe({
      next: (res) => {
        this.forgotLoading.set(false);
        if (res.success) {
          this.forgotSuccessMessage.set(
            'If an account with this Email/Mobile exists, password reset instructions have been initiated.'
          );
        } else {
          this.forgotErrorMessage.set(res.message || 'Failed to process password reset request.');
        }
      },
      error: (err) => {
        this.forgotLoading.set(false);
        this.forgotErrorMessage.set(
          err.error?.message || 'An error occurred while requesting password reset. Please try again.'
        );
      }
    });
  }
}
