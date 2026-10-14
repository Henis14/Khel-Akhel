import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AdminAboutService } from '../../../services/admin-about.service';
import {
  AboutCreatePayload,
  AboutDetail,
  AboutImage,
  AboutImageOrderDto,
  AboutUpdatePayload,
} from '../../../models/about.model';
import {
  EditorImageItem,
  ImageGalleryEditorComponent,
} from '../image-gallery-editor/image-gallery-editor';
import { GalleryLayoutSelectorComponent } from '../gallery-layout-selector/gallery-layout-selector';
import {
  GalleryImageItem,
  GalleryRendererComponent,
} from '../../../../common/components/gallery-renderer/gallery-renderer';
import { BreadcrumbComponent } from '../../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../../common/models/breadcrumb.model';
import { AppLoaderComponent } from '../../../../common/components/app-loader/app-loader';

@Component({
  selector: 'app-about-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    BreadcrumbComponent,
    AppLoaderComponent,
    GalleryLayoutSelectorComponent,
    ImageGalleryEditorComponent,
    GalleryRendererComponent,
  ],
  templateUrl: './about-form.html',
  styleUrl: './about-form.scss',
})
export class AboutFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private aboutService = inject(AdminAboutService);
  private cdr = inject(ChangeDetectorRef);

  isEditMode = false;
  encryptedAboutId: string | null = null;
  existingRecord: AboutDetail | null = null;
  initialExistingImages: AboutImage[] = [];

  form!: FormGroup;
  selectedLayout: string = 'grid-3';

  // Visual Image Assets state
  currentEditorImages: EditorImageItem[] = [];
  newFilesToUpload: File[] = [];
  removedImageIds: string[] = [];
  imageOrders: AboutImageOrderDto[] = [];

  // UI state
  loading = false;
  submitting = false;
  activeTab: 'edit' | 'preview' = 'edit';
  errorMessage: string | null = null;
  successMessage: string | null = null;
  validationErrors: string[] = [];

  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'About Sections', url: '/admin/about' },
    { label: 'New Section' },
  ];

  ngOnInit(): void {
    this.initForm();
    this.route.paramMap.subscribe((params) => {
      const id = params.get('encryptedAboutId');
      if (id) {
        this.isEditMode = true;
        this.encryptedAboutId = id;
        this.breadcrumbs[2] = { label: 'Edit Section' };
        this.loadRecord(id);
      }
    });
  }

  private initForm(): void {
    this.form = this.fb.group({
      heading: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
      subheading: ['', [Validators.maxLength(250)]],
      contentType: [''],
      projectOrEventTitle: ['', [Validators.maxLength(200)]],
      displayYear: ['', [Validators.maxLength(50)]],
      shortDescription: ['', [Validators.maxLength(1000)]],
      detailedDescription: [''],
      credits: ['', [Validators.maxLength(1000)]],
      location: ['', [Validators.maxLength(200)]],
      externalUrl: [
        '',
        [
          Validators.maxLength(500),
          Validators.pattern(/^(https?:\/\/)?([\da-z.-]+)\.([a-z.]{2,6})([/\w .-]*)*\/?$/i),
        ],
      ],
      displayOrder: [0, [Validators.min(0)]],
      isPublished: [false],
      isActive: [true],
    });
  }

  private loadRecord(id: string): void {
    this.loading = true;
    this.errorMessage = null;

    this.aboutService.getById(id).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success && res.data) {
          this.existingRecord = res.data;
          this.selectedLayout = res.data.layoutType || 'grid-3';
          this.initialExistingImages = res.data.images || [];

          this.form.patchValue({
            heading: res.data.heading || '',
            subheading: res.data.subheading || '',
            contentType: res.data.contentType || '',
            projectOrEventTitle: res.data.projectOrEventTitle || '',
            displayYear: res.data.displayYear || '',
            shortDescription: res.data.shortDescription || '',
            detailedDescription: res.data.detailedDescription || '',
            credits: res.data.credits || '',
            location: res.data.location || '',
            externalUrl: res.data.externalUrl || '',
            displayOrder: res.data.displayOrder ?? 0,
            isPublished: res.data.isPublished ?? false,
            isActive: res.data.isActive ?? true,
          });
        } else {
          this.errorMessage = res.message || 'Failed to load About section data.';
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage =
          err?.error?.message || 'Error occurred while retrieving record details.';
        this.cdr.markForCheck();
      },
    });
  }

  onLayoutChange(layout: string): void {
    this.selectedLayout = layout;
  }

  onImagesChanged(event: {
    allImages: EditorImageItem[];
    newFiles: File[];
    removeImageIds: string[];
    imageOrders: AboutImageOrderDto[];
  }): void {
    this.currentEditorImages = event.allImages;
    this.newFilesToUpload = event.newFiles;
    this.removedImageIds = event.removeImageIds;
    this.imageOrders = event.imageOrders;
  }

  get livePreviewImages(): GalleryImageItem[] {
    return this.currentEditorImages.map((img) => ({
      imageUrl: img.previewUrl,
      originalFileName: img.fileName,
      displayOrder: img.displayOrder,
    }));
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage = 'Please review and resolve the validation highlights below.';
      return;
    }

    this.submitting = true;
    this.errorMessage = null;
    this.validationErrors = [];

    const fv = this.form.value;

    if (this.isEditMode && this.encryptedAboutId) {
      const updatePayload: AboutUpdatePayload = {
        encryptedAboutId: this.encryptedAboutId,
        heading: fv.heading,
        subheading: fv.subheading || null,
        contentType: fv.contentType || null,
        projectOrEventTitle: fv.projectOrEventTitle || null,
        displayYear: fv.displayYear || null,
        shortDescription: fv.shortDescription || null,
        detailedDescription: fv.detailedDescription || null,
        credits: fv.credits || null,
        location: fv.location || null,
        externalUrl: fv.externalUrl || null,
        displayOrder: Number(fv.displayOrder ?? 0),
        layoutType: this.selectedLayout,
        isPublished: !!fv.isPublished,
        isActive: !!fv.isActive,
        newImages: this.newFilesToUpload,
        removeImageIds: this.removedImageIds,
        imageOrders: this.imageOrders,
      };

      this.aboutService.update(updatePayload).subscribe({
        next: (res) => {
          this.submitting = false;
          if (res.success) {
            this.successMessage = 'About content section updated successfully.';
            setTimeout(() => {
              this.router.navigate(['/admin/about']);
            }, 1200);
          } else {
            this.errorMessage = res.message || 'Failed to update About content.';
            if (res.errors) this.validationErrors = res.errors;
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.submitting = false;
          this.errorMessage =
            err?.error?.message || 'Server error occurred while updating About section.';
          if (err?.error?.errors) this.validationErrors = err.error.errors;
          this.cdr.markForCheck();
        },
      });
    } else {
      const createPayload: AboutCreatePayload = {
        heading: fv.heading,
        subheading: fv.subheading || null,
        contentType: fv.contentType || null,
        projectOrEventTitle: fv.projectOrEventTitle || null,
        displayYear: fv.displayYear || null,
        shortDescription: fv.shortDescription || null,
        detailedDescription: fv.detailedDescription || null,
        credits: fv.credits || null,
        location: fv.location || null,
        externalUrl: fv.externalUrl || null,
        displayOrder: Number(fv.displayOrder ?? 0),
        layoutType: this.selectedLayout,
        isPublished: !!fv.isPublished,
        isActive: !!fv.isActive,
        images: this.newFilesToUpload,
      };

      this.aboutService.create(createPayload).subscribe({
        next: (res) => {
          this.submitting = false;
          if (res.success) {
            this.successMessage = 'New About section created successfully.';
            setTimeout(() => {
              this.router.navigate(['/admin/about']);
            }, 1200);
          } else {
            this.errorMessage = res.message || 'Failed to create About section.';
            if (res.errors) this.validationErrors = res.errors;
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.submitting = false;
          this.errorMessage =
            err?.error?.message || 'Server error occurred while creating About section.';
          if (err?.error?.errors) this.validationErrors = err.error.errors;
          this.cdr.markForCheck();
        },
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/admin/about']);
  }

  hasVisibleCredits(credits: string | null | undefined): boolean {
    if (!credits) return false;
    const trimmed = credits.trim();
    if (!trimmed) return false;
    const lower = trimmed.toLowerCase();
    return lower !== 'khel-akhel studio team' && lower !== 'khel akhel studio team';
  }
}
