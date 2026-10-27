export interface ProjectImage {
  encryptedImageId: string;
  encryptedProjectId: string;
  originalFileName: string;
  storedFileName: string;
  imageUrl: string;
  imagePath: string;
  fileSize: number;
  contentType: string;
  displayOrder: number;
  isActive: boolean;
  createdDate?: string | null;
}

export interface ProjectImageOrderDto {
  encryptedImageId: string;
  displayOrder: number;
}

export interface ProjectDetail {
  encryptedProjectId: string;
  heading: string;
  subheading?: string | null;
  contentType?: string | null;
  projectOrEventTitle?: string | null;
  displayYear?: string | null;
  shortDescription?: string | null;
  detailedDescription?: string | null;
  credits?: string | null;
  location?: string | null;
  externalUrl?: string | null;
  displayOrder: number;
  layoutType: string;
  isPublished: boolean;
  isActive: boolean;
  imageCount: number;
  images: ProjectImage[];
  createdDate?: string | null;
  modifiedDate?: string | null;
}

export interface ProjectPublic {
  encryptedProjectId: string;
  heading: string;
  subheading?: string | null;
  contentType?: string | null;
  projectOrEventTitle?: string | null;
  displayYear?: string | null;
  shortDescription?: string | null;
  detailedDescription?: string | null;
  credits?: string | null;
  location?: string | null;
  externalUrl?: string | null;
  displayOrder: number;
  layoutType: string;
  images: ProjectImage[];
}

export interface ProjectListFilter {
  pageIndex?: number;
  pageSize?: number;
  search?: string;
  contentType?: string;
  isPublished?: boolean;
  isActive?: boolean;
  sortBy?: string;
}

export interface ProjectListResponse {
  items: ProjectDetail[];
  totalRecords: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
}

export interface ProjectCreatePayload {
  heading: string;
  subheading?: string | null;
  contentType?: string | null;
  projectOrEventTitle?: string | null;
  displayYear?: string | null;
  shortDescription?: string | null;
  detailedDescription?: string | null;
  credits?: string | null;
  location?: string | null;
  externalUrl?: string | null;
  displayOrder: number;
  layoutType: string;
  isPublished: boolean;
  isActive: boolean;
  images?: File[];
}

export interface ProjectUpdatePayload {
  encryptedProjectId: string;
  heading: string;
  subheading?: string | null;
  contentType?: string | null;
  projectOrEventTitle?: string | null;
  displayYear?: string | null;
  shortDescription?: string | null;
  detailedDescription?: string | null;
  credits?: string | null;
  location?: string | null;
  externalUrl?: string | null;
  displayOrder: number;
  layoutType: string;
  isPublished?: boolean;
  isActive?: boolean;
  newImages?: File[];
  removeImageIds?: string[];
  imageOrders?: ProjectImageOrderDto[];
}

export interface GalleryLayoutOption {
  id: string;
  title: string;
  badge: string;
  description: string;
  recommendedImages: string;
  presetKey: string;
  thumbnailClass: string;
  /** Picker section: grids, editorial compositions or natural-proportion layouts. */
  group?: 'grids' | 'editorial' | 'natural';
}

export const GALLERY_LAYOUT_GROUPS: { id: 'grids' | 'editorial' | 'natural'; title: string; hint: string }[] = [
  { id: 'grids', title: 'Grids', hint: 'Even tiles — calm and orderly. The tile shape follows your photos.' },
  { id: 'editorial', title: 'Editorial compositions', hint: 'Mixed tile sizes that give one or two photos the spotlight.' },
  { id: 'natural', title: 'Natural proportions', hint: 'Photos keep their real shape — nothing important is cropped.' },
];

