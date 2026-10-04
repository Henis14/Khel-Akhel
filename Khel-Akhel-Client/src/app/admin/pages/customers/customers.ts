import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AdminCustomerService } from '../../services/admin-customer.service';
import { CustomerCreateRequest, CustomerResponse, CustomerUpdateRequest } from '../../models/customer.model';
import { CustomValidators } from '../../../common/validators/custom.validators';
import { REGEX_PATTERNS } from '../../../common/constants/regex.constants';
import { PaginationComponent } from '../../../common/components/pagination/pagination';
import { ConfirmationDialogComponent } from '../../../common/components/confirmation-dialog/confirmation-dialog';
import { BreadcrumbComponent } from '../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../common/models/breadcrumb.model';
import { AppLoaderComponent } from '../../../common/components/app-loader/app-loader';

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    PaginationComponent,
    ConfirmationDialogComponent,
    BreadcrumbComponent,
    AppLoaderComponent
  ],
  templateUrl: './customers.html',
  styleUrl: './customers.scss'
})
export class CustomersComponent implements OnInit {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Customers' }
  ];
  private customerService = inject(AdminCustomerService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  customers: CustomerResponse[] = [];
  totalRecords = 0;
  pageIndex = 1;
  pageSize = 10;
  searchQuery = '';

  loading = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  // Form Modal State
  isModalOpen = false;
  isEditMode = false;
  selectedCustomer: CustomerResponse | null = null;
  customerForm!: FormGroup;
  formSubmitting = false;

  // Confirmation Dialog State
  isConfirmOpen = false;
  confirmTitle = '';
  confirmMessage = '';
  isActionSubmitting = false;
  pendingAction: (() => void) | null = null;

  confirmDelete(customer: CustomerResponse): void {
    this.confirmTitle = 'Delete Customer';
    this.confirmMessage = `Are you sure you want to delete customer "${customer.firstName} ${customer.lastName}"? This action cannot be undone.`;
    this.pendingAction = () => {
      this.isActionSubmitting = true;
      this.customerService.delete(customer.encryptedId).subscribe({
        next: () => {
          this.isActionSubmitting = false;
          this.isConfirmOpen = false;
          this.successMessage = 'Customer deleted successfully.';
          this.loadCustomers();
        },
        error: (err) => {
          this.isActionSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to delete customer.';
        }
      });
    };
    this.isConfirmOpen = true;
  }

  onConfirmAction(): void {
    if (this.pendingAction) {
      this.pendingAction();
    }
  }

  onCancelAction(): void {
    if (!this.isActionSubmitting) {
      this.isConfirmOpen = false;
      this.pendingAction = null;
    }
  }

  ngOnInit(): void {
    this.initForm();
    this.loadCustomers();
  }

  initForm(): void {
    this.customerForm = this.fb.group({
      firstName: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.NAME, 'invalidName')]],
      lastName: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.NAME, 'invalidName')]],
      email: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.EMAIL, 'invalidEmail')]],
      mobileNo: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.MOBILE, 'invalidMobile')]],
      password: ['', [CustomValidators.requiredNonEmpty, CustomValidators.rawPattern(REGEX_PATTERNS.PASSWORD, 'invalidPassword')]]
    });
  }

  loadCustomers(): void {
    this.loading = true;
    this.errorMessage = null;

    this.customerService
      .getList({
        pageIndex: this.pageIndex,
        pageSize: this.pageSize,
        search: this.searchQuery.trim() || undefined
      })
      .subscribe({
        next: (res) => {
          this.loading = false;
          if (res.success && res.data) {
            this.customers = res.data.customers || [];
            this.totalRecords = res.data.totalRecords || 0;
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.loading = false;
          this.errorMessage = err.error?.message || 'Failed to load customers.';
          this.cdr.markForCheck();
        }
      });
  }

  onSearch(term: string): void {
    this.searchQuery = term;
    this.pageIndex = 1;
    this.loadCustomers();
  }

  onPageChange(newPage: number): void {
    this.pageIndex = newPage;
    this.loadCustomers();
  }

  openCreateModal(): void {
    this.isEditMode = false;
    this.selectedCustomer = null;
    this.customerForm.reset();
    this.customerForm.get('password')?.setValidators([CustomValidators.requiredNonEmpty, CustomValidators.rawPattern(REGEX_PATTERNS.PASSWORD, 'invalidPassword')]);
    this.customerForm.get('password')?.updateValueAndValidity();
    this.isModalOpen = true;
  }

  openEditModal(customer: CustomerResponse): void {
    this.isEditMode = true;
    this.selectedCustomer = customer;
    this.customerForm.patchValue({
      firstName: customer.firstName,
      lastName: customer.lastName,
      email: customer.email,
      mobileNo: customer.mobileNo,
      password: ''
    });
    this.customerForm.get('password')?.clearValidators();
    this.customerForm.get('password')?.updateValueAndValidity();
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  saveCustomer(): void {
    if (this.customerForm.invalid) {
      this.customerForm.markAllAsTouched();
      return;
    }

    this.formSubmitting = true;
    this.errorMessage = null;
    this.successMessage = null;

    if (this.isEditMode && this.selectedCustomer) {
      const updatePayload: CustomerUpdateRequest = {
        firstName: this.customerForm.value.firstName.trim(),
        lastName: this.customerForm.value.lastName.trim(),
        email: this.customerForm.value.email.trim(),
        mobileNo: this.customerForm.value.mobileNo.trim()
      };

      this.customerService.update(this.selectedCustomer.encryptedId, updatePayload).subscribe({
        next: (res) => {
          this.formSubmitting = false;
          if (res.success) {
            this.successMessage = 'Customer updated successfully.';
            this.closeModal();
            this.loadCustomers();
          } else {
            this.errorMessage = res.message;
          }
        },
        error: (err) => {
          this.formSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to update customer.';
        }
      });
    } else {
      const createPayload: CustomerCreateRequest = {
        firstName: this.customerForm.value.firstName.trim(),
        lastName: this.customerForm.value.lastName.trim(),
        email: this.customerForm.value.email.trim(),
        mobileNo: this.customerForm.value.mobileNo.trim(),
        password: this.customerForm.value.password
      };

      this.customerService.create(createPayload).subscribe({
        next: (res) => {
          this.formSubmitting = false;
          if (res.success) {
            this.successMessage = 'Customer created successfully.';
            this.closeModal();
            this.loadCustomers();
          } else {
            this.errorMessage = res.message;
          }
        },
        error: (err) => {
          this.formSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to create customer.';
        }
      });
    }
  }
}
