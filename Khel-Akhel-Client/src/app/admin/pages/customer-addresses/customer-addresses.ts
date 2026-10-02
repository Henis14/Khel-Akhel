import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { AdminCustomerAddressService } from '../../services/admin-customer-address.service';
import { LocationService } from '../../../common/services/location.service';
import { CustomerAddressCreateRequest, CustomerAddressResponse, CustomerAddressUpdateRequest } from '../../models/customer-address.model';
import { CountryResponse, StateResponse } from '../../../common/models/location.model';
import { CustomValidators } from '../../../common/validators/custom.validators';
import { REGEX_PATTERNS } from '../../../common/constants/regex.constants';
import { ConfirmationDialogComponent } from '../../../common/components/confirmation-dialog/confirmation-dialog';
import { BreadcrumbComponent } from '../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../common/models/breadcrumb.model';
import { AppLoaderComponent } from '../../../common/components/app-loader/app-loader';

@Component({
  selector: 'app-customer-addresses',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ConfirmationDialogComponent, BreadcrumbComponent, AppLoaderComponent],
  templateUrl: './customer-addresses.html',
  styleUrl: './customer-addresses.scss'
})
export class CustomerAddressesComponent implements OnInit {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Customers', url: '/admin/customers' },
    { label: 'Customer Addresses' }
  ];
  private addressService = inject(AdminCustomerAddressService);
  private locationService = inject(LocationService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);

  addresses: CustomerAddressResponse[] = [];
  countries: CountryResponse[] = [];
  states: StateResponse[] = [];

  loading = false;
  countriesLoading = false;
  statesLoading = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  isModalOpen = false;
  isEditMode = false;
  selectedAddress: CustomerAddressResponse | null = null;
  addressForm!: FormGroup;
  formSubmitting = false;

  isConfirmOpen = false;
  confirmTitle = '';
  confirmMessage = '';
  pendingAction: (() => void) | null = null;

  ngOnInit(): void {
    this.initForm();
    queueMicrotask(() => {
      this.loadAddresses();
      this.loadCountries();
    });
  }

  initForm(): void {
    this.addressForm = this.fb.group({
      addressTitle: ['Home', [CustomValidators.requiredNonEmpty]],
      addressType: ['Shipping', [CustomValidators.requiredNonEmpty]],
      fullName: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(/^.{2,200}$/, 'invalidLength')]],
      mobileNo: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.MOBILE, 'invalidMobile')]],
      addressLine1: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(/^.{1,255}$/, 'invalidLine1')]],
      addressLine2: ['', [CustomValidators.pattern(/^[\s\S]{0,255}$/, 'invalidLine2')]],
      landmark: ['', [CustomValidators.pattern(/^[\s\S]{0,150}$/, 'invalidLandmark')]],
      countryId: ['', [CustomValidators.requiredNonEmpty]],
      stateId: ['', [CustomValidators.requiredNonEmpty]],
      city: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.CITY, 'invalidCity')]],
      pincode: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.PINCODE, 'invalidPincode')]],
      isDefault: [false]
    });
  }

  loadCountries(): void {
    this.countriesLoading = true;
    this.locationService.getCountries().subscribe({
      next: (res) => {
        this.countriesLoading = false;
        if (res.success) {
          this.countries = res.data || [];
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.countriesLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  loadAddresses(): void {
    this.loading = true;
    this.errorMessage = null;

    this.addressService.getList().subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success) {
          this.addresses = res.data || [];
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Failed to load customer addresses.';
        this.cdr.markForCheck();
      }
    });
  }

  onCountryChange(countryIdVal: string | number): void {
    const countryId = Number(countryIdVal);
    this.states = [];
    this.addressForm.patchValue({ stateId: '' });

    if (!countryId || isNaN(countryId)) {
      return;
    }

    this.statesLoading = true;
    this.locationService.getStates(countryId).subscribe({
      next: (res) => {
        this.statesLoading = false;
        if (res.success) {
          this.states = res.data || [];
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.statesLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  openCreateModal(): void {
    this.isEditMode = false;
    this.selectedAddress = null;
    this.states = [];
    this.addressForm.reset({
      addressTitle: 'Home',
      addressType: 'Shipping',
      countryId: '',
      stateId: '',
      isDefault: false
    });

    if (this.countries.length > 0) {
      const defaultIndia = this.countries.find(c => c.countryCode === 'IN') || this.countries[0];
      this.addressForm.patchValue({ countryId: defaultIndia.id });
      this.onCountryChange(defaultIndia.id);
    }
    this.isModalOpen = true;
  }

  openEditModal(address: CustomerAddressResponse): void {
    this.isEditMode = true;
    this.selectedAddress = address;

    this.addressForm.patchValue({
      addressTitle: address.addressTitle,
      addressType: address.addressType,
      fullName: address.fullName,
      mobileNo: address.mobileNo,
      addressLine1: address.addressLine1,
      addressLine2: address.addressLine2,
      landmark: address.landmark,
      city: address.city,
      pincode: address.pincode,
      isDefault: address.isDefault
    });

    if (address.countryId) {
      this.addressForm.patchValue({ countryId: address.countryId });
      this.statesLoading = true;
      this.locationService.getStates(address.countryId).subscribe({
        next: (res) => {
          this.statesLoading = false;
          if (res.success) {
            this.states = res.data || [];
            if (address.stateId) {
              this.addressForm.patchValue({ stateId: address.stateId });
            }
          }
          this.cdr.markForCheck();
        },
        error: () => {
          this.statesLoading = false;
          this.cdr.markForCheck();
        }
      });
    }

    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  saveAddress(): void {
    if (this.addressForm.invalid) {
      this.addressForm.markAllAsTouched();
      return;
    }

    this.formSubmitting = true;
    this.errorMessage = null;

    const selectedCountry = this.countries.find(c => c.id === Number(this.addressForm.value.countryId));
    const selectedState = this.states.find(s => s.id === Number(this.addressForm.value.stateId));

    if (this.isEditMode && this.selectedAddress) {
      const updatePayload: CustomerAddressUpdateRequest = {
        encryptedAddressId: this.selectedAddress.encryptedAddressId,
        addressTitle: this.addressForm.value.addressTitle.trim(),
        addressType: this.addressForm.value.addressType.trim(),
        fullName: this.addressForm.value.fullName.trim(),
        mobileNo: this.addressForm.value.mobileNo.trim(),
        addressLine1: this.addressForm.value.addressLine1.trim(),
        addressLine2: this.addressForm.value.addressLine2?.trim() || '',
        landmark: this.addressForm.value.landmark?.trim() || '',
        countryId: Number(this.addressForm.value.countryId),
        stateId: Number(this.addressForm.value.stateId),
        city: this.addressForm.value.city.trim(),
        state: selectedState?.stateName || '',
        country: selectedCountry?.countryName || '',
        pincode: this.addressForm.value.pincode.trim(),
        isDefault: !!this.addressForm.value.isDefault
      };

      this.addressService.update(updatePayload).subscribe({
        next: (res) => {
          this.formSubmitting = false;
          if (res.success) {
            this.successMessage = 'Address updated successfully.';
            this.closeModal();
            this.loadAddresses();
          } else {
            this.errorMessage = res.message;
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.formSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to update address.';
          this.cdr.markForCheck();
        }
      });
    } else {
      const createPayload: CustomerAddressCreateRequest = {
        addressTitle: this.addressForm.value.addressTitle.trim(),
        addressType: this.addressForm.value.addressType.trim(),
        fullName: this.addressForm.value.fullName.trim(),
        mobileNo: this.addressForm.value.mobileNo.trim(),
        addressLine1: this.addressForm.value.addressLine1.trim(),
        addressLine2: this.addressForm.value.addressLine2?.trim() || '',
        landmark: this.addressForm.value.landmark?.trim() || '',
        countryId: Number(this.addressForm.value.countryId),
        stateId: Number(this.addressForm.value.stateId),
        city: this.addressForm.value.city.trim(),
        state: selectedState?.stateName || '',
        country: selectedCountry?.countryName || '',
        pincode: this.addressForm.value.pincode.trim(),
        isDefault: !!this.addressForm.value.isDefault
      };

      this.addressService.create(createPayload).subscribe({
        next: (res) => {
          this.formSubmitting = false;
          if (res.success) {
            this.successMessage = 'Address created successfully.';
            this.closeModal();
            this.loadAddresses();
          } else {
            this.errorMessage = res.message;
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.formSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to create address.';
          this.cdr.markForCheck();
        }
      });
    }
  }

  isActionSubmitting = false;

  confirmDelete(address: CustomerAddressResponse): void {
    this.confirmTitle = 'Delete Address';
    this.confirmMessage = `Are you sure you want to delete address "${address.addressTitle} - ${address.city}"?`;
    this.pendingAction = () => {
      this.isActionSubmitting = true;
      this.addressService.delete(address.encryptedAddressId).subscribe({
        next: () => {
          this.isActionSubmitting = false;
          this.isConfirmOpen = false;
          this.successMessage = 'Address deleted successfully.';
          this.loadAddresses();
        },
        error: (err) => {
          this.isActionSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to delete address.';
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
}
