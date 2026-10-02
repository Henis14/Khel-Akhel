import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { AdminProductStockService } from '../../services/admin-product-stock.service';
import { AdminProductService } from '../../services/admin-product.service';
import { ProductStockAddRequest, ProductStockResponse, ProductStockUpdateRequest } from '../../models/product-stock.model';
import { ProductResponse } from '../../models/product.model';
import { CustomValidators } from '../../../common/validators/custom.validators';
import { PaginationComponent } from '../../../common/components/pagination/pagination';
import { BreadcrumbComponent } from '../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../common/models/breadcrumb.model';
import { AppLoaderComponent } from '../../../common/components/app-loader/app-loader';

@Component({
  selector: 'app-product-stock',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PaginationComponent, BreadcrumbComponent, AppLoaderComponent],
  templateUrl: './product-stock.html',
  styleUrl: './product-stock.scss'
})
export class ProductStockComponent implements OnInit {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Product Stock' }
  ];
  private stockService = inject(AdminProductStockService);
  private productService = inject(AdminProductService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);

  stocks: ProductStockResponse[] = [];
  productsList: ProductResponse[] = [];

  totalRecords = 0;
  pageIndex = 1;
  pageSize = 10;
  searchQuery = '';
  lowStockOnly = false;

  loading = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  // Add Stock Modal
  isAddModalOpen = false;
  addStockForm!: FormGroup;

  // Update Stock Modal
  isUpdateModalOpen = false;
  selectedStock: ProductStockResponse | null = null;
  updateStockForm!: FormGroup;

  formSubmitting = false;

  ngOnInit(): void {
    this.initForms();
    this.loadProductsList();
    this.loadStock();
  }

  initForms(): void {
    this.addStockForm = this.fb.group({
      encryptedProductId: ['', [CustomValidators.requiredNonEmpty]],
      quantity: [1, [CustomValidators.positiveNumber]]
    });

    this.updateStockForm = this.fb.group({
      availableQuantity: [0, [CustomValidators.nonNegativeInteger]],
      reservedQuantity: [0, [CustomValidators.nonNegativeInteger]]
    });
  }

  loadProductsList(): void {
    this.productService.getList({ pageIndex: 1, pageSize: 100 }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.productsList = res.data.products || [];
        }
        this.cdr.markForCheck();
      }
    });
  }

  loadStock(): void {
    this.loading = true;
    this.errorMessage = null;

    this.stockService
      .getList({
        pageIndex: this.pageIndex,
        pageSize: this.pageSize,
        search: this.searchQuery.trim() || undefined,
        lowStockOnly: this.lowStockOnly || undefined
      })
      .subscribe({
        next: (res) => {
          this.loading = false;
          if (res.success && res.data) {
            this.stocks = res.data.stocks || [];
            this.totalRecords = res.data.totalRecords || 0;
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.loading = false;
          this.errorMessage = err.error?.message || 'Failed to load stock records.';
          this.cdr.markForCheck();
        }
      });
  }

  onSearch(term: string): void {
    this.searchQuery = term;
    this.pageIndex = 1;
    this.loadStock();
  }

  toggleLowStockFilter(): void {
    this.lowStockOnly = !this.lowStockOnly;
    this.pageIndex = 1;
    this.loadStock();
  }

  onPageChange(newPage: number): void {
    this.pageIndex = newPage;
    this.loadStock();
  }

  openAddModal(): void {
    this.addStockForm.reset({
      encryptedProductId: this.productsList.length > 0 ? this.productsList[0].encryptedProductId : '',
      quantity: 1
    });
    this.isAddModalOpen = true;
  }

  openUpdateModal(stock: ProductStockResponse): void {
    this.selectedStock = stock;
    this.updateStockForm.patchValue({
      availableQuantity: stock.availableQty,
      reservedQuantity: stock.reservedQty
    });
    this.isUpdateModalOpen = true;
  }

  closeModals(): void {
    this.isAddModalOpen = false;
    this.isUpdateModalOpen = false;
  }

  submitAddStock(): void {
    if (this.addStockForm.invalid) {
      this.addStockForm.markAllAsTouched();
      return;
    }

    this.formSubmitting = true;
    this.errorMessage = null;

    const payload: ProductStockAddRequest = {
      encryptedProductId: this.addStockForm.value.encryptedProductId,
      quantity: Number(this.addStockForm.value.quantity)
    };

    this.stockService.addStock(payload).subscribe({
      next: (res) => {
        this.formSubmitting = false;
        if (res.success) {
          this.successMessage = 'Stock quantity added successfully.';
          this.closeModals();
          this.loadStock();
        } else {
          this.errorMessage = res.message;
        }
      },
      error: (err) => {
        this.formSubmitting = false;
        this.errorMessage = err.error?.message || 'Failed to add stock.';
      }
    });
  }

  submitUpdateStock(): void {
    if (this.updateStockForm.invalid || !this.selectedStock) {
      this.updateStockForm.markAllAsTouched();
      return;
    }

    this.formSubmitting = true;
    this.errorMessage = null;

    const payload: ProductStockUpdateRequest = {
      encryptedProductId: this.selectedStock.encryptedProductId,
      availableQuantity: Number(this.updateStockForm.value.availableQuantity),
      reservedQuantity: Number(this.updateStockForm.value.reservedQuantity)
    };

    this.stockService.updateStock(payload).subscribe({
      next: (res) => {
        this.formSubmitting = false;
        if (res.success) {
          this.successMessage = 'Stock updated successfully.';
          this.closeModals();
          this.loadStock();
        } else {
          this.errorMessage = res.message;
        }
      },
      error: (err) => {
        this.formSubmitting = false;
        this.errorMessage = err.error?.message || 'Failed to update stock.';
      }
    });
  }
}
