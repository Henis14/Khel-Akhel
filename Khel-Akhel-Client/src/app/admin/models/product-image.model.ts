export interface ProductImage {
  encryptedImageId: string;
  encryptedProductId: string;
  imagePath: string;
  displayOrder: number;
  isDefault: boolean;
  isActive: boolean;
  createdDate: string;
}

export interface ProductImageUploadResponse {
  encryptedProductId: string;
  images: ProductImage[];
}

export interface SetDefaultImageRequest {
  encryptedImageId: string;
}
