import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import { environment } from '../../../environments/environment';
import {
  AboutCreatePayload,
  AboutDetail,
  AboutImage,
  AboutImageOrderDto,
  AboutListFilter,
  AboutListResponse,
  AboutUpdatePayload,
} from '../models/about.model';

@Injectable({
  providedIn: 'root',
})
export class AdminAboutService {
  constructor(private api: ApiService) {}

  /**
   * Fetch paginated list of About content sections with filtering and search.
   */
  getList(filter: AboutListFilter): Observable<ApiResponse<AboutListResponse>> {
    const params: Record<string, any> = {};
    if (filter.pageIndex !== undefined && filter.pageIndex !== null) params['pageIndex'] = filter.pageIndex;
    if (filter.pageSize !== undefined && filter.pageSize !== null) params['pageSize'] = filter.pageSize;
    if (filter.search && filter.search.trim()) params['search'] = filter.search.trim();
    if (filter.contentType && filter.contentType.trim()) params['contentType'] = filter.contentType.trim();
    if (filter.isPublished !== undefined && filter.isPublished !== null) params['isPublished'] = filter.isPublished;
    if (filter.isActive !== undefined && filter.isActive !== null) params['isActive'] = filter.isActive;
    if (filter.sortBy && filter.sortBy.trim()) params['sortBy'] = filter.sortBy.trim();

    return this.api.get<AboutListResponse>('about/list', params);
  }

  /**
   * Retrieve administrative details of a single About section by encrypted ID.
   */
  getById(encryptedAboutId: string): Observable<ApiResponse<AboutDetail>> {
    return this.api.get<AboutDetail>(`about/admin/${encodeURIComponent(encryptedAboutId)}`);
  }

  /**
   * Create a new About section with optional initial images (multipart/form-data).
   */
  create(payload: AboutCreatePayload): Observable<ApiResponse<AboutDetail>> {
    const formData = new FormData();
    formData.append('Heading', payload.heading.trim());
    if (payload.subheading?.trim()) formData.append('Subheading', payload.subheading.trim());
    if (payload.contentType?.trim()) formData.append('ContentType', payload.contentType.trim());
    if (payload.projectOrEventTitle?.trim()) formData.append('ProjectOrEventTitle', payload.projectOrEventTitle.trim());
    if (payload.displayYear?.trim()) formData.append('DisplayYear', payload.displayYear.trim());
    if (payload.shortDescription?.trim()) formData.append('ShortDescription', payload.shortDescription.trim());
    if (payload.detailedDescription?.trim()) formData.append('DetailedDescription', payload.detailedDescription.trim());
    if (payload.credits?.trim()) formData.append('Credits', payload.credits.trim());
    if (payload.location?.trim()) formData.append('Location', payload.location.trim());
    if (payload.externalUrl?.trim()) formData.append('ExternalUrl', payload.externalUrl.trim());
    formData.append('DisplayOrder', (payload.displayOrder ?? 0).toString());
    formData.append('LayoutType', (payload.layoutType || 'grid-3').trim());
    formData.append('IsPublished', String(!!payload.isPublished));
    formData.append('IsActive', String(payload.isActive !== false));

    if (payload.images && payload.images.length > 0) {
      for (const file of payload.images) {
        formData.append('Images', file, file.name);
      }
    }

    return this.api.post<AboutDetail>('about/create', formData);
  }

