import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import {
  CustomerAddressCreateRequest,
  CustomerAddressResponse,
  CustomerAddressUpdateRequest
} from '../models/customer-address.model';

@Injectable({
  providedIn: 'root'
})
export class AdminCustomerAddressService {
  constructor(private api: ApiService) {}

  getList(encryptedCustomerId?: string): Observable<ApiResponse<CustomerAddressResponse[]>> {
    const params = encryptedCustomerId ? { encryptedCustomerId } : undefined;
    return this.api.get<CustomerAddressResponse[]>('customer-address/list', params);
  }

  getById(encryptedAddressId: string): Observable<ApiResponse<CustomerAddressResponse>> {
    return this.api.get<CustomerAddressResponse>(`customer-address/${encryptedAddressId}`);
  }

  create(request: CustomerAddressCreateRequest): Observable<ApiResponse<CustomerAddressResponse>> {
    return this.api.post<CustomerAddressResponse>('customer-address/create', request);
  }

  update(request: CustomerAddressUpdateRequest): Observable<ApiResponse<CustomerAddressResponse>> {
    return this.api.patch<CustomerAddressResponse>('customer-address/update', request);
  }

  toggleActive(encryptedAddressId: string): Observable<ApiResponse<null>> {
    return this.api.patch<null>(`customer-address/toggle-active/${encryptedAddressId}`);
  }

  delete(encryptedAddressId: string): Observable<ApiResponse<null>> {
    return this.api.delete<null>(`customer-address/delete/${encryptedAddressId}`);
  }
}
