export interface ProductResponse {
  encryptedProductId: string;
  encryptedCategoryId: string;
  categoryName: string;
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
}
