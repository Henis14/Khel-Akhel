import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import { environment } from '../../../environments/environment';
import {
  CommissionCategory,
  CommissionListFilter,
  CommissionListResponse,
  CommissionRequestDetail,
  CommissionStatusUpdateRequest,
} from '../models/commissioned-project.model';

@Injectable({
  providedIn: 'root',
})
export class AdminCommissionedProjectService {
  constructor(private api: ApiService) {}

  getCategories(): Observable<ApiResponse<CommissionCategory[]>> {
    return this.api.get<CommissionCategory[]>('commissions/categories');
  }

  getList(filter: CommissionListFilter): Observable<ApiResponse<CommissionListResponse>> {
    const params: Record<string, any> = {};
    if (filter.pageIndex) params['pageIndex'] = filter.pageIndex;
    if (filter.pageSize) params['pageSize'] = filter.pageSize;
    if (filter.search) params['search'] = filter.search.trim();
    if (filter.categoryId) params['categoryId'] = filter.categoryId;
    if (filter.encryptedCategoryId) params['encryptedCategoryId'] = filter.encryptedCategoryId;
    if (filter.status) params['status'] = filter.status;
    if (filter.startDate) params['startDate'] = filter.startDate;
    if (filter.endDate) params['endDate'] = filter.endDate;
    if (filter.sortBy) params['sortBy'] = filter.sortBy;

    return this.api.get<CommissionListResponse>('commissions/list', params);
  }

  getById(encryptedId: string): Observable<ApiResponse<CommissionRequestDetail>> {
    return this.api.get<CommissionRequestDetail>(`commissions/${encryptedId}`);
  }

  updateStatus(request: CommissionStatusUpdateRequest): Observable<ApiResponse<any>> {
    return this.api.patch<any>('commissions/update-status', request);
  }

  getImageUrl(relativePath: string | null | undefined): string {
    if (!relativePath) return '';
    if (relativePath.startsWith('http://') || relativePath.startsWith('https://')) {
      return relativePath;
    }
    const serverHost = environment.apiBaseUrl.replace(/\/api\/?$/i, '');
    const cleanPath = relativePath.startsWith('/') ? relativePath : `/${relativePath}`;
    return `${serverHost}${cleanPath}`;
  }
}
