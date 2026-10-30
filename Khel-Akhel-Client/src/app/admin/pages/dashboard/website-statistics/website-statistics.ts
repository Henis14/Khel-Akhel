import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { AdminWebsiteStatisticsService } from '../../../services/admin-website-statistics.service';
import {
  DailyVisitStat,
  WebsiteStatisticsRange,
  WebsiteStatisticsSummary,
} from '../../../models/website-statistics.model';

interface RangeOption {
  value: WebsiteStatisticsRange;
  label: string;
}

interface ChartBar {
  x: number;
  y: number;
  width: number;
  height: number;
  day: DailyVisitStat;
  label: string;
  showLabel: boolean;
}

const CHART_WIDTH = 720;
const CHART_HEIGHT = 220;
const CHART_PAD_TOP = 12;
const CHART_PAD_BOTTOM = 28;
const CHART_PAD_LEFT = 36;

function toIsoDate(date: Date): string {
  const m = `${date.getMonth() + 1}`.padStart(2, '0');
  const d = `${date.getDate()}`.padStart(2, '0');
  return `${date.getFullYear()}-${m}-${d}`;
}

function parseIsoDate(value: string): Date {
  const [y, m, d] = value.split('-').map(Number);
  return new Date(y, (m || 1) - 1, d || 1);
}

function niceMax(value: number): number {
  if (value <= 4) return 4;
  const pow = Math.pow(10, Math.floor(Math.log10(value)));
  for (const step of [1, 2, 2.5, 5, 10]) {
    if (step * pow >= value) return step * pow;
  }
  return 10 * pow;
}

@Component({
  selector: 'app-website-statistics',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './website-statistics.html',
  styleUrl: './website-statistics.scss',
})
export class WebsiteStatisticsComponent implements OnInit {
  private statsService = inject(AdminWebsiteStatisticsService);
  private destroyRef = inject(DestroyRef);

  readonly ranges: RangeOption[] = [
    { value: 'today', label: 'Today' },
    { value: 'week', label: 'Last 7 days' },
    { value: 'month', label: 'Last 30 days' },
    { value: 'custom', label: 'Custom' },
  ];

  readonly today = toIsoDate(new Date());

  range = signal<WebsiteStatisticsRange>('today');
  customStart = signal(toIsoDate(new Date(Date.now() - 6 * 86400000)));
  customEnd = signal(this.today);

  loading = signal(true);
  errorMessage = signal<string | null>(null);
  summary = signal<WebsiteStatisticsSummary | null>(null);
  activeBar = signal<ChartBar | null>(null);

  readonly chartWidth = CHART_WIDTH;
  readonly chartHeight = CHART_HEIGHT;
  readonly plotLeft = CHART_PAD_LEFT;
  readonly plotBottom = CHART_HEIGHT - CHART_PAD_BOTTOM;

  customInvalid = computed(() => {
    const start = this.customStart();
    const end = this.customEnd();
    if (!start || !end) return 'Choose both dates.';
    if (start > end) return 'Start date must be on or before the end date.';
    if (start > this.today) return 'Start date cannot be in the future.';
    const days = (parseIsoDate(end).getTime() - parseIsoDate(start).getTime()) / 86400000 + 1;
    if (days > 366) return 'Choose a range of one year or less.';
    return null;
  });

  trend = computed(() => {
    const s = this.summary();
    if (!s || s.previousTotalVisits === 0) return null;
    return Math.round(((s.totalVisits - s.previousTotalVisits) / s.previousTotalVisits) * 100);
  });

  topPage = computed(() => {
    const s = this.summary();
    const top = s?.pages[0];
    return top && top.visits > 0 ? top : null;
  });

  maxPageVisits = computed(() => Math.max(1, ...(this.summary()?.pages.map((p) => p.visits) ?? [1])));

  showChart = computed(() => (this.summary()?.daily.length ?? 0) > 1);

  yMax = computed(() => niceMax(Math.max(0, ...(this.summary()?.daily.map((d) => d.visits) ?? [0]))));

  yTicks = computed(() => {
    const max = this.yMax();
    const plotHeight = this.plotBottom - CHART_PAD_TOP;
    return [0, 0.5, 1].map((f) => ({
      value: Math.round(max * f),
      y: this.plotBottom - plotHeight * f,
    }));
  });

  bars = computed<ChartBar[]>(() => {
    const days = this.summary()?.daily ?? [];
    if (days.length < 2) return [];

    const plotWidth = CHART_WIDTH - CHART_PAD_LEFT;
    const plotHeight = this.plotBottom - CHART_PAD_TOP;
    const slot = plotWidth / days.length;
    const gap = Math.max(2, Math.min(slot * 0.3, 10));
    const width = Math.max(2, slot - gap);
    const labelEvery = Math.ceil(days.length / 8);
    const max = this.yMax();

    return days.map((day, i) => {
      const height = max ? (day.visits / max) * plotHeight : 0;
      const date = parseIsoDate(day.date);
      return {
        x: CHART_PAD_LEFT + i * slot + gap / 2,
        y: this.plotBottom - height,
        width,
        height,
        day,
        label: date.toLocaleDateString('en-IN', { day: 'numeric', month: 'short' }),
        showLabel:
          i === days.length - 1 ||
          (i % labelEvery === 0 && days.length - 1 - i >= Math.max(2, labelEvery * 0.6)),
      };
    });
  });

  ngOnInit(): void {
    this.load();
  }

  selectRange(value: WebsiteStatisticsRange): void {
    if (this.range() === value) return;
    this.range.set(value);
    if (value !== 'custom') this.load();
  }

  applyCustom(): void {
    if (this.customInvalid()) return;
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.activeBar.set(null);

    const range = this.range();
    this.statsService
      .getSummary({
        range,
        startDate: range === 'custom' ? this.customStart() : undefined,
        endDate: range === 'custom' ? this.customEnd() : undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          if (res?.success && res.data) {
            this.summary.set(res.data);
          } else {
            this.summary.set(null);
            this.errorMessage.set(res?.errors?.[0] || 'Website statistics could not be loaded.');
          }
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          this.summary.set(null);
          this.errorMessage.set(
            err?.error?.errors?.[0] || 'Website statistics could not be loaded from the server.',
          );
        },
      });
  }

  periodLabel(s: WebsiteStatisticsSummary): string {
    const fmt = (v: string) =>
      parseIsoDate(v).toLocaleDateString('en-IN', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
      });
    return s.startDate === s.endDate ? fmt(s.startDate) : `${fmt(s.startDate)} to ${fmt(s.endDate)}`;
  }

  previousLabel(s: WebsiteStatisticsSummary): string {
    if (s.totalDays === 1) return 'yesterday';
    return `previous ${s.totalDays} days`;
  }

  barPath(bar: ChartBar): string {
    const { x, y, width: w, height: h } = bar;
    if (h <= 0) return '';
    const r = Math.min(4, w / 2, h);
    return `M${x},${y + h}V${y + r}Q${x},${y} ${x + r},${y}H${x + w - r}Q${x + w},${y} ${x + w},${y + r}V${y + h}Z`;
  }

  barTooltipLeft(bar: ChartBar): number {
    return ((bar.x + bar.width / 2) / CHART_WIDTH) * 100;
  }

  fullDate(value: string): string {
    return parseIsoDate(value).toLocaleDateString('en-IN', {
      weekday: 'short',
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    });
  }
}
