import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserProductService } from '../../services/user-product.service';
import { ProductResponse } from '../../../admin/models/product.model';

@Component({
  selector: 'app-user-products',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './products.html',
  styleUrl: './products.scss'
})
export class UserProductsComponent implements OnInit {
  private productService = inject(UserProductService);

  products: ProductResponse[] = [];
  searchQuery = '';

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.productService.getPublicProducts(this.searchQuery).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.products = res.data.products || [];
        }
      }
    });
  }

  onSearch(query: string): void {
    this.searchQuery = query;
    this.loadProducts();
  }
}
