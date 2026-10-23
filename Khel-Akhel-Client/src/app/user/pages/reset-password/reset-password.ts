import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../common/services/auth.service';
import { CustomValidators } from '../../../common/validators/custom.validators';

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

  readonly token = signal<string | null>(null);
  readonly validating = signal<boolean>(true);
  readonly isValidToken = signal<boolean>(false);
  readonly loading = signal<boolean>(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal<boolean>(false);

  ngOnInit(): void {
    const rawToken = this.route.snapshot.queryParams['token'];
    this.token.set(rawToken || null);

    this.resetForm = this.fb.group({
      newPassword: [
        '',
        [
          CustomValidators.requiredNonEmpty,
          Validators.minLength(8),
          Validators.pattern(
            /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{8,}$/,
          ),
        ],
      ],
      confirmPassword: ['', [CustomValidators.requiredNonEmpty]],
    });

    if (!rawToken) {
      this.validating.set(false);
      this.isValidToken.set(false);
      return;
    }

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

  get f() {
    return this.resetForm.controls;
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  onSubmit(): void {
    if (this.resetForm.invalid || this.loading() || !this.token()) {
      this.resetForm.markAllAsTouched();
      return;
    }

    const { newPassword, confirmPassword } = this.resetForm.value;
    if (newPassword !== confirmPassword) {
      this.errorMessage.set('New password and confirm password do not match.');
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    this.authService.resetPassword(this.token()!, newPassword, confirmPassword).subscribe({
      next: () => {
        this.loading.set(false);
        this.successMessage.set(
          'Your password has been successfully reset. Redirecting you to sign in...',
        );
        setTimeout(() => {
          this.router.navigate(['/admin/login']);
        }, 2200);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.error?.message === 'INVALID_OR_EXPIRED_RESET_TOKEN') {
          this.isValidToken.set(false);
          this.errorMessage.set('This reset link has expired, is invalid, or was already used.');
        } else if (err.status === 429) {
          this.errorMessage.set('Too many attempts. Please try again later.');
        } else {
          this.errorMessage.set(
            err.error?.errors?.[0] || 'Failed to reset password. Please contact the studio.',
          );
        }
      },
    });
  }
}
