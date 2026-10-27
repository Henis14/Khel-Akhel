import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormsModule,
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';
import { AuthService } from '../../../common/services/auth.service';
import { BreadcrumbComponent } from '../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../common/models/breadcrumb.model';
import { PaginationComponent } from '../../../common/components/pagination/pagination';
import { CustomValidators } from '../../../common/validators/custom.validators';

export interface PasswordResetItem {
  requestId: string;
  customerId: string;
  accountNo: string;
  fullName: string;
  email: string;
  mobileNo: string;
  status: string;
  source: string;
  requestedDate: string;
  requestedIp?: string;
  issuedByAdminName?: string;
  issuedDate?: string;
  tokenExpiresDate?: string;
  adminNotes?: string;
}

@Component({
  selector: 'app-password-reset-requests',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    BreadcrumbComponent,
    PaginationComponent,
  ],
  templateUrl: './password-reset-requests.html',
  styleUrl: './password-reset-requests.scss',
})
export class PasswordResetRequestsComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);

  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Password Reset Requests' },
  ];

  requests: PasswordResetItem[] = [];
  loading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  // Filters
  selectedStatus: string = 'PENDING';
  searchQuery: string = '';
  pageIndex: number = 1;
  pageSize: number = 10;
  totalRecords: number = 0;
  pendingCount: number = 0;

  // Search Debounce
  private searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;

  // Modals
  showVerifyModal = signal<boolean>(false);
  showRejectModal = signal<boolean>(false);
  selectedRequest: PasswordResetItem | null = null;
  verifyForm!: FormGroup;
  rejectForm!: FormGroup;
  actionLoading = signal<boolean>(false);
  actionError = signal<string | null>(null);

  // Generated Link (Held in component memory only)
  generatedLink = signal<string | null>(null);
  generatedExpiry = signal<string | null>(null);
  copySuccess = signal<boolean>(false);

  ngOnInit(): void {
    this.verifyForm = this.fb.group({
      adminNotes: [
        '',
        [
          CustomValidators.requiredNonEmpty,
          Validators.minLength(5),
          Validators.maxLength(1000),
        ],
      ],
    });

    this.rejectForm = this.fb.group({
      adminNotes: [
        '',
        [
          CustomValidators.requiredNonEmpty,
          Validators.minLength(5),
          Validators.maxLength(1000),
        ],
      ],
    });

    this.searchSubscription = this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged())
      .subscribe((query) => {
        this.searchQuery = query;
        this.pageIndex = 1;
        this.loadRequests();
      });

    this.loadRequests();
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
  }

  onSearchChange(value: string): void {
    this.searchSubject.next(value);
  }

  onStatusChange(status: string): void {
    this.selectedStatus = status;
    this.pageIndex = 1;
    this.loadRequests();
  }

  onPageChange(page: number): void {
    this.pageIndex = page;
    this.loadRequests();
  }

  loadRequests(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.authService
      .getPasswordResetRequests(
        this.selectedStatus,
        this.pageIndex,
        this.pageSize,
        this.searchQuery.trim() || undefined,
      )
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          if (res.success && res.data) {
            this.requests = res.data.requests || [];
            this.totalRecords = res.data.totalRecords || 0;
            this.pendingCount = res.data.pendingCount || 0;
          }
        },
        error: (err) => {
          this.loading.set(false);
          this.errorMessage.set(
            err.error?.errors?.[0] || 'Failed to load password reset requests.',
          );
        },
      });
  }

  openVerifyModal(item: PasswordResetItem): void {
    this.selectedRequest = item;
    this.generatedLink.set(null);
    this.generatedExpiry.set(null);
    this.copySuccess.set(false);
    this.actionError.set(null);
    this.verifyForm.reset();
    this.showVerifyModal.set(true);
  }

  closeVerifyModal(): void {
    this.showVerifyModal.set(false);
    this.selectedRequest = null;
    this.generatedLink.set(null);
    this.generatedExpiry.set(null);
    this.copySuccess.set(false);
  }

  submitIssueLink(): void {
    if (this.verifyForm.invalid || this.actionLoading() || !this.selectedRequest) {
      this.verifyForm.markAllAsTouched();
      return;
    }

    this.actionLoading.set(true);
    this.actionError.set(null);

    const notes = this.verifyForm.value.adminNotes.trim();

    this.authService.issueResetLink(this.selectedRequest.requestId, notes).subscribe({
      next: (res) => {
        this.actionLoading.set(false);
        if (res.success && res.data) {
          this.generatedLink.set(res.data.resetUrl);
          this.generatedExpiry.set(res.data.expiresUtc);
          this.loadRequests();
        }
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.actionError.set(
          err.error?.errors?.[0] || err.error?.message || 'Failed to issue reset link.',
        );
      },
    });
  }

  copyLink(): void {
    const link = this.generatedLink();
    if (!link) return;

    if (navigator.clipboard) {
      navigator.clipboard.writeText(link).then(() => {
        this.copySuccess.set(true);
        setTimeout(() => this.copySuccess.set(false), 2500);
      });
    }
  }

  openRejectModal(item: PasswordResetItem): void {
    this.selectedRequest = item;
    this.actionError.set(null);
    this.rejectForm.reset();
    this.showRejectModal.set(true);
  }

  closeRejectModal(): void {
    this.showRejectModal.set(false);
    this.selectedRequest = null;
  }

  submitReject(): void {
    if (this.rejectForm.invalid || this.actionLoading() || !this.selectedRequest) {
      this.rejectForm.markAllAsTouched();
      return;
    }

    this.actionLoading.set(true);
    this.actionError.set(null);

    const notes = this.rejectForm.value.adminNotes.trim();

    this.authService.rejectResetRequest(this.selectedRequest.requestId, notes).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.closeRejectModal();
        this.loadRequests();
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.actionError.set(
          err.error?.errors?.[0] || err.error?.message || 'Failed to reject request.',
        );
      },
    });
  }
}
