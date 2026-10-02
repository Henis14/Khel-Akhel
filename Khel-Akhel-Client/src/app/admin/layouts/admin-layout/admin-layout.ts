import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../common/services/auth.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.scss'
})
export class AdminLayoutComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);

  sidebarOpen = true;
  currentUser = this.authService.currentUser;

  // Submenu Toggle States
  customersMenuOpen = true;
  productsMenuOpen = false;
  categoriesMenuOpen = false;
  stockMenuOpen = false;
  reportsMenuOpen = false;
  settingsMenuOpen = false;

  ngOnInit(): void {
    const url = this.router.url;
    if (url.includes('/admin/products')) this.productsMenuOpen = true;
    if (url.includes('/admin/product-categories')) this.categoriesMenuOpen = true;
    if (url.includes('/admin/product-stock')) this.stockMenuOpen = true;
    if (url.includes('/admin/customers') || url.includes('/admin/customer-addresses')) this.customersMenuOpen = true;
  }

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }

  toggleMenu(menu: string): void {
    if (menu === 'customers') this.customersMenuOpen = !this.customersMenuOpen;
    if (menu === 'products') this.productsMenuOpen = !this.productsMenuOpen;
    if (menu === 'categories') this.categoriesMenuOpen = !this.categoriesMenuOpen;
    if (menu === 'stock') this.stockMenuOpen = !this.stockMenuOpen;
    if (menu === 'reports') this.reportsMenuOpen = !this.reportsMenuOpen;
    if (menu === 'settings') this.settingsMenuOpen = !this.settingsMenuOpen;
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/admin/login']);
      },
      error: () => {
        this.router.navigate(['/admin/login']);
      }
    });
  }
}
