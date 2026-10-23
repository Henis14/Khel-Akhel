import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import { HeroBannerItem, HeroBannerUpdateRequest } from '../models/hero-banner.model';

@Injectable({
  providedIn: 'root',
})
export class AdminHeroBannerService {
  constructor(private api: ApiService) {}

  getAdminList(): Observable<ApiResponse<HeroBannerItem[]>> {
    return this.api.get<HeroBannerItem[]>('hero-banner/admin/list');
  }

  getById(id: string): Observable<ApiResponse<HeroBannerItem>> {
    return this.api.get<HeroBannerItem>(`hero-banner/${id}`);
  }

  createImages(
    files: File[],
    title?: string,
    displayOrder: number = 1,
    replaceExisting: boolean = false,
  ): Observable<ApiResponse<HeroBannerItem[]>> {
    const formData = new FormData();
    if (title && title.trim()) {
      formData.append('Title', title.trim());
    }
    formData.append('DisplayOrder', displayOrder.toString());
    formData.append('ReplaceExisting', replaceExisting ? 'true' : 'false');
    formData.append('IsImage', 'true');
    formData.append('IsVideo', 'false');

    files.forEach((file) => {
      formData.append('Images', file);
    });

    return this.api.post<HeroBannerItem[]>('hero-banner', formData);
  }

  createVideo(
    file: File,
    title?: string,
    displayOrder: number = 1,
    replaceExisting: boolean = false,
  ): Observable<ApiResponse<HeroBannerItem[]>> {
    const formData = new FormData();
    if (title && title.trim()) {
      formData.append('Title', title.trim());
    }
    formData.append('DisplayOrder', displayOrder.toString());
    formData.append('ReplaceExisting', replaceExisting ? 'true' : 'false');
    formData.append('IsImage', 'false');
    formData.append('IsVideo', 'true');
    formData.append('Video', file);

    return this.api.post<HeroBannerItem[]>('hero-banner', formData);
  }

  update(
    id: string,
    request: HeroBannerUpdateRequest,
    replacementFile?: File,
  ): Observable<ApiResponse<HeroBannerItem>> {
    const formData = new FormData();
    if (request.title !== undefined && request.title !== null) {
      formData.append('Title', request.title);
    }
    if (request.displayOrder !== undefined && request.displayOrder !== null) {
      formData.append('DisplayOrder', request.displayOrder.toString());
    }
    if (request.isActive !== undefined && request.isActive !== null) {
      formData.append('IsActive', request.isActive ? 'true' : 'false');
    }
    if (replacementFile) {
      formData.append('MediaFile', replacementFile);
    }

    return this.api.put<HeroBannerItem>(`hero-banner/${id}`, formData);
  }

  toggleStatus(id: string, isActive: boolean): Observable<ApiResponse<HeroBannerItem>> {
    return this.api.patch<HeroBannerItem>(`hero-banner/${id}/status`, { isActive });
  }

  delete(id: string): Observable<ApiResponse<any>> {
    return this.api.delete<any>(`hero-banner/${id}`);
  }

  getPublic(): Observable<ApiResponse<HeroBannerItem[]>> {
    return this.api.get<HeroBannerItem[]>('hero-banner');
  }
}
