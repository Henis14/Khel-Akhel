import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../common/services/auth.service';
import { CustomValidators } from '../../../common/validators/custom.validators';

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
  readonly submitted = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly captchaId = signal<string | null>(null);
  readonly captchaImage = signal<string | null>(null);
  readonly captchaLoading = signal<boolean>(false);

  ngOnInit(): void {
    this.forgotForm = this.fb.group({
      userName: ['', [CustomValidators.requiredNonEmpty]],
      captcha: [
        '',
        [
          CustomValidators.requiredNonEmpty,
          Validators.minLength(5),
          Validators.maxLength(5),
        ],
      ],
    });

    this.loadCaptcha();
  }

  get f() {
    return this.forgotForm.controls;
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
    if (this.forgotForm.invalid || this.loading()) {
      this.forgotForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const userName = this.forgotForm.value.userName.trim();
    const captchaCode = this.forgotForm.value.captcha.trim();
    const captchaId = this.captchaId() ?? undefined;

    this.authService.forgotPassword(userName, captchaId, captchaCode).subscribe({
      next: () => {
        this.loading.set(false);
        this.submitted.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.error?.message === 'INVALID_CAPTCHA') {
          this.errorMessage.set('Invalid or expired CAPTCHA code. Please try again.');
          this.loadCaptcha();
        } else if (err.status === 429) {
          this.errorMessage.set('Too many attempts. Please try again later.');
        } else {
          // Even on unexpected non-captcha errors, protect user enumeration
          this.submitted.set(true);
        }
      },
    });
  }
}
