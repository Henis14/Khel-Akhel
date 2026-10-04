import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { AdminProductService } from '../../../services/admin-product.service';
import { AdminProductCategoryService } from '../../../services/admin-product-category.service';
import { AdminProductImageService } from '../../../services/admin-product-image.service';
import { ProductResponse, ProductUpdateRequest } from '../../../models/product.model';
import { ProductCategoryResponse } from '../../../models/product-category.model';
import { ProductImage } from '../../../models/product-image.model';
import { CustomValidators } from '../../../../common/validators/custom.validators';
import { REGEX_PATTERNS } from '../../../../common/constants/regex.constants';
import { BreadcrumbComponent } from '../../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../../common/models/breadcrumb.model';
import { AppLoaderComponent } from '../../../../common/components/app-loader/app-loader';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, BreadcrumbComponent, AppLoaderComponent],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss'
})
export class ProductDetailComponent implements OnInit {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Products', url: '/admin/products' },
    { label: 'Product Details' }
  ];
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productService = inject(AdminProductService);
  private categoryService = inject(AdminProductCategoryService);
  private productImageService = inject(AdminProductImageService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);

  encryptedProductId: string | null = null;
  product: ProductResponse | null = null;
  categories: ProductCategoryResponse[] = [];
  productImages: ProductImage[] = [];

  loading = true;
  imagesLoading = false;
  uploadingNewImages = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  // Edit Modal State
  isEditModalOpen = false;
  editForm!: FormGroup;
  editSubmitting = false;

  readonly minImages = 3;
  readonly maxImages = 7;
  readonly maxFileSizeMB = 5;

  ngOnInit(): void {
    this.initEditForm();
    this.loadCategories();
    this.encryptedProductId = this.route.snapshot.paramMap.get('encryptedId');

    if (this.encryptedProductId) {
      this.loadProductDetail();
      this.loadProductImages();
    } else {
      this.errorMessage = 'Invalid product ID.';
      this.loading = false;
    }
  }

  parseDate(dateStr: string | null | undefined): Date | null {
    if (!dateStr) return null;
    const d = new Date(dateStr);
    if (!isNaN(d.getTime())) return d;

    // Parse DD/MM/YYYY HH:mm:ss or DD-MM-YYYY HH:mm:ss
    const parts = dateStr.split(/[\sT\/-]+/);
    if (parts.length >= 3) {
      const day = parseInt(parts[0], 10);
      const month = parseInt(parts[1], 10) - 1;
      const year = parseInt(parts[2], 10);
      let hours = 0, minutes = 0, seconds = 0;
      if (parts.length >= 4) hours = parseInt(parts[3], 10) || 0;
      if (parts.length >= 5) minutes = parseInt(parts[4], 10) || 0;
      if (parts.length >= 6) seconds = parseInt(parts[5], 10) || 0;
      const parsed = new Date(year, month, day, hours, minutes, seconds);
      if (!isNaN(parsed.getTime())) return parsed;
    }
    return null;
  }

  initEditForm(): void {
    this.editForm = this.fb.group({
      encryptedCategoryId: ['', [CustomValidators.requiredNonEmpty]],
      productName: ['', [CustomValidators.requiredNonEmpty]],
      sku: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.SKU, 'invalidSku')]],
      shortDescription: [''],
      description: [''],
      mrp: [0, [CustomValidators.requiredNonEmpty, CustomValidators.nonNegativeNumber, CustomValidators.maxDecimalPlaces(2)]],
      sellingPrice: [0, [CustomValidators.requiredNonEmpty, CustomValidators.nonNegativeNumber, CustomValidators.maxDecimalPlaces(2)]],
      isFeatured: [false],
      isNewArrival: [false],
      isBestSeller: [false],
      isTrending: [false],
      isCustomerFavourite: [false]
    }, {
      validators: [CustomValidators.sellingPriceLessOrEqualMrp('mrp', 'sellingPrice')]
    });
  }

  loadCategories(): void {
    this.categoryService.getList().subscribe({
      next: (res) => {
        if (res.success) {
          this.categories = res.data || [];
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.cdr.markForCheck();
      }
    });
  }

  loadProductDetail(): void {
    if (!this.encryptedProductId) return;

    this.loading = true;
    this.errorMessage = null;

    this.productService.getById(this.encryptedProductId).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success && res.data) {
          this.product = res.data;
        } else {
          this.errorMessage = res.message || 'Product record not found.';
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Failed to load product details.';
        this.cdr.markForCheck();
      }
    });
  }

  loadProductImages(): void {
    if (!this.encryptedProductId) return;

    this.imagesLoading = true;
    this.productImageService.getImages(this.encryptedProductId).subscribe({
      next: (res) => {
        this.imagesLoading = false;
        if (res.success && res.data) {
          this.productImages = res.data;
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.imagesLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  getImageUrl(imagePath: string): string {
    if (!imagePath) return '';
    if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
      return imagePath;
    }
    const baseUrl = environment.apiBaseUrl.replace(/\/api\/?$/i, '');
    return `${baseUrl}${imagePath.startsWith('/') ? '' : '/'}${imagePath}`;
  }

  onUploadNewImages(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0 || !this.encryptedProductId) return;

    this.errorMessage = null;
    this.successMessage = null;
    const filesArray = Array.from(input.files);

    if (this.productImages.length + filesArray.length > this.maxImages) {
      this.errorMessage = `Cannot exceed maximum limit of ${this.maxImages} images per product. Currently active: ${this.productImages.length}.`;
      input.value = '';
      return;
    }

    this.uploadingNewImages = true;
    this.productImageService.uploadImages(this.encryptedProductId, filesArray).subscribe({
      next: (res) => {
        this.uploadingNewImages = false;
        input.value = '';
        if (res.success) {
          this.successMessage = 'Product image(s) uploaded successfully.';
          this.loadProductImages();
        } else {
          this.errorMessage = res.message || 'Failed to upload images.';
        }
      },
      error: (err) => {
        this.uploadingNewImages = false;
        input.value = '';
        this.errorMessage = err.error?.message || 'Failed to upload images.';
      }
    });
  }

  onSetDefaultImage(encryptedImageId: string): void {
    this.errorMessage = null;
    this.successMessage = null;

    this.productImageService.setDefault(encryptedImageId).subscribe({
      next: (res) => {
        if (res.success) {
          this.successMessage = 'Default product image updated.';
          this.loadProductImages();
        } else {
          this.errorMessage = res.message || 'Failed to update default image.';
        }
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Failed to update default image.';
      }
    });
  }

  onDeleteImage(encryptedImageId: string): void {
    this.errorMessage = null;
    this.successMessage = null;

    if (this.productImages.length <= this.minImages) {
      this.errorMessage = `Minimum ${this.minImages} images are required. Add a replacement image before deleting.`;
      return;
    }

    if (!confirm('Are you sure you want to delete this product image?')) return;

    this.errorMessage = null;
    this.successMessage = null;

    this.productImageService.deleteImage(encryptedImageId).subscribe({
      next: (res) => {
        if (res.success) {
          this.successMessage = 'Product image deleted successfully.';
          this.loadProductImages();
        } else {
          this.errorMessage = res.message || 'Failed to delete image.';
        }
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Failed to delete image.';
      }
    });
  }

  openEditModal(): void {
    if (!this.product) return;

    this.editForm.patchValue({
      encryptedCategoryId: this.product.encryptedCategoryId,
      productName: this.product.productName,
      sku: this.product.sku,
      shortDescription: this.product.shortDescription,
      description: this.product.description,
      mrp: this.product.mrp,
      sellingPrice: this.product.sellingPrice,
      isFeatured: this.product.isFeatured,
      isNewArrival: this.product.isNewArrival,
      isBestSeller: this.product.isBestSeller,
      isTrending: this.product.isTrending,
      isCustomerFavourite: this.product.isCustomerFavourite
    });

    this.isEditModalOpen = true;
  }

  closeEditModal(): void {
    this.isEditModalOpen = false;
  }

  saveProduct(): void {
    if (this.editForm.invalid || !this.encryptedProductId) {
      this.editForm.markAllAsTouched();
      return;
    }

    const mrp = Number(this.editForm.value.mrp);
    const sellingPrice = Number(this.editForm.value.sellingPrice);

    if (sellingPrice > mrp) {
      this.errorMessage = 'Selling price cannot be greater than MRP.';
      return;
    }

    this.editSubmitting = true;
    this.errorMessage = null;

    const payload: ProductUpdateRequest = {
      encryptedProductId: this.encryptedProductId,
      encryptedCategoryId: this.editForm.value.encryptedCategoryId,
      productName: this.editForm.value.productName.trim(),
      sku: this.editForm.value.sku.trim(),
      shortDescription: this.editForm.value.shortDescription?.trim() || '',
      description: this.editForm.value.description?.trim() || '',
      mrp: mrp,
      sellingPrice: sellingPrice,
      isFeatured: !!this.editForm.value.isFeatured,
      isNewArrival: !!this.editForm.value.isNewArrival,
      isBestSeller: !!this.editForm.value.isBestSeller,
      isTrending: !!this.editForm.value.isTrending,
      isCustomerFavourite: !!this.editForm.value.isCustomerFavourite
    };

    this.productService.update(payload).subscribe({
      next: (res) => {
        this.editSubmitting = false;
        if (res.success) {
          this.successMessage = 'Product details updated successfully.';
          this.closeEditModal();
          this.loadProductDetail();
        } else {
          this.errorMessage = res.message;
        }
      },
      error: (err) => {
        this.editSubmitting = false;
        this.errorMessage = err.error?.message || 'Failed to update product details.';
      }
    });
  }

  get discountPercent(): number {
    if (!this.product || !this.product.mrp || this.product.mrp <= 0) return 0;
    const diff = this.product.mrp - this.product.sellingPrice;
    return Math.round((diff / this.product.mrp) * 100);
  }

  goBack(): void {
    this.router.navigate(['/admin/products']);
  }
}