export const GALLERY_LAYOUT_OPTIONS: GalleryLayoutOption[] = [
  /* ---------- Grids ---------- */
  {
    id: 'grid-3',
    group: 'grids',
    title: 'Three-column grid',
    badge: 'Classic',
    description:
      'The everyday daras.co.in gallery. Equal tiles in three columns; a short last row stretches to fill.',
    recommendedImages: '3, 6, 9… or any count',
    presetKey: 'uniform-3col',
    thumbnailClass: 'thumb-grid-3',
  },
  {
    id: 'grid-4',
    group: 'grids',
    title: 'Four-column grid',
    badge: 'Catalogue',
    description:
      'Smaller tiles, four across — a tidy contact sheet for projects with many photos.',
    recommendedImages: '8, 12, 16…',
    presetKey: 'uniform-4col',
    thumbnailClass: 'thumb-grid-4',
  },
  {
    id: 'grid-2',
    group: 'grids',
    title: 'Two-column pairs',
    badge: 'Pairs',
    description:
      'Photos side by side in pairs — bigger, calmer frames for murals, wall pieces and before/after views.',
    recommendedImages: '2, 4, 6…',
    presetKey: 'uniform-2col',
    thumbnailClass: 'thumb-grid-2',
  },
  /* ---------- Editorial ---------- */
  {
    id: 'featured',
    group: 'editorial',
    title: 'Lead image + grid',
    badge: 'Lead',
    description:
      'The first photo opens wide; the rest follow in three columns. A hero view of a set or installation.',
    recommendedImages: '4, 7, 10…',
    presetKey: 'lead-grid',
    thumbnailClass: 'thumb-featured',
  },
  {
    id: 'editorial',
    group: 'editorial',
    title: 'Mosaic',
    badge: 'Tiled',
    description:
      'A large tile with two stacked squares, then a full-width frame — mirrored as it repeats, like daras.co.in tiled galleries.',
    recommendedImages: '4, 8, 12…',
    presetKey: 'mosaic',
    thumbnailClass: 'thumb-editorial',
  },
  {
    id: 'bento',
    group: 'editorial',
    title: 'Bento grid',
    badge: 'Bento',
    description:
      'Large, square and wide tiles locked together into one neat block — lively but perfectly aligned.',
    recommendedImages: '6, 12, 18…',
    presetKey: 'bento',
    thumbnailClass: 'thumb-bento',
  },
  {
    id: 'split',
    group: 'editorial',
    title: 'Feature split',
    badge: 'Split',
    description:
      'One large photo beside a 2×2 grid of details, switching sides for each set of five.',
    recommendedImages: '5, 10, 15',
    presetKey: 'split',
    thumbnailClass: 'thumb-split',
  },
  {
    id: 'staggered',
    group: 'editorial',
    title: 'Staggered columns',
    badge: 'Spread',
    description:
      'Three columns with the middle one dropped — a relaxed, magazine-style rhythm for portraits.',
    recommendedImages: '6 or more',
    presetKey: 'staggered',
    thumbnailClass: 'thumb-staggered',
  },
  /* ---------- Natural proportions ---------- */
  {
    id: 'asymmetric',
    group: 'natural',
    title: 'Justified rows',
    badge: 'Natural',
    description:
      'Rows of photos at their real proportions with edges aligned on both sides — neat for mixed shapes.',
    recommendedImages: '4 or more',
    presetKey: 'justified',
    thumbnailClass: 'thumb-asymmetric',
  },
  {
    id: 'masonry',
    group: 'natural',
    title: 'Masonry',
    badge: 'Uncropped',
    description:
      'Columns of uncropped photos at their natural height — mixes portrait and landscape shots freely.',
    recommendedImages: '5 or more',
    presetKey: 'masonry',
    thumbnailClass: 'thumb-masonry',
  },
  {
    id: 'full-width',
    group: 'natural',
    title: 'Full-width stack',
    badge: 'Stack',
    description: 'One photo per row across the full width. For a few strong images or a single wide view.',
    recommendedImages: '1 to 4',
    presetKey: 'stack',
    thumbnailClass: 'thumb-full-width',
  },
  {
    id: 'carousel',
    group: 'natural',
    title: 'Filmstrip',
    badge: 'Scroll',
    description:
      'A single scrolling row at equal height — visitors swipe through. Keeps long projects compact.',
    recommendedImages: '5 or more',
    presetKey: 'filmstrip',
    thumbnailClass: 'thumb-carousel',
  },
];

