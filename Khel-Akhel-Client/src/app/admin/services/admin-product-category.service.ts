import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import {
  ProductCategoryCreateRequest,
  ProductCategoryResponse,
  ProductCategoryUpdateRequest
} from '../models/product-category.model';

@Injectable({
  providedIn: 'root'
})
export class AdminProductCategoryService {
  constructor(private api: ApiService) {}

  getList(): Observable<ApiResponse<ProductCategoryResponse[]>> {
    return this.api.get<ProductCategoryResponse[]>('product-category/list');
  }

  getById(encryptedCategoryId: string): Observable<ApiResponse<ProductCategoryResponse>> {
    return this.api.get<ProductCategoryResponse>(`product-category/${encryptedCategoryId}`);
  }

  create(request: ProductCategoryCreateRequest): Observable<ApiResponse<ProductCategoryResponse>> {
    return this.api.post<ProductCategoryResponse>('product-category/create', request);
  }

  update(request: ProductCategoryUpdateRequest): Observable<ApiResponse<ProductCategoryResponse>> {
    return this.api.patch<ProductCategoryResponse>('product-category/update', request);
  }

  toggleActive(encryptedCategoryId: string): Observable<ApiResponse<null>> {
    return this.api.patch<null>(`product-category/toggle-active/${encryptedCategoryId}`);
  }

  delete(encryptedCategoryId: string): Observable<ApiResponse<null>> {
    return this.api.delete<null>(`product-category/delete/${encryptedCategoryId}`);
  }
}
