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
  readonly loading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
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
      password: ['', [CustomValidators.requiredNonEmpty, Validators.minLength(6)]]
    });
  }

  get f() {
    return this.loginForm.controls;
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
          }
        } else {
          this.errorMessage.set(response.message || 'Login failed. Please check credentials.');
        }
      },
      error: (err) => {
        this.loading.set(false);

        if (err.error && err.error.message) {
          if (err.error.message === 'INVALID_CREDENTIALS') {
            this.errorMessage.set('Invalid username or password.');
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
}
