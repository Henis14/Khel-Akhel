export interface ProductCategoryResponse {
  encryptedCategoryId: string;
  categoryName: string;
  description: string;
  displayOrder: number;
  isActive: boolean;
  createdDate: string;
}

export interface ProductCategoryCreateRequest {
  categoryName: string;
  description?: string;
  displayOrder: number;
}

export interface ProductCategoryUpdateRequest {
  encryptedCategoryId: string;
  categoryName?: string;
  description?: string;
  displayOrder?: number;
  isActive?: boolean;
}

export interface ProductSubCategoryResponse {
  encryptedSubCategoryId: string;
  encryptedCategoryId: string;
  categoryName: string;
  subCategoryName: string;
  description: string;
  displayOrder: number;
  isActive: boolean;
  createdDate: string;
}

export interface ProductSubCategoryCreateRequest {
  encryptedCategoryId: string;
  subCategoryName: string;
  description?: string;
  displayOrder: number;
}

export interface ProductSubCategoryUpdateRequest {
  encryptedSubCategoryId: string;
  encryptedCategoryId?: string;
  subCategoryName?: string;
  description?: string;
  displayOrder?: number;
  isActive?: boolean;
}
