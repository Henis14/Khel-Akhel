import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminProductStockService } from '../../../services/admin-product-stock.service';
import { AdminProductService } from '../../../services/admin-product.service';
import { ProductResponse } from '../../../models/product.model';
import { ProductStockAddRequest, ProductStockUpdateRequest } from '../../../models/product-stock.model';
import { CustomValidators } from '../../../../common/validators/custom.validators';
import { BreadcrumbComponent } from '../../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../../common/models/breadcrumb.model';

@Component({
  selector: 'app-update-stock',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, BreadcrumbComponent],
  templateUrl: './update-stock.html',
  styleUrl: './update-stock.scss'
})
export class UpdateStockComponent implements OnInit {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Product Stock', url: '/admin/product-stock' },
    { label: 'Add / Update Stock' }
  ];
  private fb = inject(FormBuilder);
  private stockService = inject(AdminProductStockService);
  private productService = inject(AdminProductService);
  private router = inject(Router);

  productsList: ProductResponse[] = [];
  mode: 'add' | 'set' = 'add';

  addStockForm!: FormGroup;
  setStockForm!: FormGroup;

  loadingProducts = false;
  submitting = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  ngOnInit(): void {
    this.initForms();
    this.loadProducts();
  }

  initForms(): void {
    this.addStockForm = this.fb.group({
      encryptedProductId: ['', [CustomValidators.requiredNonEmpty]],
      quantity: [1, [CustomValidators.requiredNonEmpty, CustomValidators.positiveNumber]]
    });

    this.setStockForm = this.fb.group({
      encryptedProductId: ['', [CustomValidators.requiredNonEmpty]],
      availableQuantity: [0, [CustomValidators.requiredNonEmpty, CustomValidators.nonNegativeInteger]],
      reservedQuantity: [0, [CustomValidators.nonNegativeInteger]]
    });
  }

  loadProducts(): void {
    this.loadingProducts = true;
    this.productService.getList({ pageIndex: 1, pageSize: 200 }).subscribe({
      next: (res) => {
        this.loadingProducts = false;
        if (res.success && res.data) {
          this.productsList = res.data.products || [];
          if (this.productsList.length > 0) {
            const firstId = this.productsList[0].encryptedProductId;
            this.addStockForm.patchValue({ encryptedProductId: firstId });
            this.setStockForm.patchValue({ encryptedProductId: firstId });
          }
        }
      },
      error: () => {
        this.loadingProducts = false;
      }
    });
  }

  setMode(newMode: 'add' | 'set'): void {
    this.mode = newMode;
    this.errorMessage = null;
    this.successMessage = null;
  }

  onAddStockSubmit(): void {
    if (this.addStockForm.invalid) {
      this.addStockForm.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = null;
    this.successMessage = null;

    const payload: ProductStockAddRequest = {
      encryptedProductId: this.addStockForm.value.encryptedProductId,
      quantity: Number(this.addStockForm.value.quantity)
    };

    this.stockService.addStock(payload).subscribe({
      next: (res) => {
        this.submitting = false;
        if (res.success) {
          this.successMessage = 'Stock quantity added successfully! Redirecting...';
          setTimeout(() => {
            this.router.navigate(['/admin/product-stock']);
          }, 1200);
        } else {
          this.errorMessage = res.message || 'Failed to add stock.';
        }
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = err.error?.message || 'Server error occurred while adding stock.';
      }
    });
  }

  onSetStockSubmit(): void {
    if (this.setStockForm.invalid) {
      this.setStockForm.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = null;
    this.successMessage = null;

    const payload: ProductStockUpdateRequest = {
      encryptedProductId: this.setStockForm.value.encryptedProductId,
      availableQuantity: Number(this.setStockForm.value.availableQuantity),
      reservedQuantity: Number(this.setStockForm.value.reservedQuantity) || 0
    };

    this.stockService.updateStock(payload).subscribe({
      next: (res) => {
        this.submitting = false;
        if (res.success) {
          this.successMessage = 'Stock quantity updated successfully! Redirecting...';
          setTimeout(() => {
            this.router.navigate(['/admin/product-stock']);
          }, 1200);
        } else {
          this.errorMessage = res.message || 'Failed to update stock.';
        }
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = err.error?.message || 'Server error occurred while updating stock.';
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/product-stock']);
  }
}
