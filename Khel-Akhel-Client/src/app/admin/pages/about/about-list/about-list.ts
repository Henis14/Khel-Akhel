import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';
import { AdminAboutService } from '../../../services/admin-about.service';
import {
  AboutDetail,
  AboutListFilter,
  GALLERY_LAYOUT_OPTIONS,
} from '../../../models/about.model';
import { BreadcrumbComponent } from '../../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../../common/models/breadcrumb.model';
import { AppLoaderComponent } from '../../../../common/components/app-loader/app-loader';
import { PaginationComponent } from '../../../../common/components/pagination/pagination';
import { ConfirmationDialogComponent } from '../../../../common/components/confirmation-dialog/confirmation-dialog';
import { GalleryRendererComponent } from '../../../../common/components/gallery-renderer/gallery-renderer';

@Component({
  selector: 'app-about-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    BreadcrumbComponent,
    AppLoaderComponent,
    PaginationComponent,
    ConfirmationDialogComponent,
    GalleryRendererComponent,
  ],
  templateUrl: './about-list.html',
  styleUrl: './about-list.scss',
})
export class AboutListComponent implements OnInit, OnDestroy {
  private aboutService = inject(AdminAboutService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'About Sections' },
  ];

  // Data
  sections: AboutDetail[] = [];
  readonly layoutPresets = GALLERY_LAYOUT_OPTIONS;

  // Pagination & Filtering
  pageIndex = 1;
  pageSize = 10;
  totalRecords = 0;

  searchQuery = '';
  selectedContentType = '';
  selectedPublished = '';
  sortBy = 'orderAsc';

  // Search debounce
  private searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;

  // UI States
  loading = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  // Confirmation dialog
  isConfirmOpen = false;
  confirmTitle = '';
  confirmMessage = '';
  itemToDelete: AboutDetail | null = null;

  // Live preview modal
  previewModalOpen = false;
  previewSection: AboutDetail | null = null;

  ngOnInit(): void {
    this.searchSubscription = this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged())
      .subscribe(() => {
        this.pageIndex = 1;
        this.loadSections();
      });

    this.loadSections();
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
  }

  onSearchChange(text: string): void {
    this.searchSubject.next(text);
  }

  onFilterChange(): void {
    this.pageIndex = 1;
    this.loadSections();
  }

  onPageChange(page: number): void {
    this.pageIndex = page;
    this.loadSections();
  }

  loadSections(): void {
    this.loading = true;
    this.errorMessage = null;

    const filter: AboutListFilter = {
      pageIndex: this.pageIndex,
      pageSize: this.pageSize,
      search: this.searchQuery,
      contentType: this.selectedContentType || undefined,
      sortBy: this.sortBy,
    };

    if (this.selectedPublished === 'published') {
      filter.isPublished = true;
    } else if (this.selectedPublished === 'draft') {
      filter.isPublished = false;
    }

    this.aboutService.getList(filter).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success && res.data) {
          this.sections = res.data.items || [];
          this.totalRecords = res.data.totalRecords || 0;
        } else {
          this.errorMessage = res.message || 'Failed to load About content sections.';
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage =
          err?.error?.message ||
          'A network error occurred while retrieving About content sections.';
        this.cdr.markForCheck();
      },
    });
  }

  // Publication Toggle
  togglePublish(section: AboutDetail): void {
    const newStatus = !section.isPublished;
    this.loading = true;

    this.aboutService.togglePublish(section.encryptedAboutId, newStatus).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success) {
          section.isPublished = newStatus;
          this.successMessage = `"${section.heading}" is now ${newStatus ? 'published' : 'saved as draft'}.`;
          this.autoClearSuccess();
        } else {
          this.errorMessage = res.message || 'Could not update publication status.';
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err?.error?.message || 'Failed to update publication status.';
        this.cdr.markForCheck();
      },
    });
  }

  // Delete
  promptDelete(section: AboutDetail): void {
    this.itemToDelete = section;
    this.confirmTitle = 'Delete About Section';
    this.confirmMessage = `Are you sure you want to permanently remove "${section.heading}"? Associated image files will be archived.`;
    this.isConfirmOpen = true;
  }

  onConfirmDelete(): void {
    if (!this.itemToDelete) return;
    const sec = this.itemToDelete;
    this.isConfirmOpen = false;
    this.loading = true;

    this.aboutService.delete(sec.encryptedAboutId).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success) {
          this.successMessage = `Section "${sec.heading}" was deleted successfully.`;
          this.autoClearSuccess();
          this.loadSections();
        } else {
          this.errorMessage = res.message || 'Failed to delete section.';
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err?.error?.message || 'An error occurred while deleting section.';
        this.cdr.markForCheck();
      },
    });
  }

  onCancelDelete(): void {
    this.isConfirmOpen = false;
    this.itemToDelete = null;
  }

  // Live Preview Modal
  openPreview(section: AboutDetail): void {
    this.previewSection = section;
    this.previewModalOpen = true;
  }

  closePreview(): void {
    this.previewModalOpen = false;
    this.previewSection = null;
  }

  // Navigation
  navigateToAdd(): void {
    this.router.navigate(['/admin/about/create']);
  }

  navigateToEdit(section: AboutDetail): void {
    this.router.navigate(['/admin/about/edit', section.encryptedAboutId]);
  }

  getLayoutBadge(layoutId: string): string {
    const found = this.layoutPresets.find((p) => p.id === layoutId?.toLowerCase());
    return found ? `${found.badge}: ${found.title}` : layoutId || 'Grid-3';
  }

  getImageThumb(section: AboutDetail): string | null {
    if (section.images && section.images.length > 0) {
      return this.aboutService.getImageUrl(
        section.images[0].imageUrl || section.images[0].imagePath
      );
    }
    return null;
  }

  getPreviewImages(section: AboutDetail) {
    if (!section.images) return [];
    return section.images.map((img) => ({
      imageUrl: this.aboutService.getImageUrl(img.imageUrl || img.imagePath),
      originalFileName: img.originalFileName,
      displayOrder: img.displayOrder,
    }));
  }

  private autoClearSuccess(): void {
    setTimeout(() => {
      this.successMessage = null;
      this.cdr.markForCheck();
    }, 4500);
  }
}
