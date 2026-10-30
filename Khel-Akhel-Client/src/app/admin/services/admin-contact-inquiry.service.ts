import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import {
  ContactInquiryItem,
  ContactInquiryListFilter,
  ContactInquiryListResponse,
  ContactInquirySummary,
} from '../models/contact-inquiry.model';

@Injectable({
  providedIn: 'root',
})
export class AdminContactInquiryService {
  private api = inject(ApiService);

  getList(filter: ContactInquiryListFilter): Observable<ApiResponse<ContactInquiryListResponse>> {
    const params: Record<string, any> = {};
    if (filter.pageIndex) params['pageIndex'] = filter.pageIndex;
    if (filter.pageSize) params['pageSize'] = filter.pageSize;
    if (filter.search) params['search'] = filter.search.trim();
    if (filter.isActive !== undefined && filter.isActive !== null) {
      params['isActive'] = filter.isActive;
    }
    if (filter.startDate) params['startDate'] = filter.startDate;
    if (filter.endDate) params['endDate'] = filter.endDate;
    if (filter.sortBy) params['sortBy'] = filter.sortBy;

    return this.api.get<ContactInquiryListResponse>('contact-us/list', params);
  }

  getById(encryptedId: string): Observable<ApiResponse<ContactInquiryItem>> {
    return this.api.get<ContactInquiryItem>(`contact-us/${encryptedId}`);
  }

  toggleStatus(encryptedId: string, isActive?: boolean): Observable<ApiResponse<any>> {
    const body = isActive !== undefined ? { isActive } : {};
    return this.api.patch<any>(`contact-us/status/${encryptedId}`, body);
  }

  delete(encryptedId: string): Observable<ApiResponse<boolean>> {
    return this.api.delete<boolean>(`contact-us/${encryptedId}`);
  }

  getSummary(): Observable<ApiResponse<ContactInquirySummary>> {
    return this.api.get<ContactInquirySummary>('contact-us/summary');
  }
}
