import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import {
  CustomerAddressResponse,
  CustomerAddressCreateRequest,
  CustomerAddressUpdateRequest,
} from '../../admin/models/customer-address.model';

@Injectable({
  providedIn: 'root',
})
export class UserAddressService {
  private api = inject(ApiService);

  getMyAddresses(): Observable<ApiResponse<CustomerAddressResponse[]>> {
    return this.api.get<CustomerAddressResponse[]>('customer-address/list');
  }

  getAddressById(encryptedAddressId: string): Observable<ApiResponse<CustomerAddressResponse>> {
    return this.api.get<CustomerAddressResponse>(`customer-address/${encodeURIComponent(encryptedAddressId)}`);
  }

  createAddress(request: CustomerAddressCreateRequest): Observable<ApiResponse<CustomerAddressResponse>> {
    return this.api.post<CustomerAddressResponse>('customer-address/create', request);
  }

  updateAddress(request: CustomerAddressUpdateRequest): Observable<ApiResponse<CustomerAddressResponse>> {
    return this.api.patch<CustomerAddressResponse>('customer-address/update', request);
  }

  deleteAddress(encryptedAddressId: string): Observable<ApiResponse<null>> {
    return this.api.delete<null>(`customer-address/delete/${encodeURIComponent(encryptedAddressId)}`);
  }
}
