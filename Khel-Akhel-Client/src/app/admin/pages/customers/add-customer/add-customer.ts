import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminCustomerService } from '../../../services/admin-customer.service';
import { CustomValidators } from '../../../../common/validators/custom.validators';
import { REGEX_PATTERNS } from '../../../../common/constants/regex.constants';
import { BreadcrumbComponent } from '../../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../../common/models/breadcrumb.model';

@Component({
  selector: 'app-add-customer',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, BreadcrumbComponent],
  templateUrl: './add-customer.html',
  styleUrl: './add-customer.scss'
})
export class AddCustomerComponent implements OnInit {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Customers', url: '/admin/customers' },
    { label: 'Add Customer' }
  ];
  private fb = inject(FormBuilder);
  private customerService = inject(AdminCustomerService);
  private router = inject(Router);

  customerForm!: FormGroup;
  loading = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  generatedAccountNo: string | null = null;

  showPassword = false;
  showConfirmPassword = false;

  ngOnInit(): void {
    this.customerForm = this.fb.group({
      firstName: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.NAME, 'invalidName')]],
      lastName: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.NAME, 'invalidName')]],
      email: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.EMAIL, 'invalidEmail')]],
      mobileNo: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.MOBILE, 'invalidMobile')]],
      password: ['', [CustomValidators.requiredNonEmpty, CustomValidators.rawPattern(REGEX_PATTERNS.PASSWORD, 'invalidPassword')]],
      confirmPassword: ['', [CustomValidators.requiredNonEmpty]]
    }, {
      validators: [this.passwordsMatchValidator]
    });
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
    return this.customerForm.controls;
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  onSubmit(): void {
    if (this.customerForm.invalid) {
      this.customerForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = null;
    this.successMessage = null;
    this.generatedAccountNo = null;

    const payload = {
      firstName: this.customerForm.value.firstName.trim(),
      lastName: this.customerForm.value.lastName.trim(),
      email: this.customerForm.value.email.trim(),
      mobileNo: this.customerForm.value.mobileNo.trim(),
      password: this.customerForm.value.password
    };

    this.customerService.create(payload).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success && res.data) {
          this.generatedAccountNo = res.data.accountNo || 'DRC000001';
          this.successMessage = `Customer account created successfully! Account No: ${this.generatedAccountNo}`;
          this.customerForm.reset();
        } else {
          this.errorMessage = res.message || 'Failed to create customer.';
        }
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Failed to create customer. Please check input details.';
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/customers']);
  }
}
