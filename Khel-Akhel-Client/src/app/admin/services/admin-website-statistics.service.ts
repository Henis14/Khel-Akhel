import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../common/services/api.service';
import { ApiResponse } from '../../common/models/api-response.model';
import {
  WebsiteStatisticsFilter,
  WebsiteStatisticsSummary,
} from '../models/website-statistics.model';

@Injectable({
  providedIn: 'root',
})
export class AdminWebsiteStatisticsService {
  private api = inject(ApiService);

  getSummary(filter: WebsiteStatisticsFilter): Observable<ApiResponse<WebsiteStatisticsSummary>> {
    const params: Record<string, any> = { range: filter.range };
    if (filter.range === 'custom') {
      if (filter.startDate) params['startDate'] = filter.startDate;
      if (filter.endDate) params['endDate'] = filter.endDate;
    }

    return this.api.get<WebsiteStatisticsSummary>('website-statistics/admin/summary', params);
  }
}
