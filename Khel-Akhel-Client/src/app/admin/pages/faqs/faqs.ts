import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';
import { AdminFaqService } from '../../services/admin-faq.service';
import {
  FaqAdminFilter,
  FaqCreateRequest,
  FaqResponse,
  FaqUpdateRequest,
} from '../../models/faq.model';
import { CustomValidators } from '../../../common/validators/custom.validators';
import { BreadcrumbComponent } from '../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../common/models/breadcrumb.model';
import { AppLoaderComponent } from '../../../common/components/app-loader/app-loader';
import { PaginationComponent } from '../../../common/components/pagination/pagination';
import { ConfirmationDialogComponent } from '../../../common/components/confirmation-dialog/confirmation-dialog';

@Component({
  selector: 'app-faqs',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    BreadcrumbComponent,
    AppLoaderComponent,
    PaginationComponent,
    ConfirmationDialogComponent,
  ],
  templateUrl: './faqs.html',
  styleUrl: './faqs.scss',
})
export class FaqsComponent implements OnInit, OnDestroy {
  breadcrumbs: BreadcrumbItem[] = [{ label: 'Home', url: '/admin/dashboard' }, { label: 'FAQs' }];

  private faqService = inject(AdminFaqService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);

  // Raw and filtered lists
  allFaqs: FaqResponse[] = [];
  filteredFaqs: FaqResponse[] = [];
  pagedFaqs: FaqResponse[] = [];
  categoriesList: string[] = [];

  // Filter toolbar
  searchQuery: string = '';
  selectedCategory: string = '';
  /** 'all' | 'live' (active + published) | 'draft' (not published) | 'inactive' */
  statusFilter: string = 'all';

  // Search debounce
  private searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;

  // Pagination
  pageIndex: number = 1;
  pageSize: number = 10;
  totalRecords: number = 0;

  // View States
  loading: boolean = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  // Create / Edit Modal
  isModalOpen: boolean = false;
  isEditMode: boolean = false;
  selectedFaq: FaqResponse | null = null;
  faqForm!: FormGroup;
  formSubmitting: boolean = false;

  // Details Modal
  isDetailModalOpen: boolean = false;
  detailFaq: FaqResponse | null = null;

  // Confirmation Dialog
  isConfirmOpen: boolean = false;
  confirmTitle: string = '';
  confirmMessage: string = '';
  isActionSubmitting: boolean = false;
  pendingAction: (() => void) | null = null;

  ngOnInit(): void {
    this.initForm();
    this.setupSearchDebounce();
    this.loadFaqs();
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
  }

  private initForm(): void {
    this.faqForm = this.fb.group({
      question: [
        '',
        [
          CustomValidators.requiredNonEmpty,
          CustomValidators.pattern(/^[\s\S]{3,500}$/, 'invalidLength'),
        ],
      ],
      answer: ['', [CustomValidators.requiredNonEmpty]],
      category: ['', [CustomValidators.pattern(/^[\s\S]{0,100}$/, 'invalidLength')]],
      displayOrder: [0, [CustomValidators.nonNegativeInteger]],
      isActive: [true],
      isPublished: [true],
      displayOnHomePage: [false],
      displayOnFaqPage: [true],
      displayOnCheckoutPage: [false],
      displayOnContactPage: [false],
      displayOnFooter: [false],
    });
  }

