export interface AboutImage {
  encryptedImageId: string;
  encryptedAboutId: string;
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

export interface AboutImageOrderDto {
  encryptedImageId: string;
  displayOrder: number;
}

export interface AboutDetail {
  encryptedAboutId: string;
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
  images: AboutImage[];
  createdDate?: string | null;
  modifiedDate?: string | null;
}

export interface AboutPublic {
  encryptedAboutId: string;
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
  images: AboutImage[];
}

export interface AboutListFilter {
  pageIndex?: number;
  pageSize?: number;
  search?: string;
  contentType?: string;
  isPublished?: boolean;
  isActive?: boolean;
  sortBy?: string;
}

export interface AboutListResponse {
  items: AboutDetail[];
  totalRecords: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
}

export interface AboutCreatePayload {
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

export interface AboutUpdatePayload {
  encryptedAboutId: string;
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
  imageOrders?: AboutImageOrderDto[];
}

export interface GalleryLayoutOption {
  id: string;
  title: string;
  badge: string;
  description: string;
  recommendedImages: string;
  presetKey: string;
  thumbnailClass: string;
}

export const GALLERY_LAYOUT_OPTIONS: GalleryLayoutOption[] = [
  {
    id: 'grid-3',
    title: 'Three-Column Grid',
    badge: 'Layout A',
    description: 'Uniform 3-column arrangement with consistent aspect proportions. Ideal for artwork series, print editions, and visual collections.',
    recommendedImages: '3, 6, 9+ images',
    presetKey: 'uniform-3col',
    thumbnailClass: 'thumb-grid-3',
  },
  {
    id: 'grid-2',
    title: 'Two-Column Grid',
    badge: 'Layout B',
    description: 'Balanced 2-column presentation offering larger photographic scale and detail visibility for key artwork pieces.',
    recommendedImages: '2, 4, 6+ images',
    presetKey: 'uniform-2col',
    thumbnailClass: 'thumb-grid-2',
  },
  {
    id: 'editorial',
    title: 'Asymmetric Editorial',
    badge: 'Layout C',
    description: 'Dynamic magazine-style composition pairing tall hero images with staggered secondary visual blocks.',
    recommendedImages: '3 to 7 images',
    presetKey: 'editorial-asym',
    thumbnailClass: 'thumb-editorial',
  },
  {
    id: 'featured',
    title: 'Featured + Supporting',
    badge: 'Layout D',
    description: 'One prominent centerpiece image highlighted by a balanced constellation of supporting detail images.',
    recommendedImages: '3 to 5 images',
    presetKey: 'featured-focus',
    thumbnailClass: 'thumb-featured',
  },
  {
    id: 'full-width',
    title: 'Full-Width Showcase',
    badge: 'Layout E',
    description: 'Expansive wide-span featured image spanning the full editorial width, accompanied by supporting photo rows.',
    recommendedImages: '1 to 5 images',
    presetKey: 'panoramic-hero',
    thumbnailClass: 'thumb-full-width',
  },
  {
    id: 'masonry',
    title: 'Masonry Flow',
    badge: 'Layout F',
    description: 'Natural vertical columns that honor the native aspect ratio and vertical orientation of every photograph without cropping.',
    recommendedImages: '4 to 12 images',
    presetKey: 'masonry-cascade',
    thumbnailClass: 'thumb-masonry',
  },
  {
    id: 'asymmetric',
    title: 'Asymmetric Dual Spread',
    badge: 'Layout G',
    description: 'Editorial cadence combining wide horizontal landscape frames with narrow vertical portrait frames in an alternating rhythm.',
    recommendedImages: '2 to 8 images',
    presetKey: 'asymmetric-duo',
    thumbnailClass: 'thumb-asymmetric',
  },
];

export const CONTENT_TYPES: string[] = [
  'Studio Narrative',
  'Art & Creative Practice',
  'Architecture & Spatial Design',
  'Craft & Material Culture',
  'Cultural & Festival Experience',
  'Film, Sound & Media',
  'Exhibition & Gallery',
  'Research & Documentation',
  'Collaborative Commission',
  'Publication & Editorial',
];
