import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AdminCustomerService } from '../../services/admin-customer.service';
import { AdminProductCategoryService } from '../../services/admin-product-category.service';
import { AdminProductService } from '../../services/admin-product.service';
import { AdminProductStockService } from '../../services/admin-product-stock.service';
import { AdminCommissionedProjectService } from '../../services/admin-commissioned-project.service';
import { AdminProjectService } from '../../services/admin-project.service';
import { AdminContactInquiryService } from '../../services/admin-contact-inquiry.service';
import { BreadcrumbComponent } from '../../../common/components/breadcrumb/breadcrumb';
import { BreadcrumbItem } from '../../../common/models/breadcrumb.model';
import { AppLoaderComponent } from '../../../common/components/app-loader/app-loader';
import { WebsiteStatisticsComponent } from './website-statistics/website-statistics';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    BreadcrumbComponent,
    AppLoaderComponent,
    WebsiteStatisticsComponent,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class AdminDashboardComponent implements OnInit {
  breadcrumbs: BreadcrumbItem[] = [
    { label: 'Home', url: '/admin/dashboard' },
    { label: 'Dashboard' },
  ];
  private customerService = inject(AdminCustomerService);
  private categoryService = inject(AdminProductCategoryService);
  private productService = inject(AdminProductService);
  private stockService = inject(AdminProductStockService);
  private commissionService = inject(AdminCommissionedProjectService);
  private projectService = inject(AdminProjectService);
  private inquiryService = inject(AdminContactInquiryService);
  private cdr = inject(ChangeDetectorRef);

  loading = true;
  errorMessage: string | null = null;

  totalCustomers = 0;
  totalCategories = 0;
  totalProducts = 0;
  totalStockRecords = 0;
  totalCommissionRequests = 0;
  totalProjects = 0;
  totalContactInquiries = 0;
  activeContactInquiries = 0;

  recentProducts: any[] = [];

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;
    this.errorMessage = null;

    forkJoin({
      customers: this.customerService.getList({ pageIndex: 1, pageSize: 1 }),
      categories: this.categoryService.getList(),
      products: this.productService.getList({ pageIndex: 1, pageSize: 5 }),
      stock: this.stockService.getList({ pageIndex: 1, pageSize: 1 }),
      commissions: this.commissionService.getList({ pageIndex: 1, pageSize: 1 }),
      projects: this.projectService.getList({ pageIndex: 1, pageSize: 1 }),
      inquiries: this.inquiryService.getSummary(),
    }).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.customers.success && res.customers.data) {
          this.totalCustomers = res.customers.data.totalRecords;
        }
        if (res.categories.success && Array.isArray(res.categories.data)) {
          this.totalCategories = res.categories.data.length;
        }
        if (res.products.success && res.products.data) {
          this.totalProducts = res.products.data.totalRecords;
          this.recentProducts = res.products.data.products || [];
        }
        if (res.stock.success && res.stock.data) {
          this.totalStockRecords = res.stock.data.totalRecords;
        }
        if (res.commissions.success && res.commissions.data) {
          this.totalCommissionRequests = res.commissions.data.totalRecords;
        }
        if (res.projects.success && res.projects.data) {
          this.totalProjects = res.projects.data.totalRecords;
        }
        if (res.inquiries.success && res.inquiries.data) {
          this.totalContactInquiries = res.inquiries.data.totalInquiries;
          this.activeContactInquiries = res.inquiries.data.activeInquiries;
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Failed to load dashboard statistics from server.';
        this.cdr.markForCheck();
      },
    });
  }
}
