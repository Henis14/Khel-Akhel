import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminHeroBannerService } from '../../services/admin-hero-banner.service';
import { HeroBannerItem } from '../../models/hero-banner.model';
import { BreadcrumbComponent } from '../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../common/models/breadcrumb.model';
import { ConfirmationDialogComponent } from '../../../common/components/confirmation-dialog/confirmation-dialog';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-hero-banner',
  standalone: true,
  imports: [CommonModule, FormsModule, BreadcrumbComponent, ConfirmationDialogComponent],
  templateUrl: './hero-banner.html',
  styleUrl: './hero-banner.scss',
})
export class HeroBannerComponent implements OnInit {
  private heroBannerService = inject(AdminHeroBannerService);

  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Admin', url: '/admin/dashboard' },
    { label: 'Hero Section' },
  ];

  // State
  banners = signal<HeroBannerItem[]>([]);
  isLoading = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);
  successMessage = signal<string | null>(null);
  errorMessage = signal<string | null>(null);

  // Modals
  showUploadModal = false;
  showEditModal = false;
  showDeleteDialog = false;
  itemToDelete: HeroBannerItem | null = null;
  itemToEdit: HeroBannerItem | null = null;

  // Upload Form State
  uploadMode: 'images' | 'video' = 'images';
  uploadTitle = '';
  uploadDisplayOrder = 1;
  uploadReplaceExisting = false;
  selectedImages: File[] = [];
  imagePreviews: { url: string; name: string; size: string }[] = [];
  selectedVideo: File | null = null;
  videoPreviewUrl: string | null = null;
  uploadValidationError: string | null = null;

  // Edit Form State
  editTitle = '';
  editDisplayOrder = 1;
  editIsActive = true;
  editReplacementFile: File | null = null;
  editReplacementPreviewUrl: string | null = null;
  editValidationError: string | null = null;

  readonly maxImageSize = 5 * 1024 * 1024; // 5MB
  readonly maxVideoSize = 50 * 1024 * 1024; // 50MB

  ngOnInit(): void {
    this.loadBanners();
  }

  loadBanners(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.heroBannerService.getAdminList().subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.success && res.data) {
          this.banners.set(res.data);
        } else {
          this.banners.set([]);
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to load hero section banners.');
      },
    });
  }

  get currentConfigurationType(): 'images' | 'video' | 'empty' {
    const list = this.banners();
    if (list.length === 0) return 'empty';
    if (list.some((b) => b.isVideo)) return 'video';
    return 'images';
  }

  get activeBannerCount(): number {
    return this.banners().filter((b) => b.isActive).length;
  }

  getFullMediaUrl(path: string): string {
    if (!path) return '';
    if (path.startsWith('http://') || path.startsWith('https://')) return path;
    const base = environment.apiBaseUrl.replace(/\/api\/?$/, '');
    return `${base}${path.startsWith('/') ? path : '/' + path}`;
  }

  // Upload Modal Open
  openUploadModal(): void {
    const current = this.currentConfigurationType;
    this.uploadMode = current === 'video' ? 'video' : 'images';
    this.uploadTitle = '';
    this.uploadDisplayOrder = this.banners().length + 1;
    this.uploadReplaceExisting = false;
    this.selectedImages = [];
    this.imagePreviews = [];
    this.selectedVideo = null;
    this.videoPreviewUrl = null;
    this.uploadValidationError = null;
    this.showUploadModal = true;
  }

  closeUploadModal(): void {
    this.showUploadModal = false;
    this.clearImagePreviews();
    this.clearVideoPreview();
  }

  setUploadMode(mode: 'images' | 'video'): void {
    if (this.uploadMode !== mode) {
      this.uploadMode = mode;
      this.uploadValidationError = null;
      this.clearImagePreviews();
      this.clearVideoPreview();
    }
  }

  // Image Selection Handler
  onImagesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    this.uploadValidationError = null;
    const files = Array.from(input.files);
    const validFiles: File[] = [];
    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];

    for (const file of files) {
      const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
      if (!['.jpg', '.jpeg', '.png', '.webp'].includes(ext)) {
        this.uploadValidationError = `File '${file.name}' is not supported. Only JPG, PNG, and WEBP images are allowed.`;
        return;
      }
      if (!allowedTypes.includes(file.type.toLowerCase()) && file.type) {
        this.uploadValidationError = `File '${file.name}' has invalid image MIME type.`;
        return;
      }
      if (file.size > this.maxImageSize) {
        this.uploadValidationError = `File '${file.name}' exceeds the 5MB size limit.`;
        return;
      }
      validFiles.push(file);
    }

    this.selectedImages = [...this.selectedImages, ...validFiles];
    this.generateImagePreviews();
    input.value = '';
  }

  removeSelectedImage(index: number): void {
    this.selectedImages.splice(index, 1);
    this.generateImagePreviews();
  }

  private generateImagePreviews(): void {
    this.clearImagePreviews();
    this.imagePreviews = this.selectedImages.map((file) => ({
      url: URL.createObjectURL(file),
      name: file.name,
      size: `${(file.size / 1024 / 1024).toFixed(2)} MB`,
    }));
  }

  private clearImagePreviews(): void {
    this.imagePreviews.forEach((p) => URL.revokeObjectURL(p.url));
    this.imagePreviews = [];
  }

  // Video Selection Handler
  onVideoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    this.uploadValidationError = null;
    const file = input.files[0];
    const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();

    if (ext !== '.mp4') {
      this.uploadValidationError =
        'Only MP4 videos are allowed. MOV, AVI, WebM, and MKV are strictly prohibited.';
      input.value = '';
      return;
    }

    if (file.type && file.type.toLowerCase() !== 'video/mp4') {
      this.uploadValidationError = 'Invalid video format. Content type must be video/mp4.';
      input.value = '';
      return;
    }

    if (file.size > this.maxVideoSize) {
      this.uploadValidationError = `Video file exceeds the 50MB size limit (${(file.size / 1024 / 1024).toFixed(1)} MB).`;
      input.value = '';
      return;
    }

    this.clearVideoPreview();
    this.selectedVideo = file;
    this.videoPreviewUrl = URL.createObjectURL(file);
    input.value = '';
  }

  private clearVideoPreview(): void {
    if (this.videoPreviewUrl) {
      URL.revokeObjectURL(this.videoPreviewUrl);
      this.videoPreviewUrl = null;
    }
  }

  // Submit Upload
  submitUpload(): void {
    this.uploadValidationError = null;
    const current = this.currentConfigurationType;

    if (this.uploadTitle && this.uploadTitle.trim().length > 200) {
      this.uploadValidationError = 'Title cannot exceed 200 characters.';
      return;
    }

    if (this.uploadDisplayOrder < 0 || !Number.isInteger(Number(this.uploadDisplayOrder))) {
      this.uploadValidationError = 'Display order must be a non-negative integer (0 or greater).';
      return;
    }

    // Check if mode conflicts with existing configuration
    if (this.uploadMode === 'images' && current === 'video' && !this.uploadReplaceExisting) {
      this.uploadValidationError =
        'A video configuration is currently active. You must check "Replace existing configuration" to switch to images.';
      return;
    }

    if (this.uploadMode === 'video' && current === 'images' && !this.uploadReplaceExisting) {
      this.uploadValidationError =
        'Image banners are currently active. You must check "Replace existing configuration" to switch to video.';
      return;
    }

    if (this.uploadMode === 'video' && current === 'video' && !this.uploadReplaceExisting) {
      this.uploadValidationError =
        'A hero video already exists. You must check "Replace existing configuration" to replace it.';
      return;
    }

    if (this.uploadMode === 'images') {
      if (this.selectedImages.length === 0) {
        this.uploadValidationError = 'Please select at least one image file.';
        return;
      }

      this.isSubmitting.set(true);
      this.heroBannerService
        .createImages(
          this.selectedImages,
          this.uploadTitle,
          this.uploadDisplayOrder,
          this.uploadReplaceExisting,
        )
        .subscribe({
          next: () => {
            this.isSubmitting.set(false);
            this.closeUploadModal();
            this.setSuccess('Hero banner image(s) uploaded successfully.');
            this.loadBanners();
          },
          error: (err) => {
            this.isSubmitting.set(false);
            const msg =
              err.error?.errors?.join(' ') || err.error?.message || 'Failed to upload hero images.';
            this.uploadValidationError = msg;
          },
        });
    } else {
      if (!this.selectedVideo) {
        this.uploadValidationError = 'Please select an MP4 video file.';
        return;
      }

      this.isSubmitting.set(true);
      this.heroBannerService
        .createVideo(
          this.selectedVideo,
          this.uploadTitle,
          this.uploadDisplayOrder,
          this.uploadReplaceExisting,
        )
        .subscribe({
          next: () => {
            this.isSubmitting.set(false);
            this.closeUploadModal();
            this.setSuccess('Hero banner video uploaded successfully.');
            this.loadBanners();
          },
          error: (err) => {
            this.isSubmitting.set(false);
            const msg =
              err.error?.errors?.join(' ') || err.error?.message || 'Failed to upload hero video.';
            this.uploadValidationError = msg;
          },
        });
    }
  }

  // Edit Modal
  openEditModal(item: HeroBannerItem): void {
    this.itemToEdit = item;
    this.editTitle = item.title || '';
    this.editDisplayOrder = item.displayOrder;
    this.editIsActive = item.isActive;
    this.editReplacementFile = null;
    this.editReplacementPreviewUrl = null;
    this.editValidationError = null;
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.itemToEdit = null;
    if (this.editReplacementPreviewUrl) {
      URL.revokeObjectURL(this.editReplacementPreviewUrl);
      this.editReplacementPreviewUrl = null;
    }
  }

  onReplacementFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0 || !this.itemToEdit) return;

    this.editValidationError = null;
    const file = input.files[0];
    const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();

    if (this.itemToEdit.isImage) {
      if (!['.jpg', '.jpeg', '.png', '.webp'].includes(ext)) {
        this.editValidationError = 'Only JPG, PNG, and WEBP image files are allowed.';
        input.value = '';
        return;
      }
      if (file.size > this.maxImageSize) {
        this.editValidationError = 'Image replacement file exceeds the 5MB size limit.';
        input.value = '';
        return;
      }
    } else {
      if (ext !== '.mp4') {
        this.editValidationError =
          'Only MP4 videos are allowed. MOV, AVI, WebM, and MKV are prohibited.';
        input.value = '';
        return;
      }
      if (file.size > this.maxVideoSize) {
        this.editValidationError = 'Video replacement file exceeds the 50MB size limit.';
        input.value = '';
        return;
      }
    }

    if (this.editReplacementPreviewUrl) {
      URL.revokeObjectURL(this.editReplacementPreviewUrl);
    }
    this.editReplacementFile = file;
    this.editReplacementPreviewUrl = URL.createObjectURL(file);
    input.value = '';
  }

  submitEdit(): void {
    if (!this.itemToEdit) return;

    if (this.editTitle && this.editTitle.trim().length > 200) {
      this.editValidationError = 'Title cannot exceed 200 characters.';
      return;
    }

    if (this.editDisplayOrder < 0 || !Number.isInteger(Number(this.editDisplayOrder))) {
      this.editValidationError = 'Display order must be a non-negative integer (0 or greater).';
      return;
    }

    this.isSubmitting.set(true);
    this.editValidationError = null;

    const bannerId = this.itemToEdit.encryptedId || this.itemToEdit.id.toString();

    this.heroBannerService
      .update(
        bannerId,
        {
          title: this.editTitle.trim(),
          displayOrder: this.editDisplayOrder,
          isActive: this.editIsActive,
        },
        this.editReplacementFile ?? undefined,
      )
      .subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.closeEditModal();
          this.setSuccess('Hero banner updated successfully.');
          this.loadBanners();
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.editValidationError =
            err.error?.errors?.join(' ') || err.error?.message || 'Failed to update hero banner.';
        },
      });
  }

  // Status Toggle
  toggleActive(item: HeroBannerItem): void {
    const bannerId = item.encryptedId || item.id.toString();
    const newStatus = !item.isActive;

    this.heroBannerService.toggleStatus(bannerId, newStatus).subscribe({
      next: () => {
        item.isActive = newStatus;
        this.setSuccess(`Hero banner ${newStatus ? 'activated' : 'deactivated'} successfully.`);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to update hero banner status.');
      },
    });
  }

  // Delete
  confirmDelete(item: HeroBannerItem): void {
    this.itemToDelete = item;
    this.showDeleteDialog = true;
  }

  onDeleteConfirmed(): void {
    if (!this.itemToDelete) return;

    const bannerId = this.itemToDelete.encryptedId || this.itemToDelete.id.toString();
    this.isSubmitting.set(true);

    this.heroBannerService.delete(bannerId).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.showDeleteDialog = false;
        this.itemToDelete = null;
        this.setSuccess('Hero banner deleted successfully.');
        this.loadBanners();
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.showDeleteDialog = false;
        this.errorMessage.set(err.error?.message || 'Failed to delete hero banner.');
      },
    });
  }

  onDeleteCancelled(): void {
    this.showDeleteDialog = false;
    this.itemToDelete = null;
  }

  private setSuccess(msg: string): void {
    this.successMessage.set(msg);
    setTimeout(() => {
      this.successMessage.set(null);
    }, 4500);
  }
}
