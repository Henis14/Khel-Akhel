import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import {
  ProductCategoryCreateRequest,
  ProductCategoryResponse,
  ProductCategoryUpdateRequest,
  ProductSubCategoryCreateRequest,
  ProductSubCategoryResponse,
  ProductSubCategoryUpdateRequest,
} from '../models/product-category.model';

@Injectable({
  providedIn: 'root',
})
export class AdminProductCategoryService {
  constructor(private api: ApiService) {}

  // #region Category
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

  delete(encryptedCategoryId: string): Observable<ApiResponse<null>> {
    return this.api.delete<null>(`product-category/delete/${encryptedCategoryId}`);
  }
  // #endregion

  // #region SubCategory
  getSubCategories(
    encryptedCategoryId?: string,
  ): Observable<ApiResponse<ProductSubCategoryResponse[]>> {
    const params: any = {};
    if (encryptedCategoryId) {
      params.encryptedCategoryId = encryptedCategoryId;
    }
    return this.api.get<ProductSubCategoryResponse[]>('product-category/subcategory/list', params);
  }

  getSubCategoryById(
    encryptedSubCategoryId: string,
  ): Observable<ApiResponse<ProductSubCategoryResponse>> {
    return this.api.get<ProductSubCategoryResponse>(
      `product-category/subcategory/${encryptedSubCategoryId}`,
    );
  }

  createSubCategory(
    request: ProductSubCategoryCreateRequest,
  ): Observable<ApiResponse<ProductSubCategoryResponse>> {
    return this.api.post<ProductSubCategoryResponse>(
      'product-category/subcategory/create',
      request,
    );
  }

  updateSubCategory(
    request: ProductSubCategoryUpdateRequest,
  ): Observable<ApiResponse<ProductSubCategoryResponse>> {
    return this.api.patch<ProductSubCategoryResponse>(
      'product-category/subcategory/update',
      request,
    );
  }

  deleteSubCategory(encryptedSubCategoryId: string): Observable<ApiResponse<null>> {
    return this.api.delete<null>(`product-category/subcategory/delete/${encryptedSubCategoryId}`);
  }
  // #endregion
}
