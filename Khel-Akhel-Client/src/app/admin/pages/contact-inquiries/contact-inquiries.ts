import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';
import { AdminContactInquiryService } from '../../services/admin-contact-inquiry.service';
import {
  ContactInquiryItem,
  ContactInquiryListFilter,
  ContactInquirySummary,
} from '../../models/contact-inquiry.model';
import { BreadcrumbComponent } from '../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../common/models/breadcrumb.model';
import { AppLoaderComponent } from '../../../common/components/app-loader/app-loader';
import { PaginationComponent } from '../../../common/components/pagination/pagination';
import { ConfirmationDialogComponent } from '../../../common/components/confirmation-dialog/confirmation-dialog';

@Component({
  selector: 'app-contact-inquiries',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DatePipe,
    BreadcrumbComponent,
    AppLoaderComponent,
    PaginationComponent,
    ConfirmationDialogComponent,
  ],
  templateUrl: './contact-inquiries.html',
  styleUrl: './contact-inquiries.scss',
})
export class ContactInquiriesComponent implements OnInit, OnDestroy {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Contact Inquiries' },
  ];

  private inquiryService = inject(AdminContactInquiryService);
  private cdr = inject(ChangeDetectorRef);

  // Data lists
  inquiries: ContactInquiryItem[] = [];
  summary: ContactInquirySummary = {
    totalInquiries: 0,
    activeInquiries: 0,
    inactiveInquiries: 0,
    todayInquiries: 0,
  };

  // Pagination & Filtering
  pageIndex = 1;
  pageSize = 10;
  totalRecords = 0;

  searchQuery = '';
  sortBy = 'newest';

  // Search Debounce
  private searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;

  // States
  loading = false;
  successMessage = '';
  errorMessage = '';
  copyFeedback = '';

  // Detail Modal State
  isDetailOpen = false;
  selectedInquiry: ContactInquiryItem | null = null;
  detailLoading = false;

  // Confirmation Dialog State
  isConfirmOpen = false;
  confirmTitle = '';
  confirmMessage = '';
  inquiryToActOn: ContactInquiryItem | null = null;
  statusSubmitting = false;

  get hasActiveFilters(): boolean {
    return !!this.searchQuery.trim() || this.sortBy !== 'newest';
  }

  ngOnInit(): void {
    this.searchSubscription = this.searchSubject
      .pipe(debounceTime(400), distinctUntilChanged())
      .subscribe((query) => {
        this.searchQuery = query;
        this.pageIndex = 1;
        this.loadInquiries();
      });

    this.loadSummary();
    this.loadInquiries();
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
  }

  loadSummary(): void {
    this.inquiryService.getSummary().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.summary = res.data;
          this.cdr.markForCheck();
        }
      },
      error: () => {},
    });
  }

  loadInquiries(): void {
    this.loading = true;
    this.errorMessage = '';

    const filter: ContactInquiryListFilter = {
      pageIndex: this.pageIndex,
      pageSize: this.pageSize,
      search: this.searchQuery,
      sortBy: this.sortBy,
    };

    this.inquiryService.getList(filter).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success && res.data) {
          this.inquiries = res.data.items || [];
          this.totalRecords = res.data.totalRecords || 0;
        } else {
          this.errorMessage = res.message || 'Failed to load contact inquiries.';
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage =
          err?.error?.message || 'A network error occurred while loading contact inquiries.';
        this.cdr.markForCheck();
      },
    });
  }

  onSearchInput(val: string): void {
    this.searchSubject.next(val);
  }

  onSortChange(sort: string): void {
    this.sortBy = sort;
    this.pageIndex = 1;
    this.loadInquiries();
  }

  applyFilters(): void {
    this.pageIndex = 1;
    this.loadInquiries();
  }

  resetFilters(): void {
    this.searchQuery = '';
    this.sortBy = 'newest';
    this.pageIndex = 1;
    this.loadInquiries();
  }

  onPageChange(newPageIndex: number): void {
    this.pageIndex = newPageIndex;
    this.loadInquiries();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  // --- Detail Modal ---
  openDetail(inquiry: ContactInquiryItem): void {
    this.selectedInquiry = inquiry;
    this.isDetailOpen = true;
    this.detailLoading = true;

    this.inquiryService.getById(inquiry.encryptedInquiryId).subscribe({
      next: (res) => {
        this.detailLoading = false;
        if (res.success && res.data) {
          this.selectedInquiry = res.data;
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.detailLoading = false;
        this.cdr.markForCheck();
      },
    });
  }

  closeDetail(): void {
    this.isDetailOpen = false;
    this.selectedInquiry = null;
  }

  // --- Delete ---
  promptDelete(inquiry: ContactInquiryItem, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.inquiryToActOn = inquiry;
    this.confirmTitle = 'Delete Contact Inquiry?';
    this.confirmMessage = `Are you sure you want to permanently delete the inquiry submitted by "${inquiry.fullName}" (${inquiry.email})? This action cannot be undone.`;
    this.isConfirmOpen = true;
  }

  onConfirmAction(): void {
    if (!this.inquiryToActOn) return;

    this.statusSubmitting = true;

    this.inquiryService.delete(this.inquiryToActOn.encryptedInquiryId).subscribe({
      next: (res) => {
        this.statusSubmitting = false;
        this.isConfirmOpen = false;
        if (res.success) {
          this.showSuccess('Contact inquiry deleted successfully.');
          if (
            this.isDetailOpen &&
            this.selectedInquiry &&
            this.selectedInquiry.encryptedInquiryId === this.inquiryToActOn?.encryptedInquiryId
          ) {
            this.closeDetail();
          }
          this.inquiryToActOn = null;
          this.loadSummary();
          this.loadInquiries();
        } else {
          this.showError(res.message || 'Failed to delete contact inquiry.');
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.statusSubmitting = false;
        this.isConfirmOpen = false;
        this.showError(err?.error?.message || 'Server error occurred while deleting inquiry.');
        this.cdr.markForCheck();
      },
    });
  }

  onCancelConfirm(): void {
    this.isConfirmOpen = false;
    this.inquiryToActOn = null;
    this.statusSubmitting = false;
  }

  copyToClipboard(text: string, label: string): void {
    if (!text) return;
    navigator.clipboard.writeText(text).then(() => {
      this.copyFeedback = `Copied ${label} to clipboard!`;
      this.cdr.markForCheck();
      setTimeout(() => {
        this.copyFeedback = '';
        this.cdr.markForCheck();
      }, 2500);
    });
  }

  private showSuccess(msg: string): void {
    this.successMessage = msg;
    setTimeout(() => {
      this.successMessage = '';
      this.cdr.markForCheck();
    }, 4000);
  }

  private showError(msg: string): void {
    this.errorMessage = msg;
    setTimeout(() => {
      this.errorMessage = '';
      this.cdr.markForCheck();
    }, 6000);
  }
}
