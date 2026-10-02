import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminProductCategoryService } from '../../../services/admin-product-category.service';
import { ProductCategoryCreateRequest } from '../../../models/product-category.model';
import { CustomValidators } from '../../../../common/validators/custom.validators';
import { BreadcrumbComponent } from '../../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../../common/models/breadcrumb.model';

@Component({
  selector: 'app-add-category',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, BreadcrumbComponent],
  templateUrl: './add-category.html',
  styleUrl: './add-category.scss'
})
export class AddCategoryComponent implements OnInit {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Product Categories', url: '/admin/product-categories' },
    { label: 'Add Category' }
  ];
  private fb = inject(FormBuilder);
  private categoryService = inject(AdminProductCategoryService);
  private router = inject(Router);

  categoryForm!: FormGroup;
  submitting = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  ngOnInit(): void {
    this.initForm();
  }

  initForm(): void {
    this.categoryForm = this.fb.group({
      categoryName: ['', [CustomValidators.requiredNonEmpty, CustomValidators.pattern(/^.{2,100}$/, 'invalidLength')]],
      description: ['', [CustomValidators.pattern(/^[\s\S]{0,500}$/, 'invalidLength')]],
      displayOrder: [0, [CustomValidators.nonNegativeInteger]]
    });
  }

  onSubmit(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = null;
    this.successMessage = null;

    const payload: ProductCategoryCreateRequest = {
      categoryName: this.categoryForm.value.categoryName.trim(),
      description: this.categoryForm.value.description?.trim() || '',
      displayOrder: Number(this.categoryForm.value.displayOrder) || 0
    };

    this.categoryService.create(payload).subscribe({
      next: (res) => {
        this.submitting = false;
        if (res.success) {
          this.successMessage = 'Category created successfully! Redirecting to category list...';
          setTimeout(() => {
            this.router.navigate(['/admin/product-categories']);
          }, 1200);
        } else {
          this.errorMessage = res.message || 'Failed to create category.';
        }
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = err.error?.message || 'Server error occurred.';
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/product-categories']);
  }
}
