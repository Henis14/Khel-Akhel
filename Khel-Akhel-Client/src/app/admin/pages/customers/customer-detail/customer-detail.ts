import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { AdminCustomerService } from '../../../services/admin-customer.service';
import { AdminCustomerAddressService } from '../../../services/admin-customer-address.service';
import { CustomerResponse, CustomerUpdateRequest } from '../../../models/customer.model';
import { CustomerAddressResponse } from '../../../models/customer-address.model';
import { CustomValidators } from '../../../../common/validators/custom.validators';
import { REGEX_PATTERNS } from '../../../../common/constants/regex.constants';
import { BreadcrumbComponent } from '../../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../../common/models/breadcrumb.model';
import { AppLoaderComponent } from '../../../../common/components/app-loader/app-loader';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, BreadcrumbComponent, AppLoaderComponent],
  templateUrl: './customer-detail.html',
  styleUrl: './customer-detail.scss'
})
export class CustomerDetailComponent implements OnInit {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Customers', url: '/admin/customers' },
    { label: 'Customer Details' }
  ];
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private customerService = inject(AdminCustomerService);
  private addressService = inject(AdminCustomerAddressService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);

  encryptedId: string | null = null;
  customer: CustomerResponse | null = null;
  addresses: CustomerAddressResponse[] = [];

  loading = true;
  addressesLoading = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  activeTab: 'overview' | 'addresses' | 'orders' | 'activity' = 'overview';

  // Edit Modal
  isEditModalOpen = false;
  editForm!: FormGroup;
  editSubmitting = false;

  ngOnInit(): void {
    this.initEditForm();
    this.encryptedId = this.route.snapshot.paramMap.get('encryptedId');

    if (this.encryptedId) {
      this.loadCustomerDetail();
      this.loadAddresses();
    } else {
      this.errorMessage = 'Invalid customer ID.';
      this.loading = false;
    }
  }

  parseDate(dateStr: string | null | undefined): Date | null {
    if (!dateStr) return null;
    const d = new Date(dateStr);
    if (!isNaN(d.getTime())) return d;
    
    // Parse DD/MM/YYYY HH:mm:ss or DD-MM-YYYY HH:mm:ss
    const parts = dateStr.split(/[\sT\/-]+/);
    if (parts.length >= 3) {
      const day = parseInt(parts[0], 10);
      const month = parseInt(parts[1], 10) - 1;
      const year = parseInt(parts[2], 10);
      let hours = 0, minutes = 0, seconds = 0;
      if (parts.length >= 4) hours = parseInt(parts[3], 10) || 0;
      if (parts.length >= 5) minutes = parseInt(parts[4], 10) || 0;
      if (parts.length >= 6) seconds = parseInt(parts[5], 10) || 0;
      const parsed = new Date(year, month, day, hours, minutes, seconds);
      if (!isNaN(parsed.getTime())) return parsed;
    }
    return null;
  }

  initEditForm(): void {
    this.editForm = this.fb.group({
      firstName: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.NAME, 'invalidName')]],
      lastName: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.NAME, 'invalidName')]],
      email: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.EMAIL, 'invalidEmail')]],
      mobileNo: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.MOBILE, 'invalidMobile')]]
    });
  }

  loadCustomerDetail(): void {
    if (!this.encryptedId) return;

    this.loading = true;
    this.errorMessage = null;

    this.customerService.getById(this.encryptedId).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success && res.data) {
          this.customer = res.data;
        } else {
          this.errorMessage = res.message || 'Customer record not found.';
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Failed to load customer details.';
        this.cdr.markForCheck();
      }
    });
  }

  loadAddresses(): void {
    this.addressesLoading = true;
    this.addressService.getList().subscribe({
      next: (res) => {
        this.addressesLoading = false;
        if (res.success) {
          this.addresses = res.data || [];
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.addressesLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  setTab(tab: 'overview' | 'addresses' | 'orders' | 'activity'): void {
    this.activeTab = tab;
  }

  openEditModal(): void {
    if (!this.customer) return;

    this.editForm.patchValue({
      firstName: this.customer.firstName,
      lastName: this.customer.lastName,
      email: this.customer.email,
      mobileNo: this.customer.mobileNo
    });
    this.isEditModalOpen = true;
  }

  closeEditModal(): void {
    this.isEditModalOpen = false;
  }

  saveCustomer(): void {
    if (this.editForm.invalid || !this.encryptedId) {
      this.editForm.markAllAsTouched();
      return;
    }

    this.editSubmitting = true;
    this.errorMessage = null;

    const payload: CustomerUpdateRequest = {
      firstName: this.editForm.value.firstName.trim(),
      lastName: this.editForm.value.lastName.trim(),
      email: this.editForm.value.email.trim(),
      mobileNo: this.editForm.value.mobileNo.trim()
    };

    this.customerService.update(this.encryptedId, payload).subscribe({
      next: (res) => {
        this.editSubmitting = false;
        if (res.success) {
          this.successMessage = 'Customer details updated successfully.';
          this.closeEditModal();
          this.loadCustomerDetail();
        } else {
          this.errorMessage = res.message;
        }
      },
      error: (err) => {
        this.editSubmitting = false;
        this.errorMessage = err.error?.message || 'Failed to update customer details.';
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/customers']);
  }
}
