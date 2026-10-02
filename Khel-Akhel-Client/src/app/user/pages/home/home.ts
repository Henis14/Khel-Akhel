import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { UserProductService } from '../../services/user-product.service';
import { ProductResponse } from '../../../admin/models/product.model';

@Component({
  selector: 'app-user-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.scss'
})
export class UserHomeComponent implements OnInit {
  private productService = inject(UserProductService);

  featuredProducts: ProductResponse[] = [];

  ngOnInit(): void {
    this.productService.getPublicProducts().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.featuredProducts = res.data.products || [];
        }
      }
    });
  }
}
