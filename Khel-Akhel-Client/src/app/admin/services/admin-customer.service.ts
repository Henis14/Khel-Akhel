import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import {
  CustomerCreateRequest,
  CustomerListRequest,
  CustomerListResponse,
  CustomerResponse,
  CustomerUpdateRequest
} from '../models/customer.model';

@Injectable({
  providedIn: 'root'
})
export class AdminCustomerService {
  constructor(private api: ApiService) {}

  getList(request: CustomerListRequest): Observable<ApiResponse<CustomerListResponse>> {
    const params: any = {};
    if (request.pageIndex) params.pageIndex = request.pageIndex;
    if (request.pageSize) params.pageSize = request.pageSize;
    if (request.search) params.search = request.search;
    if (request.isActive !== undefined && request.isActive !== null) params.isActive = request.isActive;
    return this.api.get<CustomerListResponse>('customer/list', params);
  }

  getById(encryptedId: string): Observable<ApiResponse<CustomerResponse>> {
    return this.api.get<CustomerResponse>(`customer/${encryptedId}`);
  }

  create(request: CustomerCreateRequest): Observable<ApiResponse<CustomerResponse>> {
    return this.api.post<CustomerResponse>('customer/create', request);
  }

  update(encryptedId: string, request: CustomerUpdateRequest): Observable<ApiResponse<CustomerResponse>> {
    return this.api.patch<CustomerResponse>(`customer/update/${encryptedId}`, request);
  }

  toggleActive(encryptedId: string): Observable<ApiResponse<null>> {
    return this.api.patch<null>(`customer/toggle-active/${encryptedId}`);
  }

  delete(encryptedId: string): Observable<ApiResponse<null>> {
    return this.api.delete<null>(`customer/delete/${encryptedId}`);
  }
}
