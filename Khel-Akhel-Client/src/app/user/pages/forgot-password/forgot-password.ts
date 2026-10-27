import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../common/services/auth.service';
import { CustomValidators } from '../../../common/validators/custom.validators';
import { REGEX_PATTERNS } from '../../../common/constants/regex.constants';

/**
 * Forgot Password — the customer enters their registered email + mobile number (both must match
 * the account), a new password and the CAPTCHA. The password is updated straight away.
 */
@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.scss',
})
export class ForgotPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  forgotForm!: FormGroup;

  readonly loading = signal<boolean>(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal<boolean>(false);

  readonly captchaId = signal<string | null>(null);
  readonly captchaImage = signal<string | null>(null);
  readonly captchaLoading = signal<boolean>(false);

  ngOnInit(): void {
    this.forgotForm = this.fb.group({
      email: [
        '',
        [
          CustomValidators.requiredNonEmpty,
          CustomValidators.pattern(REGEX_PATTERNS.EMAIL, 'invalidEmail'),
        ],
      ],
      mobileNo: [
        '',
        [
          CustomValidators.requiredNonEmpty,
          CustomValidators.pattern(REGEX_PATTERNS.MOBILE, 'invalidMobile'),
        ],
      ],
      newPassword: [
        '',
        [
          CustomValidators.requiredNonEmpty,
          CustomValidators.rawPattern(REGEX_PATTERNS.PASSWORD, 'invalidPassword'),
        ],
      ],
      confirmPassword: ['', [CustomValidators.requiredNonEmpty]],
      captcha: [
        '',
        [CustomValidators.requiredNonEmpty, Validators.minLength(5), Validators.maxLength(5)],
      ],
    });

    this.loadCaptcha();
  }

  get f() {
    return this.forgotForm.controls;
  }

  get passwordsMismatch(): boolean {
    const { newPassword, confirmPassword } = this.forgotForm.value;
    return !!confirmPassword && newPassword !== confirmPassword;
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  loadCaptcha(): void {
    this.captchaLoading.set(true);
    this.authService.getCaptcha().subscribe({
      next: (res) => {
        this.captchaLoading.set(false);
        if (res.success && res.data) {
          this.captchaId.set(res.data.captchaId);
          this.captchaImage.set(res.data.captchaImage);
          this.forgotForm.patchValue({ captcha: '' });
          this.forgotForm.controls['captcha'].markAsUntouched();
        }
      },
      error: () => {
        this.captchaLoading.set(false);
      },
    });
  }

  onSubmit(): void {
    if (this.forgotForm.invalid || this.passwordsMismatch || this.loading()) {
      this.forgotForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const v = this.forgotForm.value;
    this.authService
      .forgotPassword({
        email: v.email.trim(),
        mobileNo: v.mobileNo.trim(),
        newPassword: v.newPassword,
        confirmPassword: v.confirmPassword,
        captchaId: this.captchaId() ?? undefined,
        captcha: v.captcha.trim(),
      })
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          this.successMessage.set(
            res.data || 'Your password has been reset. Please sign in with your new password.',
          );
          setTimeout(() => this.router.navigate(['/login']), 2500);
        },
        error: (err) => {
          this.loading.set(false);
          if (err.status === 429) {
            this.errorMessage.set('Too many attempts. Please try again later.');
          } else {
            this.errorMessage.set(
              err.error?.errors?.[0] || 'We could not reset your password. Please try again.',
            );
          }
          // a CAPTCHA can only be used once — always fetch a fresh one after a failed attempt
          this.loadCaptcha();
        },
      });
  }
}
