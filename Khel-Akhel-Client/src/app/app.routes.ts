import { Routes } from '@angular/router';
import { adminGuard } from './common/guards/admin.guard';
import { UnauthorizedComponent } from './common/components/unauthorized/unauthorized';

export const routes: Routes = [
  // Admin Login Route (Unprotected layout)
  {
    path: 'admin/login',
    loadComponent: () => import('./admin/pages/login/login').then((m) => m.AdminLoginComponent)
  },

  // Protected Admin Routes with Admin Layout
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () => import('./admin/layouts/admin-layout/admin-layout').then((m) => m.AdminLayoutComponent),
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./admin/pages/dashboard/dashboard').then((m) => m.AdminDashboardComponent)
      },
      {
        path: 'customers',
        loadComponent: () => import('./admin/pages/customers/customers').then((m) => m.CustomersComponent)
      },
      {
        path: 'customers/add',
        loadComponent: () => import('./admin/pages/customers/add-customer/add-customer').then((m) => m.AddCustomerComponent)
      },
      {
        path: 'customers/:encryptedId',
        loadComponent: () => import('./admin/pages/customers/customer-detail/customer-detail').then((m) => m.CustomerDetailComponent)
      },
      {
        path: 'customer-addresses',
        loadComponent: () => import('./admin/pages/customer-addresses/customer-addresses').then((m) => m.CustomerAddressesComponent)
      },
      {
        path: 'product-categories',
        loadComponent: () => import('./admin/pages/product-categories/product-categories').then((m) => m.ProductCategoriesComponent)
      },
      {
        path: 'product-categories/add',
        loadComponent: () => import('./admin/pages/product-categories/add-category/add-category').then((m) => m.AddCategoryComponent)
      },
      {
        path: 'products',
        loadComponent: () => import('./admin/pages/products/products').then((m) => m.ProductsComponent)
      },
      {
        path: 'products/add',
        loadComponent: () => import('./admin/pages/products/add-product/add-product').then((m) => m.AddProductComponent)
      },
      {
        path: 'products/:encryptedId',
        loadComponent: () => import('./admin/pages/products/product-detail/product-detail').then((m) => m.ProductDetailComponent)
      },
      {
        path: 'product-stock',
        loadComponent: () => import('./admin/pages/product-stock/product-stock').then((m) => m.ProductStockComponent)
      },
      {
        path: 'product-stock/update',
        loadComponent: () => import('./admin/pages/product-stock/update-stock/update-stock').then((m) => m.UpdateStockComponent)
      }
    ]
  },

  // User Customer-Facing Routes with User Layout
  {
    path: '',
    loadComponent: () => import('./user/layouts/user-layout/user-layout').then((m) => m.UserLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () => import('./user/pages/home/home').then((m) => m.UserHomeComponent)
      },
      {
        path: 'products',
        loadComponent: () => import('./user/pages/products/products').then((m) => m.UserProductsComponent)
      },
      {
        path: 'login',
        loadComponent: () => import('./user/pages/login/login').then((m) => m.UserLoginComponent)
      },
      {
        path: 'register',
        loadComponent: () => import('./user/pages/register/register').then((m) => m.UserRegisterComponent)
      }
    ]
  },

  // Unauthorized Access
  {
    path: 'unauthorized',
    component: UnauthorizedComponent
  },

  // Wildcard Fallback
  {
    path: '**',
    redirectTo: ''
  }
];
