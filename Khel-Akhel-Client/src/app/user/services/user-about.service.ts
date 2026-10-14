import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import { environment } from '../../../environments/environment';
import { AboutPublic } from '../../admin/models/about.model';

@Injectable({
  providedIn: 'root',
})
export class UserAboutService {
  constructor(private api: ApiService) {}

  /**
   * Fetch all active, published About sections ordered by displayOrder for the public storefront.
   */
  getPublicSections(): Observable<ApiResponse<AboutPublic[]>> {
    return this.api.get<AboutPublic[]>('about/public');
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
