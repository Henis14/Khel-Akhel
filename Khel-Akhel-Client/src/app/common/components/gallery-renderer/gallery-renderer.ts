import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  Input,
  NgZone,
  OnDestroy,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';

export interface GalleryImageItem {
  imageUrl: string;
  originalFileName?: string;
  displayOrder?: number;
  caption?: string;
}

/** One tile of a CSS-grid based layout (12-column grid). */
interface GridCell {
  index: number;
  column: string; // grid-column value
  row: string | null; // grid-row value
  aspect: number | null; // width / height; null = stretch to the row
}

interface JustifiedRow {
  items: { index: number; ratio: number }[];
  /** false for a short last row — keep natural size instead of stretching */
  fill: boolean;
  target: number;
}

type Tier = 'lg' | 'md' | 'sm';

export const GALLERY_LAYOUT_IDS = [
  'grid-3',
  'grid-2',
  'editorial',
  'featured',
  'full-width',
  'masonry',
  'asymmetric',
  'grid-4',
  'bento',
  'split',
  'staggered',
  'carousel',
] as const;

/**
 * Renders a project's photographs in one of the gallery layouts chosen in admin.
 * Used by the admin live preview AND the storefront, so both always look identical.
 *
 * Layouts follow the galleries on daras.co.in:
 *  grid-3      Three-column grid — tiles share one shape picked from the photos; last row stretches
 *  grid-2      Two-column pairs
 *  editorial   Mosaic — a large tile with two stacked squares, then a full-width frame (alternating)
 *  featured    Lead image + grid — first photo full width at its own shape, the rest in 3 columns
 *  full-width  Full-width stack — one uncropped photo per row
 *  masonry     Masonry — uncropped photos in columns
 *  asymmetric  Justified rows — each photo keeps its proportions, rows align edge to edge
 *  grid-4      Four-column grid — compact catalogue of many photos
 *  bento       Bento grid — a large tile, squares and wide frames locked into one block (mirrors)
 *  split       Feature split — one large photo beside a 2×2 grid (mirrors)
 *  staggered   Staggered columns — three columns, the middle one offset, like an editorial spread
 *  carousel    Filmstrip — one scrolling row at equal height, photos keep their proportions
 */
