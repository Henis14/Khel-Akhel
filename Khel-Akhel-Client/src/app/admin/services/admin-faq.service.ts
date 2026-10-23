import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import {
  FaqAdminFilter,
  FaqCreateRequest,
  FaqResponse,
  FaqUpdateRequest,
} from '../models/faq.model';

@Injectable({
  providedIn: 'root',
})
export class AdminFaqService {
  constructor(private api: ApiService) {}

  getAdminList(filter?: FaqAdminFilter): Observable<ApiResponse<FaqResponse[]>> {
    const params: any = {};
    if (filter?.search) params.search = filter.search;
    if (filter?.isActive !== undefined && filter?.isActive !== null)
      params.isActive = filter.isActive;
    if (filter?.isPublished !== undefined && filter?.isPublished !== null)
      params.isPublished = filter.isPublished;
    if (filter?.page) params.page = filter.page;
    if (filter?.pageSize) params.pageSize = filter.pageSize;
    if (filter?.location) params.location = filter.location;

    return this.api.get<FaqResponse[]>('faq/admin/list', params);
  }

  getById(encryptedFaqId: string): Observable<ApiResponse<FaqResponse>> {
    return this.api.get<FaqResponse>(`faq/admin/${encryptedFaqId}`);
  }

  create(request: FaqCreateRequest): Observable<ApiResponse<FaqResponse>> {
    return this.api.post<FaqResponse>('faq/create', request);
  }

  update(request: FaqUpdateRequest): Observable<ApiResponse<FaqResponse>> {
    return this.api.patch<FaqResponse>('faq/update', request);
  }

  delete(encryptedFaqId: string): Observable<ApiResponse<null>> {
    return this.api.delete<null>(`faq/delete/${encryptedFaqId}`);
  }

  getPublic(location: string = 'faq'): Observable<ApiResponse<FaqResponse[]>> {
    return this.api.get<FaqResponse[]>('faq/public', { location });
  }
}
