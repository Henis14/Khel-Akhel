import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AdminProductService } from '../../services/admin-product.service';
import { AdminProductCategoryService } from '../../services/admin-product-category.service';
import { ProductCreateRequest, ProductResponse, ProductUpdateRequest } from '../../models/product.model';
import { ProductCategoryResponse } from '../../models/product-category.model';
import { CustomValidators } from '../../../common/validators/custom.validators';
import { REGEX_PATTERNS } from '../../../common/constants/regex.constants';
import { PaginationComponent } from '../../../common/components/pagination/pagination';
import { ConfirmationDialogComponent } from '../../../common/components/confirmation-dialog/confirmation-dialog';
import { BreadcrumbComponent } from '../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../common/models/breadcrumb.model';
import { AppLoaderComponent } from '../../../common/components/app-loader/app-loader';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    PaginationComponent,
    ConfirmationDialogComponent,
    BreadcrumbComponent,
    AppLoaderComponent
  ],
  templateUrl: './products.html',
  styleUrl: './products.scss'
})
export class ProductsComponent implements OnInit {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Products' }
  ];
  private productService = inject(AdminProductService);
  private categoryService = inject(AdminProductCategoryService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  products: ProductResponse[] = [];
  categories: ProductCategoryResponse[] = [];

  totalRecords = 0;
  pageIndex = 1;
  pageSize = 10;
  searchQuery = '';
  selectedCategoryFilter = '';

  loading = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  isModalOpen = false;
  isEditMode = false;
  selectedProduct: ProductResponse | null = null;
  productForm!: FormGroup;
  formSubmitting = false;

  isConfirmOpen = false;
  confirmTitle = '';
  confirmMessage = '';
  pendingAction: (() => void) | null = null;

  ngOnInit(): void {
    this.initForm();
    this.loadCategories();
    this.loadProducts();
  }

  initForm(): void {
    this.productForm = this.fb.group({
      encryptedCategoryId: ['', [CustomValidators.requiredNonEmpty]],
      productName: ['', [CustomValidators.requiredNonEmpty]],
      sku: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(REGEX_PATTERNS.SKU, 'invalidSku')]],
      shortDescription: [''],
      description: [''],
      mrp: [0, [CustomValidators.nonNegativeNumber, CustomValidators.maxDecimalPlaces(2)]],
      sellingPrice: [0, [CustomValidators.nonNegativeNumber, CustomValidators.maxDecimalPlaces(2)]],
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

  loadProducts(): void {
    this.loading = true;
    this.errorMessage = null;

    this.productService
      .getList({
        pageIndex: this.pageIndex,
        pageSize: this.pageSize,
        search: this.searchQuery.trim() || undefined,
        encryptedCategoryId: this.selectedCategoryFilter || undefined
      })
      .subscribe({
        next: (res) => {
          this.loading = false;
          if (res.success && res.data) {
            this.products = res.data.products || [];
            this.totalRecords = res.data.totalRecords || 0;
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.loading = false;
          this.errorMessage = err.error?.message || 'Failed to load products.';
          this.cdr.markForCheck();
        }
      });
  }

  onSearch(term: string): void {
    this.searchQuery = term;
    this.pageIndex = 1;
    this.loadProducts();
  }

  onCategoryFilterChange(catId: string): void {
    this.selectedCategoryFilter = catId;
    this.pageIndex = 1;
    this.loadProducts();
  }

  onPageChange(newPage: number): void {
    this.pageIndex = newPage;
    this.loadProducts();
  }

  openCreateModal(): void {
    this.isEditMode = false;
    this.selectedProduct = null;
    this.productForm.reset({
      encryptedCategoryId: this.categories.length > 0 ? this.categories[0].encryptedCategoryId : '',
      productName: '',
      sku: '',
      shortDescription: '',
      description: '',
      mrp: 0,
      sellingPrice: 0,
      isFeatured: false,
      isNewArrival: false,
      isBestSeller: false,
      isTrending: false,
      isCustomerFavourite: false
    });
    this.isModalOpen = true;
  }

  openEditModal(product: ProductResponse): void {
    this.isEditMode = true;
    this.selectedProduct = product;
    this.productForm.patchValue({
      encryptedCategoryId: product.encryptedCategoryId,
      productName: product.productName,
      sku: product.sku,
      shortDescription: product.shortDescription,
      description: product.description,
      mrp: product.mrp,
      sellingPrice: product.sellingPrice,
      isFeatured: product.isFeatured,
      isNewArrival: product.isNewArrival,
      isBestSeller: product.isBestSeller,
      isTrending: product.isTrending,
      isCustomerFavourite: product.isCustomerFavourite
    });
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  saveProduct(): void {
    if (this.productForm.invalid) {
      this.productForm.markAllAsTouched();
      return;
    }

    const mrp = Number(this.productForm.value.mrp);
    const sellingPrice = Number(this.productForm.value.sellingPrice);

    if (sellingPrice > mrp) {
      this.errorMessage = 'Selling price cannot be greater than MRP.';
      return;
    }

    this.formSubmitting = true;
    this.errorMessage = null;

    if (this.isEditMode && this.selectedProduct) {
      const updatePayload: ProductUpdateRequest = {
        encryptedProductId: this.selectedProduct.encryptedProductId,
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

      this.productService.update(updatePayload).subscribe({
        next: (res) => {
          this.formSubmitting = false;
          if (res.success) {
            this.successMessage = 'Product updated successfully.';
            this.closeModal();
            this.loadProducts();
          } else {
            this.errorMessage = res.message;
          }
        },
        error: (err) => {
          this.formSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to update product.';
        }
      });
    } else {
      const createPayload: ProductCreateRequest = {
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

      this.productService.create(createPayload).subscribe({
        next: (res) => {
          this.formSubmitting = false;
          if (res.success) {
            this.successMessage = 'Product created successfully.';
            this.closeModal();
            this.loadProducts();
          } else {
            this.errorMessage = res.message;
          }
        },
        error: (err) => {
          this.formSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to create product.';
        }
      });
    }
  }

  isActionSubmitting = false;

  confirmToggleActive(product: ProductResponse): void {
    const actionName = product.isActive ? 'deactivate' : 'activate';
    this.confirmTitle = `${actionName.toUpperCase()} Product`;
    this.confirmMessage = `Are you sure you want to ${actionName} product "${product.productName}"?`;
    this.pendingAction = () => {
      this.isActionSubmitting = true;
      this.productService.toggleActive(product.encryptedProductId).subscribe({
        next: () => {
          this.isActionSubmitting = false;
          this.isConfirmOpen = false;
          this.successMessage = `Product ${actionName}d successfully.`;
          this.loadProducts();
        },
        error: (err) => {
          this.isActionSubmitting = false;
          this.errorMessage = err.error?.message || `Failed to ${actionName} product.`;
        }
      });
    };
    this.isConfirmOpen = true;
  }

  confirmDelete(product: ProductResponse): void {
    this.confirmTitle = 'Delete Product';
    this.confirmMessage = `Are you sure you want to delete product "${product.productName}"?`;
    this.pendingAction = () => {
      this.isActionSubmitting = true;
      this.productService.delete(product.encryptedProductId).subscribe({
        next: () => {
          this.isActionSubmitting = false;
          this.isConfirmOpen = false;
          this.successMessage = 'Product deleted successfully.';
          this.loadProducts();
        },
        error: (err) => {
          this.isActionSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to delete product.';
        }
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
