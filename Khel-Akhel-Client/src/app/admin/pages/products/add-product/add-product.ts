import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminProductService } from '../../../services/admin-product.service';
import { AdminProductCategoryService } from '../../../services/admin-product-category.service';
import { AdminProductImageService } from '../../../services/admin-product-image.service';
import { ProductCreateRequest } from '../../../models/product.model';
import { ProductCategoryResponse } from '../../../models/product-category.model';
import { CustomValidators } from '../../../../common/validators/custom.validators';
import { REGEX_PATTERNS } from '../../../../common/constants/regex.constants';
import { BreadcrumbComponent } from '../../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../../common/models/breadcrumb.model';

interface ImagePreview {
  file: File;
  url: string;
}

@Component({
  selector: 'app-add-product',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, BreadcrumbComponent],
  templateUrl: './add-product.html',
  styleUrl: './add-product.scss'
})
export class AddProductComponent implements OnInit, OnDestroy {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Products', url: '/admin/products' },
    { label: 'Add Product' }
  ];
  private fb = inject(FormBuilder);
  private productService = inject(AdminProductService);
  private categoryService = inject(AdminProductCategoryService);
  private productImageService = inject(AdminProductImageService);
  private router = inject(Router);

  productForm!: FormGroup;
  categories: ProductCategoryResponse[] = [];

  loadingCategories = false;
  submitting = false;
  isUploadingImages = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  selectedFiles: File[] = [];
  imagePreviews: ImagePreview[] = [];
  readonly minImages = 3;
  readonly maxImages = 7;
  readonly maxFileSizeMB = 5;
  readonly allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];
  readonly allowedExtensions = ['.jpg', '.jpeg', '.png', '.webp'];

  ngOnInit(): void {
    this.initForm();
    this.loadCategories();
  }

  ngOnDestroy(): void {
    this.clearImagePreviews();
  }

  initForm(): void {
    this.productForm = this.fb.group({
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
    this.loadingCategories = true;
    this.categoryService.getList().subscribe({
      next: (res) => {
        this.loadingCategories = false;
        if (res.success && res.data) {
          this.categories = res.data.filter(c => c.isActive);
          if (this.categories.length > 0) {
            this.productForm.patchValue({ encryptedCategoryId: this.categories[0].encryptedCategoryId });
          }
        }
      },
      error: () => {
        this.loadingCategories = false;
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    this.errorMessage = null;
    const filesArray = Array.from(input.files);

    if (this.selectedFiles.length + filesArray.length > this.maxImages) {
      this.errorMessage = `Maximum ${this.maxImages} images are allowed.`;
      input.value = '';
      return;
    }

    for (const file of filesArray) {
      if (file.size === 0) {
        this.errorMessage = `File '${file.name}' is empty.`;
        continue;
      }

      if (file.size > this.maxFileSizeMB * 1024 * 1024) {
        this.errorMessage = `File '${file.name}' exceeds maximum allowed size of ${this.maxFileSizeMB}MB.`;
        continue;
      }

      const ext = '.' + (file.name.split('.').pop() || '').toLowerCase();
      if (!this.allowedTypes.includes(file.type.toLowerCase()) && !this.allowedExtensions.includes(ext)) {
        this.errorMessage = `File '${file.name}' is not a valid image. Only JPG, PNG, and WEBP formats are allowed.`;
        continue;
      }

      this.selectedFiles.push(file);
      this.imagePreviews.push({
        file: file,
        url: URL.createObjectURL(file)
      });
    }

    input.value = '';
  }

  removeSelectedImage(index: number): void {
    if (index >= 0 && index < this.imagePreviews.length) {
      URL.revokeObjectURL(this.imagePreviews[index].url);
      this.imagePreviews.splice(index, 1);
      this.selectedFiles.splice(index, 1);
    }
  }

  clearImagePreviews(): void {
    this.imagePreviews.forEach(p => URL.revokeObjectURL(p.url));
    this.imagePreviews = [];
    this.selectedFiles = [];
  }

  onSubmit(): void {
    if (this.productForm.invalid) {
      this.productForm.markAllAsTouched();
      if (!this.productForm.value.encryptedCategoryId) {
        this.errorMessage = 'Please select a product category.';
      } else if (this.productForm.errors?.['sellingPriceGreaterThanMrp'] || this.productForm.errors?.['sellingPriceExceedsMrp']) {
        this.errorMessage = 'Selling price cannot be greater than MRP.';
      } else {
        this.errorMessage = 'Please complete all required fields correctly.';
      }
      return;
    }

    const mrp = Number(this.productForm.value.mrp);
    const sellingPrice = Number(this.productForm.value.sellingPrice);

    if (sellingPrice > mrp) {
      this.errorMessage = 'Selling price cannot be greater than MRP.';
      return;
    }

    if (this.selectedFiles.length < this.minImages) {
      this.errorMessage = `Minimum ${this.minImages} images are required. Currently selected: ${this.selectedFiles.length}.`;
      return;
    }

    if (this.selectedFiles.length > this.maxImages) {
      this.errorMessage = `Maximum ${this.maxImages} images are allowed. Currently selected: ${this.selectedFiles.length}.`;
      return;
    }

    this.submitting = true;
    this.errorMessage = null;
    this.successMessage = null;

    const payload: ProductCreateRequest = {
      encryptedCategoryId: this.productForm.value.encryptedCategoryId,
      productName: this.productForm.value.productName.trim(),
      sku: this.productForm.value.sku.trim(),
      shortDescription: this.productForm.value.shortDescription?.trim() || '',
      description: this.productForm.value.description?.trim() || '',
      mrp: mrp,
      sellingPrice: sellingPrice,
      isFeatured: !!this.productForm.value.isFeatured,
      isNewArrival: !!this.productForm.value.isNewArrival,
      isBestSeller: !!this.productForm.value.isBestSeller,
      isTrending: !!this.productForm.value.isTrending,
      isCustomerFavourite: !!this.productForm.value.isCustomerFavourite
    };

    this.productService.create(payload).subscribe({
      next: (res) => {
        if (res.success && res.data?.encryptedProductId) {
          const encryptedProductId = res.data.encryptedProductId;

          if (this.selectedFiles.length > 0) {
            this.isUploadingImages = true;
            this.productImageService.uploadImages(encryptedProductId, this.selectedFiles).subscribe({
              next: (imgRes) => {
                this.submitting = false;
                this.isUploadingImages = false;
                if (imgRes.success) {
                  this.successMessage = 'Product and images created successfully! Redirecting...';
                  this.clearImagePreviews();
                  setTimeout(() => {
                    this.router.navigate(['/admin/products']);
                  }, 1200);
                } else {
                  this.errorMessage = `Product created, but image upload failed: ${imgRes.message}.`;
                  setTimeout(() => {
                    this.router.navigate(['/admin/products']);
                  }, 2500);
                }
              },
              error: (imgErr) => {
                this.submitting = false;
                this.isUploadingImages = false;
                this.errorMessage = `Product created, but image upload failed: ${imgErr.error?.message || 'Upload error'}. You can upload images in Product Detail.`;
                setTimeout(() => {
                  this.router.navigate(['/admin/products', encryptedProductId]);
                }, 2500);
              }
            });
          } else {
            this.submitting = false;
            this.successMessage = 'Product created successfully! Redirecting...';
            setTimeout(() => {
              this.router.navigate(['/admin/products']);
            }, 1200);
          }
        } else {
          this.submitting = false;
          this.errorMessage = res.message || 'Failed to create product.';
        }
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = err.error?.message || 'Server error occurred while creating product.';
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/products']);
  }
}
