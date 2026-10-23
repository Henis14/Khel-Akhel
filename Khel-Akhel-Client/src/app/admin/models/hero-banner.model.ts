export interface HeroBannerItem {
  id: number;
  encryptedId: string;
  title?: string | null;
  mediaUrl: string;
  isImage: boolean;
  isVideo: boolean;
  displayOrder: number;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface HeroBannerCreateRequest {
  title?: string;
  displayOrder?: number;
  replaceExisting?: boolean;
  isImage?: boolean;
  isVideo?: boolean;
}

export interface HeroBannerUpdateRequest {
  title?: string;
  displayOrder?: number;
  isActive?: boolean;
}
