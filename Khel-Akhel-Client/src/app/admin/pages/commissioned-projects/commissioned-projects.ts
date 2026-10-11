import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';
import { AdminCommissionedProjectService } from '../../services/admin-commissioned-project.service';
import {
  CommissionCategory,
  CommissionImage,
  CommissionRequestDetail,
  CommissionRequestListItem,
  COMMISSION_STATUSES,
  CommissionStatus,
} from '../../models/commissioned-project.model';
import { BreadcrumbComponent } from '../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../common/models/breadcrumb.model';
import { AppLoaderComponent } from '../../../common/components/app-loader/app-loader';
import { PaginationComponent } from '../../../common/components/pagination/pagination';
import { ConfirmationDialogComponent } from '../../../common/components/confirmation-dialog/confirmation-dialog';

@Component({
  selector: 'app-commissioned-projects',
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
  templateUrl: './commissioned-projects.html',
  styleUrl: './commissioned-projects.scss',
})
export class CommissionedProjectsComponent implements OnInit, OnDestroy {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Commissioned Projects' },
  ];

  private commissionService = inject(AdminCommissionedProjectService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);

  // Data lists
  requests: CommissionRequestListItem[] = [];
  categories: CommissionCategory[] = [];
  readonly availableStatuses = COMMISSION_STATUSES;

  // Pagination & Filtering
  pageIndex = 1;
  pageSize = 10;
  totalRecords = 0;

  searchQuery = '';
  selectedCategory = '';
  selectedStatus = '';
  startDate = '';
  endDate = '';
  sortBy = 'newest';
  dateRangeError: string | null = null;

  // Search Debounce
  private searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;

  // States
  loading = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  // Request Details Modal
  isDetailModalOpen = false;
  detailLoading = false;
  selectedDetail: CommissionRequestDetail | null = null;

  // Status & Notes Update Modal
  isStatusModalOpen = false;
  statusForm!: FormGroup;
  statusSubmitting = false;
  targetRequestForUpdate: {
    encryptedRequestId: string;
    requestNo: string;
    currentStatus: string;
    currentNotes?: string | null;
  } | null = null;

  // Image Lightbox Preview
  isLightboxOpen = false;
  activeLightboxImage: CommissionImage | null = null;

  // Confirmation Dialog
  isConfirmOpen = false;
  confirmTitle = '';
  confirmMessage = '';
  pendingConfirmAction: (() => void) | null = null;

  // Copy Feedback
  copyFeedback: string | null = null;

  ngOnInit(): void {
    this.initStatusForm();
    this.setupSearchDebounce();
    this.loadCategories();
    this.loadRequests();
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
  }

  private initStatusForm(): void {
    this.statusForm = this.fb.group({
      status: ['', [Validators.required]],
      adminNotes: ['', [Validators.maxLength(2000)]],
    });
  }

  private setupSearchDebounce(): void {
    this.searchSubscription = this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged())
      .subscribe((query) => {
        this.searchQuery = query;
        this.pageIndex = 1;
        this.loadRequests();
      });
  }

  loadCategories(): void {
    this.commissionService.getCategories().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.categories = res.data;
          this.cdr.markForCheck();
        }
      },
      error: () => {
        // Fallback or silent fail; categories not critical to block requests
      },
    });
  }

  loadRequests(): void {
    this.loading = true;
    this.errorMessage = null;

    this.commissionService
      .getList({
        pageIndex: this.pageIndex,
        pageSize: this.pageSize,
        search: this.searchQuery,
        encryptedCategoryId: this.selectedCategory || undefined,
        status: this.selectedStatus || undefined,
        startDate: this.startDate || undefined,
        endDate: this.endDate || undefined,
        sortBy: this.sortBy,
      })
      .subscribe({
        next: (res) => {
          this.loading = false;
          if (res.success && res.data) {
            this.requests = res.data.items || [];
            this.totalRecords = res.data.totalRecords || 0;
          } else {
            this.requests = [];
            this.totalRecords = 0;
            this.errorMessage = res.message || 'Failed to load commissioned project requests.';
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.loading = false;
          this.requests = [];
          this.totalRecords = 0;
          this.errorMessage =
            err.error?.message || 'Server error occurred while loading requests. Please retry.';
          this.cdr.markForCheck();
        },
      });
  }

  onSearchInput(value: string): void {
    this.searchSubject.next(value);
  }

  onCategoryFilter(catId: string): void {
    this.selectedCategory = catId;
    this.pageIndex = 1;
    this.loadRequests();
  }

  onStatusFilter(status: string): void {
    this.selectedStatus = status;
    this.pageIndex = 1;
    this.loadRequests();
  }

  validateDateRange(): boolean {
    if (this.startDate && this.endDate && this.startDate > this.endDate) {
      this.dateRangeError = 'From date cannot be later than To date.';
      return false;
    }
    this.dateRangeError = null;
    return true;
  }

  onDateFilter(): void {
    if (!this.validateDateRange()) {
      return;
    }
    this.pageIndex = 1;
    this.loadRequests();
  }

  applyFilters(): void {
    if (!this.validateDateRange()) {
      return;
    }
    this.pageIndex = 1;
    this.loadRequests();
  }

  onSortChange(sortBy: string): void {
    this.sortBy = sortBy;
    this.pageIndex = 1;
    this.loadRequests();
  }

  resetFilters(): void {
    this.searchQuery = '';
    this.selectedCategory = '';
    this.selectedStatus = '';
    this.startDate = '';
    this.endDate = '';
    this.sortBy = 'newest';
    this.dateRangeError = null;
    this.pageIndex = 1;
    this.loadRequests();
  }

  get hasActiveFilters(): boolean {
    return !!(
      this.searchQuery ||
      this.selectedCategory ||
      this.selectedStatus ||
      this.startDate ||
      this.endDate ||
      this.sortBy !== 'newest'
    );
  }

  onPageChange(newPage: number): void {
    this.pageIndex = newPage;
    this.loadRequests();
  }

  // View Details Modal
  openDetailModal(item: CommissionRequestListItem): void {
    this.isDetailModalOpen = true;
    this.detailLoading = true;
    this.selectedDetail = null;

    this.commissionService.getById(item.encryptedRequestId).subscribe({
      next: (res) => {
        this.detailLoading = false;
        if (res.success && res.data) {
          this.selectedDetail = res.data;
        } else {
          this.errorMessage = res.message || 'Failed to retrieve commission details.';
          this.closeDetailModal();
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.detailLoading = false;
        this.errorMessage = err.error?.message || 'Failed to load request details.';
        this.closeDetailModal();
        this.cdr.markForCheck();
      },
    });
  }

  closeDetailModal(): void {
    this.isDetailModalOpen = false;
    this.selectedDetail = null;
    this.detailLoading = false;
  }

  // Update Status Modal
  openStatusModal(target: {
    encryptedRequestId: string;
    requestNo: string;
    currentStatus: string;
    currentNotes?: string | null;
  }): void {
    this.targetRequestForUpdate = target;
    this.statusForm.reset({
      status: target.currentStatus,
      adminNotes: target.currentNotes || '',
    });
    this.isStatusModalOpen = true;
  }

  closeStatusModal(): void {
    this.isStatusModalOpen = false;
    this.targetRequestForUpdate = null;
    this.statusSubmitting = false;
  }

  submitStatusUpdate(): void {
    if (!this.targetRequestForUpdate || this.statusForm.invalid) {
      this.statusForm.markAllAsTouched();
      return;
    }

    const newStatus = this.statusForm.value.status;
    const isMajorStatusChange = newStatus === 'Rejected' || newStatus === 'Cancelled';

    if (isMajorStatusChange && this.targetRequestForUpdate.currentStatus !== newStatus) {
      this.confirmTitle = `Confirm Status Change: ${newStatus}`;
      this.confirmMessage = `Are you sure you want to mark request ${this.targetRequestForUpdate.requestNo} as "${newStatus}"?`;
      this.pendingConfirmAction = () => this.executeStatusUpdate();
      this.isConfirmOpen = true;
      return;
    }

    this.executeStatusUpdate();
  }

  private executeStatusUpdate(): void {
    if (!this.targetRequestForUpdate) return;

    this.statusSubmitting = true;
    this.errorMessage = null;
    this.successMessage = null;

    const reqData = {
      encryptedRequestId: this.targetRequestForUpdate.encryptedRequestId,
      status: this.statusForm.value.status,
      adminNotes: this.statusForm.value.adminNotes?.trim() || null,
    };

    this.commissionService.updateStatus(reqData).subscribe({
      next: (res) => {
        this.statusSubmitting = false;
        this.closeStatusModal();

        if (res.success) {
          this.successMessage = `Request ${reqData.status} status updated successfully.`;

          // If detail modal is currently showing this request, update it live
          if (
            this.selectedDetail &&
            this.selectedDetail.encryptedRequestId === reqData.encryptedRequestId
          ) {
            this.selectedDetail.status = reqData.status;
            this.selectedDetail.adminNotes = reqData.adminNotes;
          }

          // Reload list to update row
          this.loadRequests();
        } else {
          this.errorMessage = res.message || 'Failed to update commission request status.';
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.statusSubmitting = false;
        this.errorMessage = err.error?.message || 'Server error occurred while updating status.';
        this.cdr.markForCheck();
      },
    });
  }

  onConfirmAction(): void {
    this.isConfirmOpen = false;
    if (this.pendingConfirmAction) {
      this.pendingConfirmAction();
      this.pendingConfirmAction = null;
    }
  }

  onCancelConfirm(): void {
    this.isConfirmOpen = false;
    this.pendingConfirmAction = null;
  }

  // Lightbox Image Preview
  openLightbox(image: CommissionImage): void {
    this.activeLightboxImage = image;
    this.isLightboxOpen = true;
  }

  closeLightbox(): void {
    this.isLightboxOpen = false;
    this.activeLightboxImage = null;
  }

  // Utility helpers
  getImageUrl(path: string | null | undefined): string {
    return this.commissionService.getImageUrl(path);
  }

  onImageError(event: Event): void {
    const target = event.target as HTMLImageElement;
    target.src =
      'data:image/svg+xml;charset=UTF-8,%3Csvg%20width%3D%22200%22%20height%3D%22200%22%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F200%2Fsvg%22%20viewBox%3D%220%200%2024%2024%22%20fill%3D%22none%22%20stroke%3D%22%238a887f%22%20stroke-width%3D%221.5%22%3E%3Crect%20x%3D%223%22%20y%3D%223%22%20width%3D%2218%22%20height%3D%2218%22%20rx%3D%222%22%20ry%3D%222%22%2F%3E%3Ccircle%20cx%3D%228.5%22%20cy%3D%228.5%22%20r%3D%221.5%22%2F%3E%3Cpolyline%20points%3D%2221%2015%2016%2010%205%2021%22%2F%3E%3C%2Fsvg%3E';
  }

  formatFileSize(bytes: number): string {
    if (!bytes || bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Submitted':
        return 'badge-info';
      case 'Under Review':
        return 'badge-warning';
      case 'Contacted':
        return 'badge-primary';
      case 'In Progress':
        return 'badge-teal';
      case 'Completed':
        return 'badge-success';
      case 'Rejected':
        return 'badge-danger';
      case 'Cancelled':
        return 'badge-secondary';
      default:
        return 'badge-secondary';
    }
  }

  copyToClipboard(text: string, label: string): void {
    if (navigator?.clipboard?.writeText) {
      navigator.clipboard.writeText(text).then(() => {
        this.copyFeedback = `Copied ${label}!`;
        setTimeout(() => {
          this.copyFeedback = null;
          this.cdr.markForCheck();
        }, 2200);
        this.cdr.markForCheck();
      });
    }
  }
}
