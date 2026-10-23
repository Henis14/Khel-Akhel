import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminFaqService } from '../../../admin/services/admin-faq.service';
import { FaqResponse } from '../../../admin/models/faq.model';

interface FaqGroup {
  name: string;
  icon: TopicIcon;
  items: FaqResponse[];
}

export type TopicIcon = 'all' | 'parcel' | 'return' | 'pay' | 'pencil' | 'hand' | 'user' | 'spark';

const GENERAL = 'General';

/** picks a line-art icon from the words in an FAQ category name */
function iconFor(category: string): TopicIcon {
  const c = category.toLowerCase();
  if (/(order|deliver|ship|track|dispatch|courier)/.test(c)) return 'parcel';
  if (/(return|refund|exchange|cancel)/.test(c)) return 'return';
  if (/(pay|price|cod|upi|card|invoice|gst)/.test(c)) return 'pay';
  if (/(commission|custom|bespoke|project|design)/.test(c)) return 'pencil';
  if (/(care|clean|material|handmade|product|craft)/.test(c)) return 'hand';
  if (/(account|login|sign|profile|password)/.test(c)) return 'user';
  return 'spark';
}

/**
 * Public FAQ page: GET api/faq/public?location=faq (published, active,
 * "show on FAQ page" items in admin display order), grouped by category,
 * shown as a one-at-a-time accordion.
 */
@Component({
  selector: 'app-faq',
  standalone: true,
  imports: [RouterLink, NgTemplateOutlet],
  templateUrl: './faq.html',
  styleUrl: './faq.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Faq implements OnInit {
  private faqService = inject(AdminFaqService);
  private destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly error = signal(false);
  readonly faqs = signal<FaqResponse[]>([]);
  readonly activeCategory = signal<string>('All');
  readonly openId = signal<string | null>(null);

  readonly skeleton = [0, 1, 2, 3, 4];

  /** categories in the order they first appear (admin display order) */
  readonly categories = computed(() => {
    const seen: string[] = [];
    for (const f of this.faqs()) {
      const c = this.categoryOf(f);
      if (!seen.includes(c)) seen.push(c);
    }
    return seen.map((name) => ({ name, icon: iconFor(name) }));
  });

  readonly counts = computed(() => {
    const map = new Map<string, number>();
    for (const f of this.faqs()) {
      const c = this.categoryOf(f);
      map.set(c, (map.get(c) ?? 0) + 1);
    }
    return map;
  });

  readonly groups = computed<FaqGroup[]>(() => {
    const active = this.activeCategory();
    const list = this.faqs().filter((f) => active === 'All' || this.categoryOf(f) === active);

    return this.categories()
      .map((c) => ({
        name: c.name,
        icon: c.icon,
        items: list.filter((f) => this.categoryOf(f) === c.name),
      }))
      .filter((g) => g.items.length > 0);
  });

  /** running number for each visible question: 01, 02, … */
  readonly numbers = computed(() => {
    const map = new Map<string, string>();
    let n = 0;
    for (const g of this.groups()) {
      for (const f of g.items) map.set(f.encryptedFaqId, String(++n).padStart(2, '0'));
    }
    return map;
  });


  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(false);

    this.faqService
      .getPublic('faq')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.faqs.set(res?.success && Array.isArray(res.data) ? res.data : []);
          this.loading.set(false);
        },
        error: () => {
          this.faqs.set([]);
          this.loading.set(false);
          this.error.set(true);
        },
      });
  }

  categoryOf(f: FaqResponse): string {
    return f.category?.trim() || GENERAL;
  }

  selectCategory(name: string): void {
    this.activeCategory.set(name);
    this.openId.set(null);
  }



  /** one answer open at a time */
  toggle(id: string): void {
    this.openId.update((cur) => (cur === id ? null : id));
  }
}
