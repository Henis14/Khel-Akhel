import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import { ProductListResponse, ProductResponse } from '../../admin/models/product.model';

@Injectable({
  providedIn: 'root'
})
export class UserProductService {
  constructor(private api: ApiService) {}

  getPublicProducts(search?: string, categoryId?: string): Observable<ApiResponse<ProductListResponse>> {
    return this.api.post<ProductListResponse>('product/search', {
      pageIndex: 1,
      pageSize: 20,
      search: search || undefined,
      encryptedCategoryId: categoryId || undefined,
      isActive: true
    });
  }

  getProductById(encryptedProductId: string): Observable<ApiResponse<ProductResponse>> {
    return this.api.get<ProductResponse>(`product/${encryptedProductId}`);
  }
}
