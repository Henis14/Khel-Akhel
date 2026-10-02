import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import {
  ProductStockAddRequest,
  ProductStockListRequest,
  ProductStockListResponse,
  ProductStockResponse,
  ProductStockUpdateRequest
} from '../models/product-stock.model';

@Injectable({
  providedIn: 'root'
})
export class AdminProductStockService {
  constructor(private api: ApiService) {}

  getList(request: ProductStockListRequest): Observable<ApiResponse<ProductStockListResponse>> {
    const params: any = {};
    if (request.pageIndex) params.pageIndex = request.pageIndex;
    if (request.pageSize) params.pageSize = request.pageSize;
    if (request.search) params.search = request.search;
    if (request.lowStockOnly !== undefined && request.lowStockOnly !== null) params.lowStockOnly = request.lowStockOnly;
    return this.api.get<ProductStockListResponse>('product-stock/list', params);
  }

  getById(encryptedStockId: string): Observable<ApiResponse<ProductStockResponse>> {
    return this.api.get<ProductStockResponse>(`product-stock/${encryptedStockId}`);
  }

  addStock(request: ProductStockAddRequest): Observable<ApiResponse<ProductStockResponse>> {
    return this.api.post<ProductStockResponse>('product-stock/add', request);
  }

  updateStock(request: ProductStockUpdateRequest): Observable<ApiResponse<ProductStockResponse>> {
    return this.api.patch<ProductStockResponse>('product-stock/update', request);
  }
}
