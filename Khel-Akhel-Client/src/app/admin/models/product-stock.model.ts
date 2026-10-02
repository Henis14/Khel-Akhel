export interface ProductStockResponse {
  encryptedStockId: string;
  encryptedProductId: string;
  productName: string;
  productCode: string;
  sku: string;
  availableQty: number;
  reservedQty: number;
  isActive: boolean;
  createdDate: string;
}

export interface ProductStockAddRequest {
  encryptedProductId: string;
  quantity: number;
}

export interface ProductStockUpdateRequest {
  encryptedProductId: string;
  availableQuantity: number;
  reservedQuantity: number;
}

export interface ProductStockListRequest {
  pageIndex: number;
  pageSize: number;
  search?: string;
  lowStockOnly?: boolean;
}

export interface ProductStockListResponse {
  stocks: ProductStockResponse[];
  totalRecords: number;
  pageIndex: number;
  pageSize: number;
}
