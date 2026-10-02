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