/**
 * Project disciplines — mirrors the sections on daras.co.in and the storefront "Projects" menu.
 * The storefront filters /projects?type=<slug> by slugifying these names, so keep them in sync.
 */
export interface ContentTypeGroup {
  group: string;
  types: string[];
}

export const CONTENT_TYPE_GROUPS: ContentTypeGroup[] = [
  {
    group: 'Spaces & play',
    types: ['Festivals & Sets', 'Interactive Art', 'Objects & Forms', 'Wearable Art', 'Architecture'],
  },
  {
    group: 'Image & sound',
    types: ['Visual Art Works', 'Photo Art Works', 'Film Art Works', 'Soundscapes'],
  },
  {
    group: 'Circles & learning',
    types: ['Circle: Sawal-e-Safar', 'E7: LTA Workshop', 'Shilp Khel', 'Exhibitions', 'Publications'],
  },
];

export const CONTENT_TYPES: string[] = CONTENT_TYPE_GROUPS.flatMap((g) => g.types);

/** "Festivals & Sets" -> "festivals-and-sets" (same rule the storefront menu uses). */
export function slugifyProjectText(value: string | null | undefined): string {
  return (value || '')
    .toLowerCase()
    .replace(/&/g, ' and ')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

/* --------------------------------------------------------------------------
   Layout slots — where each image lands in a gallery layout and what shape
   that frame is. Mirrors common/components/gallery-renderer/gallery-renderer.scss
   -------------------------------------------------------------------------- */
export interface LayoutSlot {
  label: string;
  /** width / height of the frame; 0 = natural (no crop) */
  ratio: number;
  ratioLabel: string;
}

const R = (w: number, h: number, label: string): LayoutSlot => ({
  label,
  ratio: w / h,
  ratioLabel: `${w}:${h}`,
});

export function getLayoutSlot(layout: string, index: number): LayoutSlot {
  const n = index + 1;
  switch ((layout || 'grid-3').toLowerCase()) {
    case 'grid-2':
      return { label: 'Pair tile', ratio: 0, ratioLabel: 'shared shape' };
    case 'editorial': {
      // blocks of 4: large, square, square, full-width (mirrored every other block)
      const m = ((n - 1) % 4) + 1;
      if (m === 4) return R(3, 2, 'Full-width frame');
      const mirrored = Math.floor((n - 1) / 4) % 2 === 1;
      const largePos = mirrored ? 3 : 1;
      return m === largePos ? R(1, 1, 'Large tile') : R(1, 1, 'Small square');
    }
    case 'featured':
      return n === 1
        ? { label: 'Lead · own shape', ratio: 0, ratioLabel: 'natural' }
        : { label: 'Grid tile', ratio: 0, ratioLabel: 'shared shape' };
    case 'full-width':
      return { label: 'Full width · uncropped', ratio: 0, ratioLabel: 'natural' };
    case 'masonry':
      return { label: 'Column · uncropped', ratio: 0, ratioLabel: 'natural' };
    case 'asymmetric':
      return { label: 'Row · own shape', ratio: 0, ratioLabel: 'natural' };
    case 'grid-4':
      return { label: 'Grid tile', ratio: 0, ratioLabel: 'shared shape' };
    case 'bento': {
      const m = ((n - 1) % 6) + 1;
      if (m === 1) return R(1, 1, 'Large tile');
      if (m === 4 || m === 6) return R(2, 1, 'Wide frame');
      return R(1, 1, 'Square');
    }
    case 'split':
      return ((n - 1) % 5) + 1 === 1 ? R(1, 1, 'Feature') : R(1, 1, 'Detail square');
    case 'staggered':
      return { label: 'Column tile', ratio: 0, ratioLabel: 'shared shape' };
    case 'carousel':
      return { label: 'Strip frame · own shape', ratio: 0, ratioLabel: 'natural' };
    default:
      return { label: 'Grid tile', ratio: 0, ratioLabel: 'shared shape' };
  }
}

export interface LayoutFit {
  state: 'empty' | 'good' | 'ok' | 'warn';
  message: string;
}

/** How well the current image count suits a layout, with a concrete suggestion. */
export function getLayoutFit(layout: string, count: number): LayoutFit {
  if (count === 0) return { state: 'empty', message: 'Add photographs to see how they fill this layout.' };
  const rows = (cols: number, label = 'row'): LayoutFit => {
    const r = count % cols;
    return r === 0
      ? { state: 'good', message: `Every ${label} is complete — balanced and even.` }
      : {
          state: 'ok',
          message: `The last ${label} has ${r} of ${cols}; it will stretch to fill the width. Add ${cols - r} for an even grid.`,
        };
  };
  switch ((layout || 'grid-3').toLowerCase()) {
    case 'grid-2':
      return rows(2, 'pair');
    case 'featured':
      if (count === 1) return { state: 'ok', message: 'Only the lead image. Add 3 more for the grid below it.' };
      return (count - 1) % 3 === 0
        ? { state: 'good', message: 'Lead image plus complete rows of three.' }
        : { state: 'ok', message: `The last row has ${(count - 1) % 3} of 3; it will stretch to fill.` };
    case 'editorial':
      if (count < 4) return { state: 'warn', message: `The mosaic needs 4 photos for its pattern. Add ${4 - count}.` };
      return count % 4 === 0
        ? { state: 'good', message: 'Every mosaic block is complete.' }
        : { state: 'ok', message: `The pattern repeats every 4 photos — add ${4 - (count % 4)} to complete the last block.` };
    case 'full-width':
      return count <= 4
        ? { state: 'good', message: 'A short stack of strong images — reads well.' }
        : { state: 'warn', message: 'A long stack makes visitors scroll a lot. Consider Mosaic or Justified rows.' };
    case 'masonry':
      return count >= 5
        ? { state: 'good', message: 'Masonry keeps every photo uncropped — any count works.' }
        : { state: 'ok', message: 'Masonry looks best with 5 or more photos.' };
    case 'grid-4':
      return rows(4);
    case 'bento':
      if (count < 6) return { state: 'warn', message: `The bento block needs 6 photos. Add ${6 - count}.` };
      return count % 6 === 0
        ? { state: 'good', message: 'Every bento block is complete.' }
        : { state: 'ok', message: `${count % 6} extra photo(s) will follow in a 3-column row. Add ${6 - (count % 6)} for another full block.` };
    case 'split':
      if (count < 5) return { state: 'warn', message: `Feature split needs 5 photos (1 large + 4 details). Add ${5 - count}.` };
      return count % 5 === 0
        ? { state: 'good', message: 'Every feature + 2×2 block is complete.' }
        : { state: 'ok', message: `${count % 5} extra photo(s) will follow in a row. Add ${5 - (count % 5)} for another block.` };
    case 'staggered':
      return count >= 6
        ? { state: 'good', message: 'Columns will be balanced with the middle one dropped.' }
        : { state: 'ok', message: 'Staggered columns look best with 6 or more photos.' };
    case 'carousel':
      return count >= 4
        ? { state: 'good', message: 'Visitors will swipe through the strip — any count works.' }
        : { state: 'ok', message: 'A filmstrip makes most sense with 4 or more photos.' };
    case 'asymmetric':
      return count >= 4
        ? { state: 'good', message: 'Rows will be packed to their real proportions automatically.' }
        : { state: 'ok', message: 'Justified rows look best with 4 or more photos.' };
    default:
      return rows(3);
  }
}

// Compatibility aliases during transition
export type AboutImage = ProjectImage;
export type AboutImageOrderDto = ProjectImageOrderDto;
export type AboutDetail = ProjectDetail;
export type AboutPublic = ProjectPublic;
export type AboutListFilter = ProjectListFilter;
export type AboutListResponse = ProjectListResponse;
export type AboutCreatePayload = ProjectCreatePayload;
export type AboutUpdatePayload = ProjectUpdatePayload;
