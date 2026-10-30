import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserAddressService } from '../../../services/user-address.service';
import { LocationService } from '../../../../common/services/location.service';
import { AuthService } from '../../../../common/services/auth.service';
import { ConfirmationDialogComponent } from '../../../../common/components/confirmation-dialog/confirmation-dialog';
import { CustomValidators } from '../../../../common/validators/custom.validators';
import { REGEX_PATTERNS } from '../../../../common/constants/regex.constants';
import {
  CustomerAddressCreateRequest,
  CustomerAddressResponse,
  CustomerAddressUpdateRequest,
} from '../../../../admin/models/customer-address.model';
import { CountryResponse, StateResponse } from '../../../../common/models/location.model';

@Component({
  selector: 'app-manage-addresses',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ConfirmationDialogComponent],
  templateUrl: './manage-addresses.html',
  styleUrl: './manage-addresses.scss',
})
export class ManageAddresses implements OnInit {
  private fb = inject(FormBuilder);
  private addressService = inject(UserAddressService);
  private locationService = inject(LocationService);
  public authService = inject(AuthService);

  readonly addresses = signal<CustomerAddressResponse[]>([]);
  readonly loading = signal<boolean>(true);
  readonly submitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // Modal State
  readonly isModalOpen = signal<boolean>(false);
  readonly isEditing = signal<boolean>(false);
  readonly editingAddressId = signal<string | null>(null);

  // Location Dropdown State
  readonly countries = signal<CountryResponse[]>([]);
  readonly states = signal<StateResponse[]>([]);
  readonly statesLoading = signal<boolean>(false);

  // Delete Dialog State
  readonly isConfirmOpen = signal<boolean>(false);
  readonly isDeleting = signal<boolean>(false);
  readonly pendingDeleteId = signal<string | null>(null);

  addressForm!: FormGroup;

  ngOnInit(): void {
    this.initForm();
    this.loadAddresses();
    this.loadCountries();
  }

