export interface CustomerAddressResponse {
  encryptedAddressId: string;
  addressTitle: string;
  addressType: string;
  fullName: string;
  mobileNo: string;
  addressLine1: string;
  addressLine2: string;
  landmark: string;
  countryId?: number;
  stateId?: number;
  city: string;
  state: string;
  country: string;
  pincode: string;
  isDefault: boolean;
  isActive: boolean;
  createdDate: string;
}

export interface CustomerAddressCreateRequest {
  addressTitle?: string;
  addressType?: string;
  fullName: string;
  mobileNo: string;
  addressLine1: string;
  addressLine2?: string;
  landmark?: string;
  countryId: number;
  stateId: number;
  city: string;
  state?: string;
  country?: string;
  pincode: string;
  isDefault: boolean;
}

export interface CustomerAddressUpdateRequest {
  encryptedAddressId: string;
  addressTitle?: string;
  addressType?: string;
  fullName?: string;
  mobileNo?: string;
  addressLine1?: string;
  addressLine2?: string;
  landmark?: string;
  countryId?: number;
  stateId?: number;
  city?: string;
  state?: string;
  country?: string;
  pincode?: string;
  isDefault?: boolean;
}
