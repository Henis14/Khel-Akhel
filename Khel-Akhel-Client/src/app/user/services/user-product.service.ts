import { Injectable } from '@angular/core';
import { EMPTY, Observable, expand, map, reduce } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import { ProductListResponse, ProductResponse } from '../../admin/models/product.model';
import { ProductCategoryResponse } from '../../admin/models/product-category.model';
import { environment } from '../../../environments/environment';

/** Page size the API allows at most (ProductController clamps to 100) */
const MAX_PAGE_SIZE = 100;

@Injectable({
  providedIn: 'root',
})
export class UserProductService {
  private readonly assetBaseUrl = environment.apiBaseUrl.replace(/\/api\/?$/i, '');

  constructor(private api: ApiService) {}

  getPublicProducts(
    search?: string,
    categoryId?: string,
    subCategoryId?: string,
  ): Observable<ApiResponse<ProductListResponse>> {
    return this.api.post<ProductListResponse>('product/search', {
      pageIndex: 1,
      pageSize: 20,
      search: search || undefined,
      encryptedCategoryId: categoryId || undefined,
      encryptedSubCategoryId: subCategoryId || undefined,
      isActive: true,
    });
  }

  getProductById(encryptedProductId: string): Observable<ApiResponse<ProductResponse>> {
    return this.api.get<ProductResponse>(`product/${encryptedProductId}`);
  }

  /** Active categories, already ordered by DisplayOrder on the server */
  getCategories(): Observable<ProductCategoryResponse[]> {
    return this.api
      .get<ProductCategoryResponse[]>('product-category/list')
      .pipe(map((res) => (res?.success && Array.isArray(res.data) ? res.data : [])));
  }

  /** Every active product (walks all pages of product/list) */
  getAllActiveProducts(): Observable<ProductResponse[]> {
    const page = (pageIndex: number) =>
      this.api.post<ProductListResponse>('product/list', {
        pageIndex,
        pageSize: MAX_PAGE_SIZE,
        isActive: true,
        sortBy: 'newest',
      });

    return page(1).pipe(
      expand((res) => {
        const d = res?.data;
        if (!res?.success || !d) return EMPTY;
        const loaded = d.pageIndex * d.pageSize;
        return loaded < d.totalRecords ? page(d.pageIndex + 1) : EMPTY;
      }),
      map((res) => (res?.success ? (res.data?.products ?? []) : [])),
      reduce((all, chunk) => all.concat(chunk), [] as ProductResponse[]),
    );
  }

  /** Images are stored as `/uploads/products/x.jpg` on the API host */
  imageUrl(path: string | null | undefined): string {
    if (!path) return '';
    if (/^https?:\/\//i.test(path)) return path;
    return `${this.assetBaseUrl}${path.startsWith('/') ? '' : '/'}${path}`;
  }
}
