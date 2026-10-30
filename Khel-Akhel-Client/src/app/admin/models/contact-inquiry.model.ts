export interface ContactInquiryItem {
  encryptedInquiryId: string;
  fullName: string;
  mobileNumber: string;
  email: string;
  companyName?: string | null;
  serviceDescription: string;
  customerAccountNo?: string | null;
  ipAddress?: string | null;
  userAgent?: string | null;
  isActive: boolean;
  createdDate: string;
  modifiedDate?: string | null;
}

export interface ContactInquiryListFilter {
  pageIndex?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
  startDate?: string;
  endDate?: string;
  sortBy?: string;
}

export interface ContactInquiryListResponse {
  items: ContactInquiryItem[];
  totalRecords: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
}

export interface ContactInquirySummary {
  totalInquiries: number;
  activeInquiries: number;
  inactiveInquiries: number;
  todayInquiries: number;
}
