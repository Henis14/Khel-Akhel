import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../common/services/auth.service';
import { CustomValidators } from '../../../common/validators/custom.validators';
import { REGEX_PATTERNS } from '../../../common/constants/regex.constants';

@Component({
  selector: 'app-user-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class UserRegisterComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  registerForm!: FormGroup;

  readonly loading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly captchaId = signal<string | null>(null);
  readonly captchaImage = signal<string | null>(null);
  readonly captchaLoading = signal<boolean>(false);

  showPassword = signal<boolean>(false);
  showConfirmPassword = signal<boolean>(false);

  ngOnInit(): void {
    this.registerForm = this.fb.group(
      {
        firstName: [
          '',
          [
            CustomValidators.requiredNonEmpty,
            CustomValidators.pattern(REGEX_PATTERNS.NAME, 'invalidName'),
          ],
        ],
        lastName: [
          '',
          [
            CustomValidators.requiredNonEmpty,
            CustomValidators.pattern(REGEX_PATTERNS.NAME, 'invalidName'),
          ],
        ],
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
        password: [
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
        website: [''], // Honeypot field
      },
      {
        validators: [this.passwordsMatchValidator],
      },
    );

    this.loadCaptcha();
  }

  passwordsMatchValidator(group: FormGroup) {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    if (password && confirmPassword && password !== confirmPassword) {
      return { passwordMismatch: true };
    }
    return null;
  }

  get f() {
    return this.registerForm.controls;
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword.update((v) => !v);
  }

  loadCaptcha(): void {
    this.captchaLoading.set(true);
    this.authService.getCaptcha().subscribe({
      next: (res) => {
        this.captchaLoading.set(false);
        if (res.success && res.data) {
          this.captchaId.set(res.data.captchaId);
          this.captchaImage.set(res.data.captchaImage);
          this.registerForm.patchValue({ captcha: '' });
          this.registerForm.controls['captcha'].markAsUntouched();
        }
      },
      error: () => {
        this.captchaLoading.set(false);
      },
    });
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const payload = {
      firstName: this.registerForm.value.firstName.trim(),
      lastName: this.registerForm.value.lastName.trim(),
      email: this.registerForm.value.email.trim(),
      mobileNo: this.registerForm.value.mobileNo.trim(),
      password: this.registerForm.value.password,
      captchaId: this.captchaId() || undefined,
      captcha: this.registerForm.value.captcha.trim(),
      website: this.registerForm.value.website?.trim() || undefined,
    };

    this.authService.register(payload).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success) {
          this.successMessage.set(
            'Account created successfully! You can now sign in with your credentials.',
          );
          this.registerForm.reset();
        } else {
          this.errorMessage.set(res.message || 'Registration failed.');
          this.loadCaptcha();
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.loadCaptcha();
        if (err.error?.message === 'INVALID_CAPTCHA') {
          this.errorMessage.set('Invalid or expired CAPTCHA. Please try again.');
        } else if (err.error?.message === 'CUSTOMER_ALREADY_EXISTS') {
          this.errorMessage.set('An account with this email or mobile number already exists.');
        } else if (err.error?.errors?.length) {
          this.errorMessage.set(err.error.errors.join(' '));
        } else {
          this.errorMessage.set(
            err.error?.message || 'An error occurred during registration. Please try again.',
          );
        }
      },
    });
  }
}
