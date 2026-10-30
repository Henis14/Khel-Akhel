import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../common/services/auth.service';
import { CustomValidators } from '../../../common/validators/custom.validators';
import { REGEX_PATTERNS } from '../../../common/constants/regex.constants';

/**
 * Reset Password.
 *  /reset-password              → change an existing password: email + mobile must match the account
 *                                 and the existing password must be correct.
 *  /reset-password?token=…      → admin-issued reset link (unchanged behaviour): new password only.
 */
@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.scss',
})
export class ResetPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  resetForm!: FormGroup;

  /** Present only when the page was opened from an admin-issued reset link. */
  readonly token = signal<string | null>(null);
  readonly validating = signal<boolean>(false);
  readonly isValidToken = signal<boolean>(false);
  readonly loading = signal<boolean>(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal<boolean>(false);

  ngOnInit(): void {
    const rawToken = this.route.snapshot.queryParams['token'] || null;
    this.token.set(rawToken);

    this.resetForm = this.fb.group({
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
      currentPassword: ['', [CustomValidators.requiredNonEmpty]],
      newPassword: [
        '',
        [
          CustomValidators.requiredNonEmpty,
          CustomValidators.rawPattern(REGEX_PATTERNS.PASSWORD, 'invalidPassword'),
        ],
      ],
      confirmPassword: ['', [CustomValidators.requiredNonEmpty]],
    });

    // Signed-in customer changing their password: their email is already known
    const signedInEmail = this.authService.currentUser()?.email;
    if (!rawToken && signedInEmail) {
      this.resetForm.patchValue({ email: signedInEmail });
    }

    if (rawToken) {
      // Admin-issued link: identity was already verified by the studio — only a new password is needed
      ['email', 'mobileNo', 'currentPassword'].forEach((c) => this.resetForm.get(c)?.disable());
      this.validating.set(true);
      this.authService.validateResetToken(rawToken).subscribe({
        next: (res) => {
          this.validating.set(false);
          this.isValidToken.set(res.success && res.data?.valid === true);
        },
        error: () => {
          this.validating.set(false);
          this.isValidToken.set(false);
        },
      });
    }
  }

  get f() {
    return this.resetForm.controls;
  }

  get passwordsMismatch(): boolean {
    const { newPassword, confirmPassword } = this.resetForm.getRawValue();
    return !!confirmPassword && newPassword !== confirmPassword;
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  onSubmit(): void {
    if (this.resetForm.invalid || this.passwordsMismatch || this.loading()) {
      this.resetForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const v = this.resetForm.getRawValue();
    const request$ = this.token()
      ? this.authService.resetPassword(this.token()!, v.newPassword, v.confirmPassword)
      : this.authService.changePassword({
          email: v.email.trim(),
          mobileNo: v.mobileNo.trim(),
          currentPassword: v.currentPassword,
          newPassword: v.newPassword,
          confirmPassword: v.confirmPassword,
        });

    request$.subscribe({
      next: (res) => {
        this.loading.set(false);
        // the server signs the account out everywhere — start a fresh session
        this.authService.clearSession();
        this.successMessage.set(
          res.data || 'Your password has been changed. Please sign in with your new password.',
        );
        setTimeout(() => this.router.navigate(['/login']), 2500);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.error?.message === 'INVALID_OR_EXPIRED_RESET_TOKEN') {
          this.isValidToken.set(false);
        } else if (err.status === 429) {
          this.errorMessage.set('Too many attempts. Please try again later.');
        } else {
          this.errorMessage.set(
            err.error?.errors?.[0] || 'We could not change your password. Please try again.',
          );
        }
      },
    });
  }
}