  private setupSearchDebounce(): void {
    this.searchSubscription = this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged())
      .subscribe((query) => {
        this.searchQuery = query;
        this.pageIndex = 1;
        this.loadFaqs();
      });
  }

  onSearchInput(value: string): void {
    this.searchSubject.next(value);
  }

  loadFaqs(): void {
    this.loading = true;
    this.errorMessage = null;

    const filter: FaqAdminFilter = {};
    if (this.searchQuery && this.searchQuery.trim().length > 0) {
      filter.search = this.searchQuery.trim();
    }
    if (this.statusFilter === 'live') {
      filter.isActive = true;
      filter.isPublished = true;
    } else if (this.statusFilter === 'draft') {
      filter.isPublished = false;
    } else if (this.statusFilter === 'inactive') {
      filter.isActive = false;
    }

    this.faqService.getAdminList(filter).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success && res.data) {
          this.allFaqs = res.data;
          this.extractCategories(res.data);
          this.applyClientFilters();
        } else {
          this.allFaqs = [];
          this.filteredFaqs = [];
          this.pagedFaqs = [];
          this.totalRecords = 0;
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Failed to load FAQs.';
        this.allFaqs = [];
        this.filteredFaqs = [];
        this.pagedFaqs = [];
        this.totalRecords = 0;
        this.cdr.markForCheck();
      },
    });
  }

  private extractCategories(faqs: FaqResponse[]): void {
    const cats = new Set<string>();
    faqs.forEach((f) => {
      if (f.category && f.category.trim().length > 0) {
        cats.add(f.category.trim());
      }
    });
    this.categoriesList = Array.from(cats).sort((a, b) => a.localeCompare(b));
  }

  applyClientFilters(): void {
    let result = [...this.allFaqs];

    // Filter by Category
    if (this.selectedCategory) {
      result = result.filter(
        (f) => (f.category || '').toLowerCase() === this.selectedCategory.toLowerCase(),
      );
    }


    this.filteredFaqs = result;
    this.totalRecords = result.length;

    // Adjust page if pageIndex is out of range
    const maxPage = Math.ceil(this.totalRecords / this.pageSize) || 1;
    if (this.pageIndex > maxPage) {
      this.pageIndex = maxPage;
    }

    this.updatePagedFaqs();
  }

  updatePagedFaqs(): void {
    const start = (this.pageIndex - 1) * this.pageSize;
    const end = start + this.pageSize;
    this.pagedFaqs = this.filteredFaqs.slice(start, end);
    this.cdr.markForCheck();
  }

  onPageChange(newPage: number): void {
    this.pageIndex = newPage;
    this.updatePagedFaqs();
  }

  onFilterChange(): void {
    this.pageIndex = 1;
    this.loadFaqs();
  }

  onClientFilterChange(): void {
    this.pageIndex = 1;
    this.applyClientFilters();
  }

  resetFilters(): void {
    this.searchQuery = '';
    this.selectedCategory = '';
    this.statusFilter = 'all';
    this.pageIndex = 1;
    this.loadFaqs();
  }

  // Create Modal
  openCreateModal(): void {
    this.isEditMode = false;
    this.selectedFaq = null;
    this.errorMessage = null;
    this.successMessage = null;

    // Suggest next display order
    const nextOrder =
      this.allFaqs.length > 0 ? Math.max(...this.allFaqs.map((f) => f.displayOrder || 0)) + 1 : 1;

    this.faqForm.reset({
      question: '',
      answer: '',
      category: '',
      displayOrder: nextOrder,
      isActive: true,
      isPublished: true,
      displayOnHomePage: false,
      displayOnFaqPage: true,
      displayOnCheckoutPage: false,
      displayOnContactPage: false,
      displayOnFooter: false,
    });
    this.isModalOpen = true;
  }

  // Edit Modal
  openEditModal(faq: FaqResponse): void {
    this.isEditMode = true;
    this.selectedFaq = faq;
    this.errorMessage = null;
    this.successMessage = null;

    this.faqForm.patchValue({
      question: faq.question,
      answer: faq.answer,
      category: faq.category || '',
      displayOrder: faq.displayOrder,
      isActive: faq.isActive,
      isPublished: faq.isPublished,
      displayOnHomePage: faq.displayOnHomePage,
      displayOnFaqPage: faq.displayOnFaqPage,
      displayOnCheckoutPage: faq.displayOnCheckoutPage,
      displayOnContactPage: faq.displayOnContactPage,
      displayOnFooter: faq.displayOnFooter,
    });
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
    this.selectedFaq = null;
  }

  saveFaq(): void {
    if (this.faqForm.invalid) {
      this.faqForm.markAllAsTouched();
      return;
    }

    this.formSubmitting = true;
    this.errorMessage = null;

    const formVal = this.faqForm.value;
    const commonPayload: FaqCreateRequest = {
      question: formVal.question.trim(),
      answer: formVal.answer.trim(),
      category: formVal.category?.trim() || undefined,
      displayOrder: Number(formVal.displayOrder) || 0,
      isActive: !!formVal.isActive,
      isPublished: !!formVal.isPublished,
      displayOnHomePage: !!formVal.displayOnHomePage,
      displayOnFaqPage: !!formVal.displayOnFaqPage,
      displayOnCheckoutPage: !!formVal.displayOnCheckoutPage,
      displayOnContactPage: !!formVal.displayOnContactPage,
      displayOnFooter: !!formVal.displayOnFooter,
    };

    if (this.isEditMode && this.selectedFaq) {
      const updatePayload: FaqUpdateRequest = {
        ...commonPayload,
        encryptedFaqId: this.selectedFaq.encryptedFaqId,
      };

      this.faqService.update(updatePayload).subscribe({
        next: (res) => {
          this.formSubmitting = false;
          if (res.success) {
            this.successMessage = 'FAQ updated successfully.';
            this.closeModal();
            this.loadFaqs();
          } else {
            this.errorMessage = res.message || 'Failed to update FAQ.';
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.formSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to update FAQ.';
          this.cdr.markForCheck();
        },
      });
    } else {
      this.faqService.create(commonPayload).subscribe({
        next: (res) => {
          this.formSubmitting = false;
          if (res.success) {
            this.successMessage = 'FAQ created successfully.';
            this.closeModal();
            this.loadFaqs();
          } else {
            this.errorMessage = res.message || 'Failed to create FAQ.';
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.formSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to create FAQ.';
          this.cdr.markForCheck();
        },
      });
    }
  }

  // Quick Status Toggle (Active)
  toggleStatus(faq: FaqResponse, event: Event): void {
    event.stopPropagation();
    const updatedStatus = !faq.isActive;
    const payload: FaqUpdateRequest = {
      encryptedFaqId: faq.encryptedFaqId,
      question: faq.question,
      answer: faq.answer,
      category: faq.category,
      displayOrder: faq.displayOrder,
      isActive: updatedStatus,
      isPublished: faq.isPublished,
      displayOnHomePage: faq.displayOnHomePage,
      displayOnFaqPage: faq.displayOnFaqPage,
      displayOnCheckoutPage: faq.displayOnCheckoutPage,
      displayOnContactPage: faq.displayOnContactPage,
      displayOnFooter: faq.displayOnFooter,
    };

    this.faqService.update(payload).subscribe({
      next: (res) => {
        if (res.success) {
          faq.isActive = updatedStatus;
          this.successMessage = `FAQ status set to ${updatedStatus ? 'Active' : 'Inactive'}.`;
          this.applyClientFilters();
        } else {
          this.errorMessage = res.message || 'Failed to toggle status.';
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Failed to update status.';
        this.cdr.markForCheck();
      },
    });
  }

  // Quick Status Toggle (Published)
  togglePublished(faq: FaqResponse, event: Event): void {
    event.stopPropagation();
    const updatedPublished = !faq.isPublished;
    const payload: FaqUpdateRequest = {
      encryptedFaqId: faq.encryptedFaqId,
      question: faq.question,
      answer: faq.answer,
      category: faq.category,
      displayOrder: faq.displayOrder,
      isActive: faq.isActive,
      isPublished: updatedPublished,
      displayOnHomePage: faq.displayOnHomePage,
      displayOnFaqPage: faq.displayOnFaqPage,
      displayOnCheckoutPage: faq.displayOnCheckoutPage,
      displayOnContactPage: faq.displayOnContactPage,
      displayOnFooter: faq.displayOnFooter,
    };

    this.faqService.update(payload).subscribe({
      next: (res) => {
        if (res.success) {
          faq.isPublished = updatedPublished;
          this.successMessage = `FAQ is now ${updatedPublished ? 'Published' : 'Unpublished'}.`;
          this.applyClientFilters();
        } else {
          this.errorMessage = res.message || 'Failed to toggle published status.';
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Failed to update published status.';
        this.cdr.markForCheck();
      },
    });
  }

  // View Details Modal
  openDetailModal(faq: FaqResponse): void {
    this.detailFaq = faq;
    this.isDetailModalOpen = true;
  }

  closeDetailModal(): void {
    this.isDetailModalOpen = false;
    this.detailFaq = null;
  }

  // Delete Action
  confirmDelete(faq: FaqResponse): void {
    this.confirmTitle = 'Delete FAQ';
    this.confirmMessage = `Are you sure you want to delete this FAQ: "${faq.question}"?`;
    this.pendingAction = () => {
      this.isActionSubmitting = true;
      this.faqService.delete(faq.encryptedFaqId).subscribe({
        next: (res) => {
          this.isActionSubmitting = false;
          this.isConfirmOpen = false;
          if (res.success) {
            this.successMessage = 'FAQ deleted successfully.';
            this.loadFaqs();
          } else {
            this.errorMessage = res.message || 'Failed to delete FAQ.';
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isActionSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to delete FAQ.';
          this.cdr.markForCheck();
        },
      });
    };
    this.isConfirmOpen = true;
  }

  onConfirmAction(): void {
    if (this.pendingAction) {
      this.pendingAction();
    }
  }

  onCancelAction(): void {
    if (!this.isActionSubmitting) {
      this.isConfirmOpen = false;
      this.pendingAction = null;
    }
  }
}