  /**
   * Update an existing About section, manage images, reorder, and persist layout.
   */
  update(payload: AboutUpdatePayload): Observable<ApiResponse<AboutDetail>> {
    const formData = new FormData();
    formData.append('EncryptedAboutId', payload.encryptedAboutId);
    formData.append('Heading', payload.heading.trim());
    if (payload.subheading !== undefined && payload.subheading !== null) {
      formData.append('Subheading', payload.subheading.trim());
    }
    if (payload.contentType !== undefined && payload.contentType !== null) {
      formData.append('ContentType', payload.contentType.trim());
    }
    if (payload.projectOrEventTitle !== undefined && payload.projectOrEventTitle !== null) {
      formData.append('ProjectOrEventTitle', payload.projectOrEventTitle.trim());
    }
    if (payload.displayYear !== undefined && payload.displayYear !== null) {
      formData.append('DisplayYear', payload.displayYear.trim());
    }
    if (payload.shortDescription !== undefined && payload.shortDescription !== null) {
      formData.append('ShortDescription', payload.shortDescription.trim());
    }
    if (payload.detailedDescription !== undefined && payload.detailedDescription !== null) {
      formData.append('DetailedDescription', payload.detailedDescription.trim());
    }
    if (payload.credits !== undefined && payload.credits !== null) {
      formData.append('Credits', payload.credits.trim());
    }
    if (payload.location !== undefined && payload.location !== null) {
      formData.append('Location', payload.location.trim());
    }
    if (payload.externalUrl !== undefined && payload.externalUrl !== null) {
      formData.append('ExternalUrl', payload.externalUrl.trim());
    }
    formData.append('DisplayOrder', (payload.displayOrder ?? 0).toString());
    formData.append('LayoutType', (payload.layoutType || 'grid-3').trim());

    if (payload.isPublished !== undefined && payload.isPublished !== null) {
      formData.append('IsPublished', String(payload.isPublished));
    }
    if (payload.isActive !== undefined && payload.isActive !== null) {
      formData.append('IsActive', String(payload.isActive));
    }

    if (payload.newImages && payload.newImages.length > 0) {
      for (const file of payload.newImages) {
        formData.append('NewImages', file, file.name);
      }
    }

    if (payload.removeImageIds && payload.removeImageIds.length > 0) {
      for (let i = 0; i < payload.removeImageIds.length; i++) {
        formData.append(`RemoveImageIds[${i}]`, payload.removeImageIds[i]);
      }
    }

    if (payload.imageOrders && payload.imageOrders.length > 0) {
      for (let i = 0; i < payload.imageOrders.length; i++) {
        formData.append(`ImageOrders[${i}].EncryptedImageId`, payload.imageOrders[i].encryptedImageId);
        formData.append(`ImageOrders[${i}].DisplayOrder`, payload.imageOrders[i].displayOrder.toString());
      }
    }

    // Backend supports both PUT and PATCH on /api/about/update
    return this.api.put<AboutDetail>('about/update', formData);
  }

  /**
   * Toggle publication status for an About section.
   */
  togglePublish(encryptedAboutId: string, isPublished: boolean): Observable<ApiResponse<any>> {
    return this.api.patch<any>(
      `about/${encodeURIComponent(encryptedAboutId)}/publish`,
      { encryptedAboutId, isPublished }
    );
  }

  /**
   * Soft-delete an About section and its associated images.
   */
  delete(encryptedAboutId: string): Observable<ApiResponse<any>> {
    return this.api.delete<any>(`about/delete/${encodeURIComponent(encryptedAboutId)}`);
  }

  /**
   * Upload additional images to an existing About section.
   */
  uploadImages(encryptedAboutId: string, files: File[]): Observable<ApiResponse<AboutImage[]>> {
    const formData = new FormData();
    formData.append('EncryptedAboutId', encryptedAboutId);
    for (const file of files) {
      formData.append('Images', file, file.name);
    }
    return this.api.post<AboutImage[]>('about/images/upload', formData);
  }

  /**
   * Delete a specific image from an About section.
   */
  deleteImage(encryptedImageId: string): Observable<ApiResponse<any>> {
    return this.api.delete<any>(`about/images/${encodeURIComponent(encryptedImageId)}`);
  }

  /**
   * Reorder images for an About section.
   */
  reorderImages(encryptedAboutId: string, imageOrders: AboutImageOrderDto[]): Observable<ApiResponse<any>> {
    return this.api.put<any>('about/images/reorder', {
      encryptedAboutId,
      imageOrders,
    });
  }

  /**
   * Resolve an image path to an absolute URL pointing to the backend static uploads directory.
   */
  getImageUrl(relativePath: string | null | undefined): string {
    if (!relativePath) return '';
    if (relativePath.startsWith('http://') || relativePath.startsWith('https://') || relativePath.startsWith('data:')) {
      return relativePath;
    }
    const serverHost = environment.apiBaseUrl.replace(/\/api\/?$/i, '');
    const cleanPath = relativePath.startsWith('/') ? relativePath : `/${relativePath}`;
    return `${serverHost}${cleanPath}`;
  }
}
