export interface CommissionCategory {
  id: number;
  encryptedCategoryId: string;
  categoryName: string;
  description?: string | null;
  displayOrder: number;
  isActive: boolean;
}

export interface CommissionImage {
  encryptedImageId: string;
  originalFileName: string;
  storedFileName: string;
  imageUrl: string;
  fileSize: number;
  contentType: string;
  displayOrder: number;
}

export interface CommissionRequestListItem {
  encryptedRequestId: string;
  requestNo: string;
  fullName: string;
  mobileNo: string;
  email: string;
  categoryId: number;
  encryptedCategoryId: string;
  categoryName: string;
  otherCategoryDescription?: string | null;
  expectedTimeline: string;
  projectLocation: string;
  status: string;
  imageCount: number;
  thumbnailUrl?: string | null;
  createdDate: string;
}

export interface CommissionListFilter {
  pageIndex?: number;
  pageSize?: number;
  search?: string;
  categoryId?: number;
  encryptedCategoryId?: string;
  status?: string;
  startDate?: string;
  endDate?: string;
  sortBy?: string;
}

export interface CommissionListResponse {
  items: CommissionRequestListItem[];
  totalRecords: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
}

export interface CommissionRequestDetail {
  encryptedRequestId: string;
  requestNo: string;
  fullName: string;
  mobileNo: string;
  email: string;
  categoryId: number;
  encryptedCategoryId: string;
  categoryName: string;
  otherCategoryDescription?: string | null;
  expectedTimeline: string;
  projectLocation: string;
  projectDescription: string;
  wordCount: number;
  keywords?: string | null;
  status: string;
  adminNotes?: string | null;
  images: CommissionImage[];
  createdDate: string;
  modifiedDate?: string | null;
}

export interface CommissionStatusUpdateRequest {
  encryptedRequestId: string;
  status: string;
  adminNotes?: string | null;
}

export type CommissionStatus =
  | 'Submitted'
  | 'Under Review'
  | 'Contacted'
  | 'In Progress'
  | 'Completed'
  | 'Rejected'
  | 'Cancelled';

export const COMMISSION_STATUSES: CommissionStatus[] = [
  'Submitted',
  'Under Review',
  'Contacted',
  'In Progress',
  'Completed',
  'Rejected',
  'Cancelled',
];