@Component({
  selector: 'app-gallery-renderer',
  standalone: true,
  imports: [NgTemplateOutlet],
  templateUrl: './gallery-renderer.html',
  styleUrl: './gallery-renderer.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GalleryRendererComponent implements AfterViewInit, OnDestroy {
  private host = inject(ElementRef<HTMLElement>);
  private zone = inject(NgZone);
  private observer: ResizeObserver | null = null;

  private readonly _images = signal<GalleryImageItem[]>([]);
  private readonly _layout = signal<string>('grid-3');
  /** Natural width/height of each photo, keyed by URL — filled as photos load. */
  private readonly ratios = signal<Record<string, number>>({});
  private readonly width = signal<number>(960);

  @Input() set images(value: GalleryImageItem[] | null | undefined) {
    this._images.set(value ?? []);
  }
  get images(): GalleryImageItem[] {
    return this._images();
  }

  @Input() set layoutType(value: string | null | undefined) {
    const v = (value || '').toLowerCase();
    this._layout.set((GALLERY_LAYOUT_IDS as readonly string[]).includes(v) ? v : 'grid-3');
  }
  get layoutType(): string {
    return this._layout();
  }

  @Input() interactive = true;
  @Input() sectionTitle = '';
  /** Space between photos in px (daras.co.in uses a tight 4–8px). */
  @Input() gap = 8;

  // Lightbox state
  readonly lightboxIndex = signal<number | null>(null);

  readonly layout = this._layout.asReadonly();

  readonly tier = computed<Tier>(() => this.tierFor(this.width()));

  /** Masonry & justified need their own markup; everything else is a 12-column grid. */
  readonly mode = computed<'grid' | 'justified' | 'masonry' | 'columns' | 'strip'>(() => {
    const l = this._layout();
    if (l === 'asymmetric') return 'justified';
    if (l === 'masonry') return 'masonry';
    if (l === 'staggered') return 'columns';
    if (l === 'carousel') return 'strip';
    return 'grid';
  });

  /** Staggered columns: photos dealt left-to-right into columns; every other column is offset. */
  readonly staggerColumns = computed<{ index: number }[][]>(() => {
    const n = this._images().length;
    const count = Math.max(1, Math.min(this.tier() === 'sm' ? 2 : 3, n));
    const cols: { index: number }[][] = Array.from({ length: count }, () => []);
    for (let i = 0; i < n; i++) cols[i % count].push({ index: i });
    return cols;
  });

  readonly staggerRatio = computed(() => this.tileRatio());

  /** Filmstrip: equal-height frames, width follows each photo's shape. */
  readonly stripHeight = computed(() => (this.tier() === 'lg' ? 380 : this.tier() === 'md' ? 300 : 230));

  readonly masonryColumns = computed(() => {
    const t = this.tier();
    const n = this._images().length;
    const cols = t === 'lg' ? 3 : t === 'md' ? 2 : 1;
    return Math.max(1, Math.min(cols, n));
  });

  readonly cells = computed<GridCell[]>(() => {
    const imgs = this._images();
    const tier = this.tier();
    const layout = this._layout();
    if (!imgs.length) return [];

    const tile = this.tileRatio();
    switch (layout) {
      case 'grid-2':
        return this.uniformGrid(0, imgs.length, tier === 'sm' ? 1 : 2, tile);
      case 'featured': {
        const lead = this.clamp(this.ratioAt(0, 1.5), 1.75, 2.4);
        const first: GridCell = { index: 0, column: '1 / -1', row: null, aspect: lead };
        return [first, ...this.uniformGrid(1, imgs.length, tier === 'sm' ? 2 : 3, tile)];
      }
      case 'full-width':
        return imgs.map((_, i) => ({
          index: i,
          column: '1 / -1',
          row: null,
          aspect: this.clamp(this.ratioAt(i, 1.5), 1.5, 3), // kept landscape so a single photo never fills the screen
        }));
      case 'editorial':
        return this.mosaic(imgs.length, tier);
      case 'grid-4':
        return this.uniformGrid(0, imgs.length, tier === 'sm' ? 2 : 4, tile);
      case 'bento':
        return this.bento(imgs.length, tier, tile);
      case 'split':
        return this.split(imgs.length, tier, tile);
      default:
        return this.uniformGrid(0, imgs.length, tier === 'sm' ? 2 : 3, tile);
    }
  });

  readonly justifiedRows = computed<JustifiedRow[]>(() => {
    const imgs = this._images();
    if (!imgs.length) return [];
    const target = this.tier() === 'lg' ? 3.2 : this.tier() === 'md' ? 2.3 : 1.4;
    const rows: JustifiedRow[] = [];
    let current: { index: number; ratio: number }[] = [];
    let sum = 0;
    imgs.forEach((_, i) => {
      const r = this.clamp(this.ratioAt(i, 1.33), 0.5, 3);
      current.push({ index: i, ratio: r });
      sum += r;
      if (sum >= target) {
        rows.push({ items: current, fill: true, target });
        current = [];
        sum = 0;
      }
    });
    if (current.length) {
      rows.push({ items: current, fill: sum >= target * 0.6, target });
    }
    return rows;
  });

  readonly activeImage = computed(() => {
    const i = this.lightboxIndex();
    return i === null ? null : (this._images()[i] ?? null);
  });

  ngAfterViewInit(): void {
    const el = this.host.nativeElement as HTMLElement;
    this.width.set(el.clientWidth || 960);
    if (typeof ResizeObserver === 'undefined') return;
    this.zone.runOutsideAngular(() => {
      this.observer = new ResizeObserver((entries) => {
        const w = Math.round(entries[0]?.contentRect.width || 0);
        if (!w) return;
        // Only re-lay out when a column breakpoint is actually crossed, and do it on the next
        // frame — changing the layout inside the observer callback (e.g. when the page scrollbar
        // appears) is what triggers "ResizeObserver loop completed with undelivered notifications".
        if (this.tierFor(w) === this.tierFor(this.width())) return;
        cancelAnimationFrame(this.resizeFrame);
        this.resizeFrame = requestAnimationFrame(() => this.zone.run(() => this.width.set(w)));
      });
      this.observer.observe(el);
    });
  }

  private resizeFrame = 0;

  private tierFor(w: number): Tier {
    return w >= 720 ? 'lg' : w >= 440 ? 'md' : 'sm';
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    cancelAnimationFrame(this.resizeFrame);
  }

  /* ---------------- layout helpers ---------------- */

  private clamp(v: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, v));
  }

  private ratioAt(i: number, fallback: number): number {
    const url = this._images()[i]?.imageUrl;
    return (url && this.ratios()[url]) || fallback;
  }

  /**
   * One shared tile shape for uniform grids, picked from the photos themselves:
   * mostly portrait → 4:5, mostly landscape → 4:3, otherwise square.
   */
  private tileRatio(): number {
    const known = Object.values(this.ratios());
    const urls = new Set(this._images().map((i) => i.imageUrl));
    const mine = Object.entries(this.ratios())
      .filter(([u]) => urls.has(u))
      .map(([, r]) => r)
      .sort((a, b) => a - b);
    if (!mine.length && !known.length) return 1;
    const list = mine.length ? mine : known;
    const median = list[Math.floor(list.length / 2)];
    if (median < 0.9) return 4 / 5;
    if (median > 1.15) return 4 / 3;
    return 1;
  }

  /** Equal tiles in `cols` columns; a short last row stretches to fill the width at the same height. */
  private uniformGrid(from: number, to: number, cols: number, ratio: number): GridCell[] {
    const cells: GridCell[] = [];
    const count = to - from;
    const full = count - (count % cols);
    for (let k = 0; k < count; k++) {
      const index = from + k;
      if (k < full) {
        cells.push({ index, column: `span ${12 / cols}`, row: null, aspect: ratio });
      } else {
        const rest = count - full;
        // wider tiles keep the same row height: aspect grows with the span
        cells.push({
          index,
          column: `span ${12 / rest}`,
          row: null,
          aspect: (ratio * cols) / rest,
        });
      }
    }
    return cells;
  }

  /**
   * Mosaic (daras "tiled gallery"): [large + 2 stacked squares] then [full-width], mirrored each time.
   */
  private mosaic(count: number, tier: Tier): GridCell[] {
    const cells: GridCell[] = [];
    let i = 0;
    let mirror = false;
    const sm = tier === 'sm';
    while (i < count) {
      const left = count - i;
      if (left >= 3) {
        if (sm) {
          cells.push({ index: i, column: '1 / -1', row: null, aspect: 1.25 });
          cells.push({ index: i + 1, column: 'span 6', row: null, aspect: 1 });
          cells.push({ index: i + 2, column: 'span 6', row: null, aspect: 1 });
        } else if (!mirror) {
          cells.push({ index: i, column: '1 / span 8', row: 'span 2', aspect: null });
          cells.push({ index: i + 1, column: '9 / span 4', row: null, aspect: 1 });
          cells.push({ index: i + 2, column: '9 / span 4', row: null, aspect: 1 });
        } else {
          cells.push({ index: i, column: '1 / span 4', row: null, aspect: 1 });
          cells.push({ index: i + 1, column: '1 / span 4', row: null, aspect: 1 });
          cells.push({ index: i + 2, column: '5 / span 8', row: 'span 2', aspect: null });
        }
        i += 3;
        // a full-width frame follows — unless exactly two remain, which read better as a pair
        if (i < count && count - i !== 2) {
          cells.push({
            index: i,
            column: '1 / -1',
            row: null,
            aspect: this.clamp(this.ratioAt(i, 2), 2, 2.6), // slim panorama between mosaic blocks
          });
          i += 1;
        }
        mirror = !mirror;
      } else if (left === 2) {
        cells.push({ index: i, column: 'span 6', row: null, aspect: 1.5 });
        cells.push({ index: i + 1, column: 'span 6', row: null, aspect: 1.5 });
        i += 2;
      } else {
        cells.push({
          index: i,
          column: '1 / -1',
          row: null,
          aspect: this.clamp(this.ratioAt(i, 2), 2, 2.6), // slim panorama between mosaic blocks
        });
        i += 1;
      }
    }
    return cells;
  }

  /**
   * Bento: blocks of 6 — [large 2×2 | square, square / wide] then [square | wide], mirrored each block.
   */
  private bento(count: number, tier: Tier, tile: number): GridCell[] {
    const cells: GridCell[] = [];
    let i = 0;
    let mirror = false;
    if (tier === 'sm') {
      for (let k = 0; k < count; k++) {
        const wide = k % 3 === 0;
        cells.push({ index: k, column: wide ? '1 / -1' : 'span 6', row: null, aspect: wide ? 1.6 : 1 });
      }
      return cells;
    }
    while (count - i >= 6) {
      const L = mirror ? 7 : 1; // large tile column start
      const S = mirror ? 1 : 7; // small tiles column start
      cells.push({ index: i, column: `${L} / span 6`, row: 'span 2', aspect: null });
      cells.push({ index: i + 1, column: `${S} / span 3`, row: null, aspect: 1 });
      cells.push({ index: i + 2, column: `${S + 3} / span 3`, row: null, aspect: 1 });
      cells.push({ index: i + 3, column: `${S} / span 6`, row: null, aspect: 2.04 }); // 2:1 plus the gap it spans
      cells.push({ index: i + 4, column: mirror ? '9 / span 4' : '1 / span 4', row: null, aspect: 1 });
      cells.push({ index: i + 5, column: mirror ? '1 / span 8' : '5 / span 8', row: null, aspect: 2.03 });
      i += 6;
      mirror = !mirror;
    }
    return [...cells, ...this.uniformGrid(i, count, 3, tile)];
  }

  /** Feature split: blocks of 5 — one large photo beside a 2×2 grid, mirrored each block. */
  private split(count: number, tier: Tier, tile: number): GridCell[] {
    const cells: GridCell[] = [];
    let i = 0;
    let mirror = false;
    while (count - i >= 5) {
      if (tier === 'sm') {
        cells.push({ index: i, column: '1 / -1', row: null, aspect: 1.1 });
        for (let k = 1; k <= 4; k++) cells.push({ index: i + k, column: 'span 6', row: null, aspect: 1 });
      } else {
        const big = mirror ? '7 / span 6' : '1 / span 6';
        const a = mirror ? 1 : 7;
        cells.push({ index: i, column: big, row: 'span 2', aspect: null });
        cells.push({ index: i + 1, column: `${a} / span 3`, row: null, aspect: 1 });
        cells.push({ index: i + 2, column: `${a + 3} / span 3`, row: null, aspect: 1 });
        cells.push({ index: i + 3, column: `${a} / span 3`, row: null, aspect: 1 });
        cells.push({ index: i + 4, column: `${a + 3} / span 3`, row: null, aspect: 1 });
      }
      i += 5;
      mirror = !mirror;
    }
    return [...cells, ...this.uniformGrid(i, count, tier === 'sm' ? 2 : 3, tile)];
  }

  /** Filmstrip scroll buttons. */
  scrollStrip(el: HTMLElement, dir: 1 | -1): void {
    el.scrollBy({ left: dir * el.clientWidth * 0.8, behavior: 'smooth' });
  }

  /* ---------------- events ---------------- */

  onImageLoad(event: Event, url: string): void {
    const img = event.target as HTMLImageElement;
    img?.classList.add('is-loaded'); // fades the photo in
    // ignore the 'image unavailable' placeholder — it must not decide the grid's tile shape
    if (!img?.naturalWidth || !img?.naturalHeight || img.dataset['failed']) return;
    const r = img.naturalWidth / img.naturalHeight;
    if (Math.abs((this.ratios()[url] || 0) - r) < 0.01) return;
    this.ratios.update((m) => ({ ...m, [url]: r }));
  }

  onImageError(event: Event): void {
    const target = event.target as HTMLImageElement;
    target?.classList.add('is-loaded');
    if (target && !target.dataset['failed']) {
      target.dataset['failed'] = '1';
      target.src =
        'data:image/svg+xml;charset=UTF-8,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 600 400%22%3E%3Crect width=%22600%22 height=%22400%22 fill=%22%23efe8dc%22/%3E%3Ctext x=%22300%22 y=%22208%22 text-anchor=%22middle%22 font-family=%22sans-serif%22 font-size=%2218%22 fill=%22%238a817d%22%3EImage unavailable%3C/text%3E%3C/svg%3E';
    }
  }

  openLightbox(index: number): void {
    if (!this.interactive || !this._images().length) return;
    this.lightboxIndex.set(Math.max(0, Math.min(index, this._images().length - 1)));
  }

  closeLightbox(): void {
    this.lightboxIndex.set(null);
  }

  nextImage(): void {
    const n = this._images().length;
    const i = this.lightboxIndex();
    if (n && i !== null) this.lightboxIndex.set((i + 1) % n);
  }

  prevImage(): void {
    const n = this._images().length;
    const i = this.lightboxIndex();
    if (n && i !== null) this.lightboxIndex.set((i - 1 + n) % n);
  }

  /** Known natural shape of a photo (masonry reserves this space before the photo loads). */
  ratioOf(url: string): number {
    return this.ratios()[url] || 4 / 5;
  }

  /** Filmstrip frame shape, kept between portrait and panorama so the strip stays even. */
  stripRatio(url: string): number {
    return this.clamp(this.ratios()[url] || 4 / 3, 0.6, 2);
  }

  altFor(i: number): string {
    const img = this._images()[i];
    return img?.caption || this.sectionTitle || img?.originalFileName || `Photograph ${i + 1}`;
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (this.lightboxIndex() === null) return;
    if (event.key === 'Escape') this.closeLightbox();
    else if (event.key === 'ArrowRight') this.nextImage();
    else if (event.key === 'ArrowLeft') this.prevImage();
  }
}
