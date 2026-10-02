import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import { ProductImage, ProductImageUploadResponse } from '../models/product-image.model';

@Injectable({
  providedIn: 'root'
})
export class AdminProductImageService {
  constructor(private api: ApiService) {}

  uploadImages(encryptedProductId: string, files: File[]): Observable<ApiResponse<ProductImageUploadResponse>> {
    const formData = new FormData();
    formData.append('EncryptedProductId', encryptedProductId);
    files.forEach((file) => {
      formData.append('Images', file);
    });

    return this.api.post<ProductImageUploadResponse>('product-image/upload', formData);
  }

  getImages(encryptedProductId: string): Observable<ApiResponse<ProductImage[]>> {
    return this.api.get<ProductImage[]>(`product-image/${encryptedProductId}`);
  }

  setDefault(encryptedImageId: string): Observable<ApiResponse<any>> {
    return this.api.patch<any>('product-image/set-default', { encryptedImageId });
  }

  deleteImage(encryptedImageId: string): Observable<ApiResponse<any>> {
    return this.api.delete<any>(`product-image/${encryptedImageId}`);
  }
}
