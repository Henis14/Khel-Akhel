import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { AdminProductCategoryService } from '../../services/admin-product-category.service';
import { ProductCategoryCreateRequest, ProductCategoryResponse, ProductCategoryUpdateRequest } from '../../models/product-category.model';
import { CustomValidators } from '../../../common/validators/custom.validators';
import { ConfirmationDialogComponent } from '../../../common/components/confirmation-dialog/confirmation-dialog';
import { BreadcrumbComponent } from '../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../common/models/breadcrumb.model';
import { AppLoaderComponent } from '../../../common/components/app-loader/app-loader';

@Component({
  selector: 'app-product-categories',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ConfirmationDialogComponent, BreadcrumbComponent, AppLoaderComponent],
  templateUrl: './product-categories.html',
  styleUrl: './product-categories.scss'
})
export class ProductCategoriesComponent implements OnInit {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Product Categories' }
  ];
  private categoryService = inject(AdminProductCategoryService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);

  categories: ProductCategoryResponse[] = [];
  loading = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  isModalOpen = false;
  isEditMode = false;
  selectedCategory: ProductCategoryResponse | null = null;
  categoryForm!: FormGroup;
  formSubmitting = false;

  isConfirmOpen = false;
  confirmTitle = '';
  confirmMessage = '';
  isActionSubmitting = false;
  pendingAction: (() => void) | null = null;

  ngOnInit(): void {
    this.initForm();
    this.loadCategories();
  }

  initForm(): void {
    this.categoryForm = this.fb.group({
      categoryName: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(/^.{2,100}$/, 'invalidLength')]],
      description: ['', [CustomValidators.pattern(/^[\s\S]{0,500}$/, 'invalidLength')]],
      displayOrder: [0, [CustomValidators.nonNegativeInteger]]
    });
  }

  loadCategories(): void {
    this.loading = true;
    this.errorMessage = null;

    this.categoryService.getList().subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success) {
          this.categories = res.data || [];
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Failed to load categories.';
        this.cdr.markForCheck();
      }
    });
  }

  openCreateModal(): void {
    this.isEditMode = false;
    this.selectedCategory = null;
    this.categoryForm.reset({
      categoryName: '',
      description: '',
      displayOrder: 0
    });
    this.isModalOpen = true;
  }

  openEditModal(category: ProductCategoryResponse): void {
    this.isEditMode = true;
    this.selectedCategory = category;
    this.categoryForm.patchValue({
      categoryName: category.categoryName,
      description: category.description,
      displayOrder: category.displayOrder
    });
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  saveCategory(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    this.formSubmitting = true;
    this.errorMessage = null;

    if (this.isEditMode && this.selectedCategory) {
      const updatePayload: ProductCategoryUpdateRequest = {
        encryptedCategoryId: this.selectedCategory.encryptedCategoryId,
        categoryName: this.categoryForm.value.categoryName.trim(),
        description: this.categoryForm.value.description?.trim() || '',
        displayOrder: Number(this.categoryForm.value.displayOrder) || 0
      };

      this.categoryService.update(updatePayload).subscribe({
        next: (res) => {
          this.formSubmitting = false;
          if (res.success) {
            this.successMessage = 'Category updated successfully.';
            this.closeModal();
            this.loadCategories();
          } else {
            this.errorMessage = res.message;
          }
        },
        error: (err) => {
          this.formSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to update category.';
        }
      });
    } else {
      const createPayload: ProductCategoryCreateRequest = {
        categoryName: this.categoryForm.value.categoryName.trim(),
        description: this.categoryForm.value.description?.trim() || '',
        displayOrder: Number(this.categoryForm.value.displayOrder) || 0
      };

      this.categoryService.create(createPayload).subscribe({
        next: (res) => {
          this.formSubmitting = false;
          if (res.success) {
            this.successMessage = 'Category created successfully.';
            this.closeModal();
            this.loadCategories();
          } else {
            this.errorMessage = res.message;
          }
        },
        error: (err) => {
          this.formSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to create category.';
        }
      });
    }
  }

  confirmDelete(category: ProductCategoryResponse): void {
    this.confirmTitle = 'Delete Category';
    this.confirmMessage = `Are you sure you want to delete category "${category.categoryName}"?`;
    this.pendingAction = () => {
      this.isActionSubmitting = true;
      this.categoryService.delete(category.encryptedCategoryId).subscribe({
        next: () => {
          this.isActionSubmitting = false;
          this.isConfirmOpen = false;
          this.successMessage = 'Category deleted successfully.';
          this.loadCategories();
        },
        error: (err) => {
          this.isActionSubmitting = false;
          this.errorMessage = err.error?.message || 'Failed to delete category.';
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
