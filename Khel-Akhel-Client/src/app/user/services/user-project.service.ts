import { Injectable } from '@angular/core';
import { Observable, catchError, map, shareReplay, throwError } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import { environment } from '../../../environments/environment';
import { ProjectPublic, slugifyProjectText } from '../../admin/models/project.model';

/** A published project prepared for the storefront: stable slug + absolute image URLs. */
export interface StoreProject extends ProjectPublic {
  slug: string;
  disciplineSlug: string;
  imageUrls: string[];
}

@Injectable({
  providedIn: 'root',
})
export class UserProjectService {
  private cache$: Observable<StoreProject[]> | null = null;

  constructor(private api: ApiService) {}

  /**
   * Fetch all active, published Project sections ordered by displayOrder for the public storefront.
   */
  getPublicProjects(): Observable<ApiResponse<ProjectPublic[]>> {
    return this.api.get<ProjectPublic[]>('projects/public');
  }

  /**
   * Published projects, cached for the session so the index and detail pages share one request.
   * Encrypted IDs change on every response, so each project gets a slug from its heading instead.
   */
  getStoreProjects(forceRefresh = false): Observable<StoreProject[]> {
    if (!this.cache$ || forceRefresh) {
      this.cache$ = this.getPublicProjects().pipe(
        map((res) => {
          if (!res?.success) throw new Error(res?.message || 'Failed to load projects');
          return this.toStoreProjects(res.data || []);
        }),
        shareReplay({ bufferSize: 1, refCount: false }),
        catchError((err) => {
          this.cache$ = null; // allow a retry
          return throwError(() => err);
        }),
      );
    }
    return this.cache$;
  }

  private toStoreProjects(items: ProjectPublic[]): StoreProject[] {
    const used = new Map<string, number>();
    return [...items]
      .sort((a, b) => (a.displayOrder ?? 0) - (b.displayOrder ?? 0))
      .map((p) => {
        const base = slugifyProjectText(p.heading) || 'project';
        const n = (used.get(base) ?? 0) + 1;
        used.set(base, n);
        const images = [...(p.images || [])].sort(
          (a, b) => (a.displayOrder ?? 0) - (b.displayOrder ?? 0),
        );
        return {
          ...p,
          images,
          slug: n > 1 ? `${base}-${n}` : base,
          disciplineSlug: slugifyProjectText(p.contentType),
          imageUrls: images.map((img) => this.getImageUrl(img.imageUrl || img.imagePath)),
        };
      });
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
