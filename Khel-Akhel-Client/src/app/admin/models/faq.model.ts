export interface FaqCreateRequest {
  category?: string;
  question: string;
  answer: string;
  displayOrder: number;
  isPublished: boolean;
  isActive: boolean;
  displayOnHomePage?: boolean;
  displayOnFaqPage?: boolean;
  displayOnCheckoutPage?: boolean;
  displayOnContactPage?: boolean;
  displayOnFooter?: boolean;
}

export interface FaqUpdateRequest extends FaqCreateRequest {
  encryptedFaqId: string;
}

export interface FaqResponse {
  encryptedFaqId: string;
  category?: string;
  question: string;
  answer: string;
  displayOrder: number;
  isPublished: boolean;
  isActive: boolean;
  displayOnHomePage: boolean;
  displayOnFaqPage: boolean;
  displayOnCheckoutPage: boolean;
  displayOnContactPage: boolean;
  displayOnFooter: boolean;
  createdDate?: string;
  modifiedDate?: string;
}

export interface FaqAdminFilter {
  search?: string;
  isActive?: boolean;
  isPublished?: boolean;
  page?: number;
  pageSize?: number;
  location?: string;
}
