import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import {
  ProductCreateRequest,
  ProductListRequest,
  ProductListResponse,
  ProductResponse,
  ProductUpdateRequest
} from '../models/product.model';

@Injectable({
  providedIn: 'root'
})
export class AdminProductService {
  constructor(private api: ApiService) {}

  getList(request?: ProductListRequest): Observable<ApiResponse<ProductListResponse>> {
    const req = request ?? { pageIndex: 1, pageSize: 10 };
    const params: any = {};
    if (req.pageIndex) params.pageIndex = req.pageIndex;
    if (req.pageSize) params.pageSize = req.pageSize;
    if (req.encryptedCategoryId) params.encryptedCategoryId = req.encryptedCategoryId;
    if (req.search) params.search = req.search;
    if (req.isActive !== undefined && req.isActive !== null) params.isActive = req.isActive;
    return this.api.get<ProductListResponse>('product/list', params);
  }

  getById(encryptedProductId: string): Observable<ApiResponse<ProductResponse>> {
    return this.api.get<ProductResponse>(`product/${encryptedProductId}`);
  }

  create(request: ProductCreateRequest): Observable<ApiResponse<ProductResponse>> {
    return this.api.post<ProductResponse>('product/create', request);
  }

  update(request: ProductUpdateRequest): Observable<ApiResponse<ProductResponse>> {
    return this.api.patch<ProductResponse>('product/update', request);
  }

  toggleActive(encryptedProductId: string): Observable<ApiResponse<null>> {
    return this.api.patch<null>(`product/toggle-active/${encryptedProductId}`);
  }

  delete(encryptedProductId: string): Observable<ApiResponse<null>> {
    return this.api.delete<null>(`product/delete/${encryptedProductId}`);
  }
}
