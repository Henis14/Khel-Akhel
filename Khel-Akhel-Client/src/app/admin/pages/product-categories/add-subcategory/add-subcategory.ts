import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AdminProductCategoryService } from '../../../services/admin-product-category.service';
import {
  ProductCategoryResponse,
  ProductSubCategoryCreateRequest,
  ProductSubCategoryResponse,
} from '../../../models/product-category.model';
import { CustomValidators } from '../../../../common/validators/custom.validators';
import { BreadcrumbComponent } from '../../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../../common/models/breadcrumb.model';

@Component({
  selector: 'app-add-subcategory',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, BreadcrumbComponent],
  templateUrl: './add-subcategory.html',
  styleUrl: './add-subcategory.scss',
})
export class AddSubCategoryComponent implements OnInit {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Product Categories', url: '/admin/product-categories' },
    { label: 'Add SubCategory' },
  ];

  private fb = inject(FormBuilder);
  private categoryService = inject(AdminProductCategoryService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);

  subCategoryForm!: FormGroup;

  categories: ProductCategoryResponse[] = [];
  loadingCategories = false;

  existingSubCategories: ProductSubCategoryResponse[] = [];
  loadingExistingSubCategories = false;
  selectedCategoryName = '';

  submitting = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  ngOnInit(): void {
    this.initForm();
    this.route.queryParamMap.subscribe((params) => {
      const catId = params.get('categoryId');
      this.loadCategories(catId || undefined);
    });
  }

  initForm(): void {
    this.subCategoryForm = this.fb.group({
      encryptedCategoryId: ['', [CustomValidators.requiredNonEmpty]],
      subCategoryName: [
        '',
        [
          CustomValidators.requiredNonEmpty,
          CustomValidators.pattern(/^.{2,100}$/, 'invalidLength'),
        ],
      ],
      description: ['', [CustomValidators.pattern(/^[\s\S]{0,500}$/, 'invalidLength')]],
      displayOrder: [0, [CustomValidators.nonNegativeInteger]],
    });
  }

  loadCategories(selectCategoryId?: string): void {
    this.loadingCategories = true;
    this.categoryService.getList().subscribe({
      next: (res) => {
        this.loadingCategories = false;
        if (res.success && res.data) {
          this.categories = res.data.filter((c) => c.isActive);
          if (selectCategoryId) {
            this.subCategoryForm.patchValue({ encryptedCategoryId: selectCategoryId });
            this.onMainCategorySelect(selectCategoryId);
          }
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingCategories = false;
        this.cdr.markForCheck();
      },
    });
  }

  onMainCategorySelect(categoryId: string): void {
    this.successMessage = null;
    this.errorMessage = null;
    this.existingSubCategories = [];
    this.selectedCategoryName = '';

    if (!categoryId) return;

    const matched = this.categories.find((c) => c.encryptedCategoryId === categoryId);
    if (matched) {
      this.selectedCategoryName = matched.categoryName;
    }

    this.loadingExistingSubCategories = true;
    this.categoryService.getSubCategories(categoryId).subscribe({
      next: (res) => {
        this.loadingExistingSubCategories = false;
        if (res.success && res.data) {
          this.existingSubCategories = res.data;
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingExistingSubCategories = false;
        this.cdr.markForCheck();
      },
    });
  }

  onSubmit(): void {
    if (this.subCategoryForm.invalid) {
      this.subCategoryForm.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = null;
    this.successMessage = null;

    const selectedCatId = this.subCategoryForm.value.encryptedCategoryId;
    const payload: ProductSubCategoryCreateRequest = {
      encryptedCategoryId: selectedCatId,
      subCategoryName: this.subCategoryForm.value.subCategoryName.trim(),
      description: this.subCategoryForm.value.description?.trim() || '',
      displayOrder: Number(this.subCategoryForm.value.displayOrder) || 0,
    };

    this.categoryService.createSubCategory(payload).subscribe({
      next: (res) => {
        this.submitting = false;
        if (res.success && res.data) {
          this.successMessage = `SubCategory "${res.data.subCategoryName}" added successfully under "${res.data.categoryName || this.selectedCategoryName}"!`;

          this.subCategoryForm.patchValue({
            subCategoryName: '',
            description: '',
            displayOrder: 0,
          });
          this.subCategoryForm.get('subCategoryName')?.markAsUntouched();
          this.subCategoryForm.get('subCategoryName')?.markAsPristine();

          // Refresh existing subcategories list for immediate feedback
          this.onMainCategorySelect(selectedCatId);
        } else {
          this.errorMessage = res.message || 'Failed to create subcategory.';
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.submitting = false;
        const msg =
          err.error?.errors && err.error.errors.length > 0
            ? err.error.errors.join('; ')
            : err.error?.message || 'Server error occurred while creating subcategory.';
        this.errorMessage = msg;
        this.cdr.markForCheck();
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/product-categories']);
  }
}
