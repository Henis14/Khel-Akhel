import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import { environment } from '../../../environments/environment';
import { ProjectPublic } from '../../admin/models/project.model';

@Injectable({
  providedIn: 'root',
})
export class UserProjectService {
  constructor(private api: ApiService) {}

  /**
   * Fetch all active, published Project sections ordered by displayOrder for the public storefront.
   */
  getPublicProjects(): Observable<ApiResponse<ProjectPublic[]>> {
    return this.api.get<ProjectPublic[]>('projects/public');
  }

  /**
   * Resolve an image path to an absolute URL pointing to the backend static uploads directory.
   */
  getImageUrl(relativePath: string | null | undefined): string {
    if (!relativePath) return '';
    if (
      relativePath.startsWith('http://') ||
      relativePath.startsWith('https://') ||
      relativePath.startsWith('data:')
    ) {
      return relativePath;
    }
    const serverHost = environment.apiBaseUrl.replace(/\/api\/?$/i, '');
    const cleanPath = relativePath.startsWith('/') ? relativePath : `/${relativePath}`;
    return `${serverHost}${cleanPath}`;
  }
}
