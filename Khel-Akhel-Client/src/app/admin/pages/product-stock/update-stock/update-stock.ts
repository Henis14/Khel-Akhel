import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AdminProductStockService } from '../../../services/admin-product-stock.service';
import { AdminProductService } from '../../../services/admin-product.service';
import { ProductResponse } from '../../../models/product.model';
import { ProductStockAddRequest } from '../../../models/product-stock.model';
import { CustomValidators } from '../../../../common/validators/custom.validators';
import { BreadcrumbComponent } from '../../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../../common/models/breadcrumb.model';

@Component({
  selector: 'app-update-stock',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, BreadcrumbComponent],
  templateUrl: './update-stock.html',
  styleUrl: './update-stock.scss',
})
export class UpdateStockComponent implements OnInit {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Product Stock', url: '/admin/product-stock' },
    { label: 'Add Stock' },
  ];

  private fb = inject(FormBuilder);
  private stockService = inject(AdminProductStockService);
  private productService = inject(AdminProductService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  productsList: ProductResponse[] = [];
  selectedProduct: ProductResponse | null = null;
  stockForm!: FormGroup;

  loadingProducts = false;
  submitting = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  ngOnInit(): void {
    this.initForm();
    this.loadProducts();
  }

  initForm(): void {
    this.stockForm = this.fb.group({
      encryptedProductId: ['', [CustomValidators.requiredNonEmpty]],
      quantity: [1, [CustomValidators.requiredNonEmpty, CustomValidators.positiveInteger]],
    });
  }

  loadProducts(): void {
    this.loadingProducts = true;
    this.productService.getList({ pageIndex: 1, pageSize: 500 }).subscribe({
      next: (res) => {
        this.loadingProducts = false;
        if (res.success && res.data) {
          this.productsList = res.data.products || [];

          const paramId = this.route.snapshot.queryParamMap.get('productId');
          if (paramId && this.productsList.some((p) => p.encryptedProductId === paramId)) {
            this.stockForm.patchValue({ encryptedProductId: paramId });
            this.onProductChange();
          }
        }
      },
      error: () => {
        this.loadingProducts = false;
      },
    });
  }

  onProductChange(): void {
    const selectedId = this.stockForm.get('encryptedProductId')?.value;
    this.selectedProduct =
      this.productsList.find((p) => p.encryptedProductId === selectedId) || null;
  }

  get newTotalStock(): number {
    const current = this.selectedProduct?.availableQty || 0;
    const qty = Number(this.stockForm.get('quantity')?.value) || 0;
    return current + (qty > 0 ? qty : 0);
  }

  onSubmit(): void {
    if (this.stockForm.invalid) {
      this.stockForm.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = null;
    this.successMessage = null;

    const payload: ProductStockAddRequest = {
      encryptedProductId: this.stockForm.value.encryptedProductId,
      quantity: Number(this.stockForm.value.quantity),
    };

    this.stockService.addStock(payload).subscribe({
      next: (res) => {
        this.submitting = false;
        if (res.success) {
          this.successMessage = 'Stock quantity added successfully! Redirecting to stock list...';
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
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/product-stock']);
  }
}
