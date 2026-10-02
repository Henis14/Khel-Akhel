export interface CustomerResponse {
  encryptedId: string;
  accountNo: string;
  firstName: string;
  lastName: string;
  email: string;
  mobileNo: string;
  isAdmin: boolean;
  isEmailVerified: boolean;
  isMobileVerified: boolean;
  isActive: boolean;
  createdDate: string;
}

export interface CustomerCreateRequest {
  firstName: string;
  lastName: string;
  email: string;
  mobileNo: string;
  password: string;
}

export interface CustomerUpdateRequest {
  firstName?: string;
  lastName?: string;
  email?: string;
  mobileNo?: string;
}

export interface CustomerListRequest {
  pageIndex: number;
  pageSize: number;
  search?: string;
  isActive?: boolean;
}

export interface CustomerListResponse {
  customers: CustomerResponse[];
  totalRecords: number;
  pageIndex: number;
  pageSize: number;
}
