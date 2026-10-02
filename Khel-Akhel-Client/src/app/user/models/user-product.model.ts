export interface UserProductItem {
  encryptedProductId: string;
  productName: string;
  shortDescription: string;
  mrp: number;
  sellingPrice: number;
  isNewArrival: boolean;
  isBestSeller: boolean;
  availableQty: number;
  imagePaths: string[];
}
