import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../common/services/auth.service';
import { CustomValidators } from '../../../common/validators/custom.validators';

@Component({
  selector: 'app-user-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class UserLoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  loginForm!: FormGroup;
  readonly loading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      Promise.resolve().then(() => {
        this.router.navigate(['/']);
      });
      return;
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

    this.authService.login({
      userName: this.loginForm.value.userName.trim(),
      password: this.loginForm.value.password.trim(),
      deviceInfo: navigator.userAgent
    }).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success && res.data?.token) {
          Promise.resolve().then(() => {
            this.router.navigate(['/']);
          });
        } else {
          this.errorMessage.set(res.message || 'Login failed.');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message || 'Invalid username or password.');
      }
    });
  }
}
