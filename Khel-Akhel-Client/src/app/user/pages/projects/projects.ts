import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { combineLatest } from 'rxjs';
import { StoreProject, UserProjectService } from '../../services/user-project.service';
import {
  GalleryImageItem,
  GalleryRendererComponent,
} from '../../../common/components/gallery-renderer/gallery-renderer';
import { CONTENT_TYPES } from '../../../admin/models/project.model';

interface Discipline {
  slug: string;
  name: string;
  count: number;
}

interface ProjectView {
  project: StoreProject;
  /** subheading with any repeated date/venue text removed (it is shown in the details list) */
  sub: string;
  /** event title, hidden when it only repeats the project heading */
  forTitle: string;
  gallery: GalleryImageItem[];
  paragraphs: string[];
  credits: string[];
}

/**
 * Storefront Projects — modelled on daras.co.in/festivals-and-sets and /objects-and-forms:
 * one page, each project's details followed by all of its photographs in the grid chosen in admin.
 *  /projects               all published projects (filter with ?type=<discipline-slug>)
 *  /projects/:slug         same page, scrolled to that project (shareable link)
 */
@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [RouterLink, GalleryRendererComponent],
  templateUrl: './projects.html',
  styleUrl: './projects.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectsComponent {
  private service = inject(UserProjectService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private host = inject(ElementRef<HTMLElement>);
  private revealObserver: IntersectionObserver | null = null;

  /** Projects fade up as they scroll into view — only switched on once the observer exists. */
  readonly revealReady = signal(false);

  readonly loading = signal(true);
  readonly error = signal(false);
  readonly projects = signal<StoreProject[]>([]);
  readonly activeType = signal<string>('');
  readonly focusSlug = signal<string | null>(null);
  readonly expanded = signal<ReadonlySet<string>>(new Set());

  readonly skeletons = [1, 2];

  /** Photographs shown per project before "+N" — the lightbox still holds every one. */
  readonly previewCount = 6;

  /** Disciplines that have projects, in the order of the site menu. */
  readonly disciplines = computed<Discipline[]>(() => {
    const map = new Map<string, Discipline>();
    for (const p of this.projects()) {
      if (!p.contentType || !p.disciplineSlug) continue;
      const d = map.get(p.disciplineSlug);
      if (d) d.count++;
      else map.set(p.disciplineSlug, { slug: p.disciplineSlug, name: p.contentType, count: 1 });
    }
    const order = (n: string) => {
      const i = CONTENT_TYPES.indexOf(n);
      return i === -1 ? 999 : i;
    };
    return [...map.values()].sort((a, b) => order(a.name) - order(b.name));
  });

  readonly activeDiscipline = computed(
    () => this.disciplines().find((d) => d.slug === this.activeType()) ?? null,
  );

  readonly views = computed<ProjectView[]>(() => {
    const type = this.activeType();
    return this.projects()
      .filter((p) => !type || p.disciplineSlug === type)
      .map((p) => ({
        project: p,
        sub: this.cleanSub(p.subheading, p.displayYear),
        forTitle: this.isSameName(p.projectOrEventTitle, p.heading) ? '' : (p.projectOrEventTitle || '').trim(),
        gallery: p.imageUrls.map((url, i) => ({
          imageUrl: url,
          originalFileName: `${p.heading}, photograph ${i + 1}`,
          displayOrder: i + 1,
        })),
        paragraphs: this.toParagraphs(p.detailedDescription),
        credits: (p.credits || '')
          .split(/\r?\n|;\s*/)
          .map((c) => c.trim())
          .filter(Boolean),
      }));
  });

  constructor() {
    combineLatest([this.route.paramMap, this.route.queryParamMap])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(([params, query]) => {
        this.activeType.set((query.get('type') || '').toLowerCase());
        this.focusSlug.set(params.get('slug'));
        this.scrollToFocus();
      });

    this.load();

    // re-observe whenever the visible list changes (load, filter)
    effect(() => {
      this.views();
      setTimeout(() => this.observeProjects(), 0);
    });

    this.destroyRef.onDestroy(() => this.revealObserver?.disconnect());
  }

  private observeProjects(): void {
    if (typeof window === 'undefined' || !('IntersectionObserver' in window)) return;
    const reduce = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    if (reduce) return;
    if (!this.revealObserver) {
      this.revealObserver = new IntersectionObserver(
        (entries) => {
          for (const e of entries) {
            if (e.isIntersecting) {
              e.target.classList.add('is-in');
              this.revealObserver?.unobserve(e.target);
            }
          }
        },
        { rootMargin: '0px 0px -8% 0px', threshold: 0.08 },
      );
    }
    const root = this.host.nativeElement as HTMLElement;
    root.querySelectorAll('section.pj-project:not(.is-in)').forEach((el) => {
      // anything already on screen shows immediately
      const r = el.getBoundingClientRect();
      if (r.top < window.innerHeight * 0.92) el.classList.add('is-in');
      else this.revealObserver!.observe(el);
    });
    this.revealReady.set(true);
  }

  load(force = false): void {
    this.loading.set(true);
    this.error.set(false);
    this.service
      .getStoreProjects(force)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (list) => {
          this.projects.set(list);
          this.loading.set(false);
          this.scrollToFocus();
        },
        error: () => {
          this.error.set(true);
          this.loading.set(false);
        },
      });
  }

  setType(slug: string): void {
    this.router.navigate(['/projects'], { queryParams: { type: slug || null } });
  }

  isExpanded(slug: string): boolean {
    return this.expanded().has(slug);
  }

  toggle(slug: string): void {
    this.expanded.update((s) => {
      const next = new Set(s);
      if (next.has(slug)) next.delete(slug);
      else next.add(slug);
      return next;
    });
  }

  linkLabel(url: string | null | undefined): string {
    if (!url) return '';
    try {
      return new URL(url).hostname.replace(/^www\./, '');
    } catch {
      return 'Visit link';
    }
  }

  /** "Festival / 14th – 16th Feb, 2025 Goa" → "Festival · Goa" when the date is already shown. */
  private cleanSub(sub: string | null | undefined, date: string | null | undefined): string {
    let s = (sub || '').trim();
    const d = (date || '').trim();
    if (s && d && s.includes(d)) {
      s = s.replace(d, ' · ').replace(/\s*\/\s*·\s*/g, ' · ').replace(/\s*·\s*/g, ' · ');
      s = s.replace(/^[\s·/]+|[\s·/]+$/g, '').replace(/(\s·\s){2,}/g, ' · ');
    }
    return s;
  }

  /** "Vivan" vs "Vivaan" → same (case/space-insensitive, allows a small typo). */
  private isSameName(a: string | null | undefined, b: string | null | undefined): boolean {
    const n = (x: string | null | undefined) => (x || '').toLowerCase().replace(/[^a-z0-9]/g, '');
    const x = n(a);
    const y = n(b);
    if (!x) return true;
    if (x === y || y.includes(x) || x.includes(y)) return true;
    // edit distance ≤ 2
    const dp = Array.from({ length: x.length + 1 }, (_, i) => [i, ...Array(y.length).fill(0)]);
    for (let j = 1; j <= y.length; j++) dp[0][j] = j;
    for (let i = 1; i <= x.length; i++)
      for (let j = 1; j <= y.length; j++)
        dp[i][j] = Math.min(dp[i - 1][j] + 1, dp[i][j - 1] + 1, dp[i - 1][j - 1] + (x[i - 1] === y[j - 1] ? 0 : 1));
    return dp[x.length][y.length] <= 2;
  }

  private toParagraphs(text: string | null | undefined): string[] {
    return (text || '')
      .replace(/\r\n/g, '\n')
      .split(/\n\s*\n/)
      .map((p) => p.trim())
      .filter(Boolean);
  }

  /** /projects/:slug → scroll that project into view once it is rendered. */
  private scrollToFocus(): void {
    const slug = this.focusSlug();
    if (!slug || this.loading() || typeof document === 'undefined') return;
    setTimeout(() => {
      document.getElementById('project-' + slug)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }, 80);
  }
}
