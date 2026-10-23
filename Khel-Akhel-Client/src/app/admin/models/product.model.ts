export interface ProductResponse {
  encryptedProductId: string;
  encryptedCategoryId: string;
  categoryName: string;
  encryptedSubCategoryId?: string | null;
  subCategoryName?: string | null;
  productName: string;
  productCode: string;
  sku: string;
  shortDescription: string;
  description: string;
  mrp: number;
  sellingPrice: number;
  isFeatured: boolean;
  isNewArrival: boolean;
  isBestSeller: boolean;
  isTrending: boolean;
  isCustomerFavourite: boolean;
  isActive: boolean;
  availableQty: number;
  imagePaths: string[];
  createdDate: string;
}

export interface ProductCreateRequest {
  encryptedCategoryId: string;
  encryptedSubCategoryId?: string | null;
  productName: string;
  productCode?: string;
  sku: string;
  shortDescription?: string;
  description?: string;
  mrp: number;
  sellingPrice: number;
  isFeatured: boolean;
  isNewArrival: boolean;
  isBestSeller: boolean;
  isTrending: boolean;
  isCustomerFavourite: boolean;
}

export interface ProductUpdateRequest {
  encryptedProductId: string;
  encryptedCategoryId?: string;
  encryptedSubCategoryId?: string | null;
  productName?: string;
  productCode?: string;
  sku?: string;
  shortDescription?: string;
  description?: string;
  mrp?: number;
  sellingPrice?: number;
  isFeatured?: boolean;
  isNewArrival?: boolean;
  isBestSeller?: boolean;
  isTrending?: boolean;
  isCustomerFavourite?: boolean;
  isActive?: boolean;
}

export interface ProductListRequest {
  pageIndex: number;
  pageSize: number;
  encryptedCategoryId?: string;
  encryptedSubCategoryId?: string;
  minPrice?: number;
  maxPrice?: number;
  isFeatured?: boolean;
  isNewArrival?: boolean;
  isBestSeller?: boolean;
  isTrending?: boolean;
  isCustomerFavourite?: boolean;
  isActive?: boolean;
  search?: string;
  sortBy?: string;
}

export interface ProductListResponse {
  products: ProductResponse[];
  totalRecords: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
}
