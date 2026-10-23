import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminProductService } from '../../../services/admin-product.service';
import { AdminProductCategoryService } from '../../../services/admin-product-category.service';
import { AdminProductImageService } from '../../../services/admin-product-image.service';
import { ProductCreateRequest } from '../../../models/product.model';
import {
  ProductCategoryResponse,
  ProductSubCategoryResponse,
} from '../../../models/product-category.model';
import { CustomValidators } from '../../../../common/validators/custom.validators';
import { REGEX_PATTERNS } from '../../../../common/constants/regex.constants';
import { BreadcrumbComponent } from '../../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../../common/models/breadcrumb.model';
import { validateImageFile } from '../../../../common/utils/file-validation.util';

interface ImagePreview {
  file: File;
  url: string;
}

@Component({
  selector: 'app-add-product',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, BreadcrumbComponent],
  templateUrl: './add-product.html',
  styleUrl: './add-product.scss',
})
export class AddProductComponent implements OnInit, OnDestroy {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Products', url: '/admin/products' },
    { label: 'Add Product' },
  ];
  private fb = inject(FormBuilder);
  private productService = inject(AdminProductService);
  private categoryService = inject(AdminProductCategoryService);
  private productImageService = inject(AdminProductImageService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  productForm!: FormGroup;
  categories: ProductCategoryResponse[] = [];
  subCategories: ProductSubCategoryResponse[] = [];

  loadingCategories = false;
  loadingSubCategories = false;
  submitting = false;
  isUploadingImages = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  selectedFiles: File[] = [];
  imagePreviews: ImagePreview[] = [];
  readonly minImages = 3;
  readonly maxImages = 7;
  readonly maxFileSizeMB = 5;
  readonly allowedTypes = ['image/jpeg', 'image/png'];
  readonly allowedExtensions = ['.jpg', '.jpeg', '.png'];

  ngOnInit(): void {
    this.initForm();
    this.loadCategories();
  }

  ngOnDestroy(): void {
    this.clearImagePreviews();
  }

  initForm(): void {
    this.productForm = this.fb.group(
      {
        encryptedCategoryId: ['', [CustomValidators.requiredNonEmpty]],
        encryptedSubCategoryId: [''],
        productName: ['', [CustomValidators.requiredNonEmpty]],
        sku: [
          '',
          [
            CustomValidators.requiredNonEmpty,
            CustomValidators.pattern(REGEX_PATTERNS.SKU, 'invalidSku'),
          ],
        ],
        shortDescription: [''],
        description: [''],
        mrp: [
          0,
          [
            CustomValidators.requiredNonEmpty,
            CustomValidators.nonNegativeNumber,
            CustomValidators.maxDecimalPlaces(2),
          ],
        ],
        sellingPrice: [
          0,
          [
            CustomValidators.requiredNonEmpty,
            CustomValidators.nonNegativeNumber,
            CustomValidators.maxDecimalPlaces(2),
          ],
        ],
        isFeatured: [false],
        isNewArrival: [false],
        isBestSeller: [false],
        isTrending: [false],
        isCustomerFavourite: [false],
      },
      {
        validators: [CustomValidators.sellingPriceLessOrEqualMrp('mrp', 'sellingPrice')],
      },
    );
  }

  loadCategories(): void {
    this.loadingCategories = true;
    this.categoryService.getList().subscribe({
      next: (res) => {
        this.loadingCategories = false;
        if (res.success && res.data) {
          this.categories = res.data.filter((c) => c.isActive);
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingCategories = false;
        this.cdr.markForCheck();
      },
    });
  }

  onCategoryChange(categoryId: string): void {
    this.productForm.patchValue({
      encryptedSubCategoryId: '',
    });
    this.subCategories = [];

    if (!categoryId) return;

    this.loadingSubCategories = true;
    this.categoryService.getSubCategories(categoryId).subscribe({
      next: (res) => {
        this.loadingSubCategories = false;
        if (res.success && res.data) {
          this.subCategories = res.data.filter((sc) => sc.isActive);
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingSubCategories = false;
        this.cdr.markForCheck();
      },
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
      const validation = validateImageFile(file, this.maxFileSizeMB * 1024 * 1024);
      if (!validation.isValid) {
        this.errorMessage = validation.errorMessage || 'Invalid image file.';
        continue;
      }

      this.selectedFiles.push(file);
      this.imagePreviews.push({
        file: file,
        url: URL.createObjectURL(file),
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
    this.imagePreviews.forEach((p) => URL.revokeObjectURL(p.url));
    this.imagePreviews = [];
    this.selectedFiles = [];
  }

  onSubmit(): void {
    if (this.productForm.invalid) {
      this.productForm.markAllAsTouched();
      if (!this.productForm.value.encryptedCategoryId) {
        this.errorMessage = 'Please select a product category.';
      } else if (
        this.productForm.errors?.['sellingPriceGreaterThanMrp'] ||
        this.productForm.errors?.['sellingPriceExceedsMrp']
      ) {
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
      encryptedSubCategoryId: this.productForm.value.encryptedSubCategoryId || null,
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
      isCustomerFavourite: !!this.productForm.value.isCustomerFavourite,
    };

    this.productService.create(payload).subscribe({
      next: (res) => {
        if (res.success && res.data?.encryptedProductId) {
          const encryptedProductId = res.data.encryptedProductId;

          if (this.selectedFiles.length > 0) {
            this.isUploadingImages = true;
            this.productImageService
              .uploadImages(encryptedProductId, this.selectedFiles)
              .subscribe({
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
                  const errDetail =
                    imgErr.error?.errors && imgErr.error.errors.length > 0
                      ? imgErr.error.errors.join('; ')
                      : imgErr.error?.message || 'Upload error';
                  this.errorMessage = `Product created, but image upload failed: ${errDetail}. You can upload images in Product Detail.`;
                  setTimeout(() => {
                    this.router.navigate(['/admin/products', encryptedProductId]);
                  }, 2500);
                },
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
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/products']);
  }
}
