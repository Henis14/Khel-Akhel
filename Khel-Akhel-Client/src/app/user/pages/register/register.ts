import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../../common/services/api.service';
import { CustomValidators } from '../../../common/validators/custom.validators';
import { REGEX_PATTERNS } from '../../../common/constants/regex.constants';

@Component({
  selector: 'app-user-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class UserRegisterComponent implements OnInit {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private router = inject(Router);

  registerForm!: FormGroup;
  loading = false;
  errorMessage: string | null = null;

  ngOnInit(): void {
    this.registerForm = this.fb.group({
      firstName: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.NAME, 'invalidName')]],
      lastName: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.NAME, 'invalidName')]],
      email: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.EMAIL, 'invalidEmail')]],
      mobileNo: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.MOBILE, 'invalidMobile')]],
      password: ['', [CustomValidators.requiredNonEmpty, CustomValidators.rawPattern(REGEX_PATTERNS.PASSWORD, 'invalidPassword')]]
    });
  }

  get f() {
    return this.registerForm.controls;
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = null;

    const payload = {
      firstName: this.registerForm.value.firstName.trim(),
      lastName: this.registerForm.value.lastName.trim(),
      email: this.registerForm.value.email.trim(),
      mobileNo: this.registerForm.value.mobileNo.trim(),
      password: this.registerForm.value.password
    };

    this.api.post('customer/create', payload).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success) {
          this.router.navigate(['/login']);
        } else {
          this.errorMessage = res.message || 'Registration failed.';
        }
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Registration failed. Please check details.';
      }
    });
  }
}
