export type WebsiteStatisticsRange = 'today' | 'week' | 'month' | 'custom';

export interface WebsiteStatisticsFilter {
  range: WebsiteStatisticsRange;
  startDate?: string;
  endDate?: string;
}

export interface PageVisitStat {
  pageKey: string;
  pageName: string;
  visits: number;
  uniqueVisits: number;
  sharePercent: number;
}

export interface DailyVisitStat {
  date: string;
  visits: number;
  uniqueVisits: number;
}

export interface WebsiteStatisticsSummary {
  range: WebsiteStatisticsRange;
  startDate: string;
  endDate: string;
  totalDays: number;
  totalVisits: number;
  totalUniqueVisits: number;
  averageDailyVisits: number;
  previousTotalVisits: number;
  pages: PageVisitStat[];
  daily: DailyVisitStat[];
}