  private initForm(): void {
    this.addressForm = this.fb.group({
      addressTitle: ['Home', [Validators.maxLength(50)]],
      addressType: ['Shipping', [Validators.required]],
      fullName: [
        '',
        [CustomValidators.requiredNonEmpty, Validators.minLength(2), Validators.maxLength(200)],
      ],
      mobileNo: [
        '',
        [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.MOBILE, 'invalidMobile')],
      ],
      addressLine1: [
        '',
        [CustomValidators.requiredNonEmpty, Validators.maxLength(255)],
      ],
      addressLine2: ['', [Validators.maxLength(255)]],
      landmark: ['', [Validators.maxLength(150)]],
      countryId: [null as number | null, [Validators.required, CustomValidators.positiveInteger]],
      stateId: [
        { value: null as number | null, disabled: true },
        [Validators.required, CustomValidators.positiveInteger],
      ],
      city: [
        '',
        [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.CITY, 'invalidCity')],
      ],
      pincode: [
        '',
        [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.PINCODE, 'invalidPincode')],
      ],
      isDefault: [false],
    });
  }

  get f() {
    return this.addressForm.controls;
  }

  loadAddresses(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.addressService.getMyAddresses().subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success && res.data) {
          this.addresses.set(res.data);
        } else {
          this.addresses.set([]);
          if (res.message) {
            this.errorMessage.set(res.message);
          }
        }
      },
      error: (err) => {
        this.loading.set(false);
        const msg = err.error?.message || 'Failed to load saved addresses. Please try again.';
        this.errorMessage.set(msg);
      },
    });
  }

  private loadCountries(): void {
    this.locationService.getCountries().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.countries.set(res.data);
          // If in add mode and India is present, select India by default (usually id=1)
          if (!this.isEditing() && res.data.length > 0) {
            const india = res.data.find((c) => c.countryName.toLowerCase() === 'india') || res.data[0];
            this.addressForm.patchValue({ countryId: india.id });
            this.loadStates(india.id);
          }
        }
      },
      error: () => {},
    });
  }

  onCountryChange(): void {
    const countryId = Number(this.addressForm.get('countryId')?.value);
    this.addressForm.patchValue({ stateId: null });
    this.addressForm.get('stateId')?.disable();
    this.states.set([]);
    if (countryId > 0) {
      this.loadStates(countryId);
    }
  }

  private loadStates(countryId: number, selectedStateId?: number | null): void {
    this.statesLoading.set(true);
    this.addressForm.get('stateId')?.disable();
    this.locationService.getStates(countryId).subscribe({
      next: (res) => {
        this.statesLoading.set(false);
        if (res.success && res.data && res.data.length > 0) {
          this.states.set(res.data);
          this.addressForm.get('stateId')?.enable();
          if (selectedStateId) {
            this.addressForm.patchValue({ stateId: selectedStateId });
          }
        } else {
          this.states.set([]);
          this.addressForm.get('stateId')?.disable();
        }
      },
      error: () => {
        this.statesLoading.set(false);
        this.states.set([]);
        this.addressForm.get('stateId')?.disable();
      },
    });
  }

  openAddModal(): void {
    this.isEditing.set(false);
    this.editingAddressId.set(null);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.addressForm.reset({
      addressTitle: 'Home',
      addressType: 'Shipping',
      fullName: '',
      mobileNo: '',
      addressLine1: '',
      addressLine2: '',
      landmark: '',
      countryId: null,
      stateId: null,
      city: '',
      pincode: '',
      isDefault: this.addresses().length === 0, // Default if first address
    });
    this.addressForm.get('stateId')?.disable();

    const countries = this.countries();
    if (countries.length > 0) {
      const india = countries.find((c) => c.countryName.toLowerCase() === 'india') || countries[0];
      this.addressForm.patchValue({ countryId: india.id });
      this.loadStates(india.id);
    }

    this.isModalOpen.set(true);
  }

  openEditModal(address: CustomerAddressResponse): void {
    this.isEditing.set(true);
    this.editingAddressId.set(address.encryptedAddressId);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.addressForm.patchValue({
      addressTitle: address.addressTitle || 'Home',
      addressType: address.addressType || 'Shipping',
      fullName: address.fullName,
      mobileNo: address.mobileNo,
      addressLine1: address.addressLine1,
      addressLine2: address.addressLine2 || '',
      landmark: address.landmark || '',
      countryId: address.countryId || null,
      city: address.city,
      pincode: address.pincode,
      isDefault: address.isDefault,
    });

    if (address.countryId) {
      this.loadStates(address.countryId, address.stateId);
    }

    this.isModalOpen.set(true);
  }

  closeModal(): void {
    if (this.submitting()) return;
    this.isModalOpen.set(false);
    this.isEditing.set(false);
    this.editingAddressId.set(null);
  }

  onSubmit(): void {
    if (this.addressForm.invalid) {
      this.addressForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const val = this.addressForm.getRawValue();
    const selectedCountry = this.countries().find((c) => c.id === Number(val.countryId));
    const selectedState = this.states().find((s) => s.id === Number(val.stateId));

    if (this.isEditing() && this.editingAddressId()) {
      const updateReq: CustomerAddressUpdateRequest = {
        encryptedAddressId: this.editingAddressId()!,
        addressTitle: val.addressTitle?.trim() || 'Home',
        addressType: val.addressType || 'Shipping',
        fullName: val.fullName.trim(),
        mobileNo: val.mobileNo.trim(),
        addressLine1: val.addressLine1.trim(),
        addressLine2: val.addressLine2?.trim() || '',
        landmark: val.landmark?.trim() || '',
        countryId: Number(val.countryId),
        stateId: Number(val.stateId),
        country: selectedCountry?.countryName || '',
        state: selectedState?.stateName || '',
        city: val.city.trim(),
        pincode: val.pincode.trim(),
        isDefault: !!val.isDefault,
      };

      this.addressService.updateAddress(updateReq).subscribe({
        next: (res) => {
          this.submitting.set(false);
          if (res.success) {
            this.successMessage.set('Address updated successfully.');
            this.closeModal();
            this.loadAddresses();
          } else {
            this.errorMessage.set(res.message || 'Failed to update address.');
          }
        },
        error: (err) => {
          this.submitting.set(false);
          const errList = err.error?.errors;
          const msg =
            errList && errList.length > 0
              ? errList.join(' ')
              : err.error?.message || 'Failed to update address. Please check input.';
          this.errorMessage.set(msg);
        },
      });
    } else {
      const createReq: CustomerAddressCreateRequest = {
        addressTitle: val.addressTitle?.trim() || 'Home',
        addressType: val.addressType || 'Shipping',
        fullName: val.fullName.trim(),
        mobileNo: val.mobileNo.trim(),
        addressLine1: val.addressLine1.trim(),
        addressLine2: val.addressLine2?.trim() || '',
        landmark: val.landmark?.trim() || '',
        countryId: Number(val.countryId),
        stateId: Number(val.stateId),
        country: selectedCountry?.countryName || '',
        state: selectedState?.stateName || '',
        city: val.city.trim(),
        pincode: val.pincode.trim(),
        isDefault: !!val.isDefault,
      };

      this.addressService.createAddress(createReq).subscribe({
        next: (res) => {
          this.submitting.set(false);
          if (res.success) {
            this.successMessage.set('Address added successfully.');
            this.closeModal();
            this.loadAddresses();
          } else {
            this.errorMessage.set(res.message || 'Failed to create address.');
          }
        },
        error: (err) => {
          this.submitting.set(false);
          const errList = err.error?.errors;
          const msg =
            errList && errList.length > 0
              ? errList.join(' ')
              : err.error?.message || 'Failed to add address. Please check input.';
          this.errorMessage.set(msg);
        },
      });
    }
  }

  setAsDefault(address: CustomerAddressResponse): void {
    if (address.isDefault || this.submitting()) return;

    this.submitting.set(true);
    const updateReq: CustomerAddressUpdateRequest = {
      encryptedAddressId: address.encryptedAddressId,
      isDefault: true,
    };

    this.addressService.updateAddress(updateReq).subscribe({
      next: (res) => {
        this.submitting.set(false);
        if (res.success) {
          this.successMessage.set(`'${address.addressTitle || 'Address'}' is now your default address.`);
          this.loadAddresses();
        } else {
          this.errorMessage.set(res.message || 'Failed to set default address.');
        }
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to set default address.');
      },
    });
  }

  confirmDelete(encryptedAddressId: string): void {
    this.pendingDeleteId.set(encryptedAddressId);
    this.isConfirmOpen.set(true);
  }

  onDeleteConfirmed(): void {
    const id = this.pendingDeleteId();
    if (!id) return;

    this.isDeleting.set(true);
    this.addressService.deleteAddress(id).subscribe({
      next: (res) => {
        this.isDeleting.set(false);
        this.isConfirmOpen.set(false);
        this.pendingDeleteId.set(null);
        if (res.success) {
          this.successMessage.set('Address removed successfully.');
          this.loadAddresses();
        } else {
          this.errorMessage.set(res.message || 'Failed to delete address.');
        }
      },
      error: (err) => {
        this.isDeleting.set(false);
        this.isConfirmOpen.set(false);
        this.pendingDeleteId.set(null);
        this.errorMessage.set(err.error?.message || 'Failed to delete address.');
      },
    });
  }

  onDeleteCanceled(): void {
    this.isConfirmOpen.set(false);
    this.pendingDeleteId.set(null);
  }
}
