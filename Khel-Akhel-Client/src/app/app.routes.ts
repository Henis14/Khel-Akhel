import { Routes } from '@angular/router';
import { adminGuard } from './common/guards/admin.guard';
import { userGuard } from './common/guards/user.guard';
import { UnauthorizedComponent } from './common/components/unauthorized/unauthorized';

export const routes: Routes = [
  // Admin Login Route (Unprotected layout)
  {
    path: 'admin/login',
    loadComponent: () => import('./admin/pages/login/login').then((m) => m.AdminLoginComponent),
  },

  // Protected Admin Routes with Admin Layout
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./admin/layouts/admin-layout/admin-layout').then((m) => m.AdminLayoutComponent),
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./admin/pages/dashboard/dashboard').then((m) => m.AdminDashboardComponent),
      },
      {
        path: 'customers',
        loadComponent: () =>
          import('./admin/pages/customers/customers').then((m) => m.CustomersComponent),
      },
      {
        path: 'customers/add',
        loadComponent: () =>
          import('./admin/pages/customers/add-customer/add-customer').then(
            (m) => m.AddCustomerComponent,
          ),
      },
      {
        path: 'customers/:encryptedId',
        loadComponent: () =>
          import('./admin/pages/customers/customer-detail/customer-detail').then(
            (m) => m.CustomerDetailComponent,
          ),
      },
      {
        path: 'customer-addresses',
        loadComponent: () =>
          import('./admin/pages/customer-addresses/customer-addresses').then(
            (m) => m.CustomerAddressesComponent,
          ),
      },
      {
        path: 'product-categories',
        loadComponent: () =>
          import('./admin/pages/product-categories/product-categories').then(
            (m) => m.ProductCategoriesComponent,
          ),
      },
      {
        path: 'product-categories/add',
        loadComponent: () =>
          import('./admin/pages/product-categories/add-category/add-category').then(
            (m) => m.AddCategoryComponent,
          ),
      },
      {
        path: 'product-categories/add-subcategory',
        loadComponent: () =>
          import('./admin/pages/product-categories/add-subcategory/add-subcategory').then(
            (m) => m.AddSubCategoryComponent,
          ),
      },
      {
        path: 'products',
        loadComponent: () =>
          import('./admin/pages/products/products').then((m) => m.ProductsComponent),
      },
      {
        path: 'products/add',
        loadComponent: () =>
          import('./admin/pages/products/add-product/add-product').then(
            (m) => m.AddProductComponent,
          ),
      },
      {
        path: 'products/:encryptedId',
        loadComponent: () =>
          import('./admin/pages/products/product-detail/product-detail').then(
            (m) => m.ProductDetailComponent,
          ),
      },
      {
        path: 'product-stock',
        loadComponent: () =>
          import('./admin/pages/product-stock/product-stock').then((m) => m.ProductStockComponent),
      },
      {
        path: 'product-stock/update',
        loadComponent: () =>
          import('./admin/pages/product-stock/update-stock/update-stock').then(
            (m) => m.UpdateStockComponent,
          ),
      },
      {
        path: 'product-stock/add',
        redirectTo: 'product-stock/update',
        pathMatch: 'full',
      },
      {
        path: 'commissioned-projects',
        loadComponent: () =>
          import('./admin/pages/commissioned-projects/commissioned-projects').then(
            (m) => m.CommissionedProjectsComponent,
          ),
      },
      {
        path: 'projects',
        loadComponent: () =>
          import('./admin/pages/projects/project-list/project-list').then(
            (m) => m.ProjectListComponent,
          ),
      },
      {
        path: 'projects/create',
        loadComponent: () =>
          import('./admin/pages/projects/project-form/project-form').then(
            (m) => m.ProjectFormComponent,
          ),
      },
      {
        path: 'projects/edit/:encryptedProjectId',
        loadComponent: () =>
          import('./admin/pages/projects/project-form/project-form').then(
            (m) => m.ProjectFormComponent,
          ),
      },
      {
        path: 'faqs',
        loadComponent: () => import('./admin/pages/faqs/faqs').then((m) => m.FaqsComponent),
      },
      {
        path: 'password-reset-requests',
        loadComponent: () =>
          import('./admin/pages/password-reset-requests/password-reset-requests').then(
            (m) => m.PasswordResetRequestsComponent,
          ),
      },
      {
        path: 'hero-banner',
        loadComponent: () =>
          import('./admin/pages/hero-banner/hero-banner').then((m) => m.HeroBannerComponent),
      },
      {
        path: 'contact-inquiries',
        loadComponent: () =>
          import('./admin/pages/contact-inquiries/contact-inquiries').then(
            (m) => m.ContactInquiriesComponent,
          ),
      },
      // Backward compatibility redirects for legacy /admin/about URLs
      {
        path: 'about',
        redirectTo: 'projects',
        pathMatch: 'full',
      },
      {
        path: 'about/create',
        redirectTo: 'projects/create',
        pathMatch: 'full',
      },
      {
        path: 'about/edit/:encryptedAboutId',
        redirectTo: 'projects/edit/:encryptedAboutId',
      },
      // Admin Child Wildcard: Catch any unknown URL beneath /admin while retaining admin layout and guards
      {
        path: '**',
        loadComponent: () =>
          import('./common/components/error-pages/not-found/not-found').then(
            (m) => m.NotFoundComponent,
          ),
      },
    ],
  },

  // User Customer-Facing Routes with User Layout
  {
    path: '',
    loadComponent: () =>
      import('./user/layouts/user-layout/user-layout').then((m) => m.UserLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () => import('./user/pages/home/home').then((m) => m.UserHomeComponent),
      },
      {
        path: 'projects',
        loadComponent: () =>
          import('./user/pages/projects/projects').then((m) => m.ProjectsComponent),
      },
      {
        path: 'about',
        loadComponent: () => import('./user/pages/about/about').then((m) => m.About),
      },
      {
        path: 'products',
        loadComponent: () =>
          import('./user/pages/products/products').then((m) => m.UserProductsComponent),
      },
      {
        path: 'shop',
        redirectTo: 'products',
        pathMatch: 'full',
      },
      {
        path: 'login',
        loadComponent: () => import('./user/pages/login/login').then((m) => m.UserLoginComponent),
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./user/pages/register/register').then((m) => m.UserRegisterComponent),
      },
      {
        path: 'faq',
        loadComponent: () => import('./user/pages/faq/faq').then((m) => m.Faq),
      },
      {
        path: 'commissions',
        loadComponent: () =>
          import('./user/pages/commissions/commissions').then((m) => m.CommissionsComponent),
      },
      {
        path: 'contact',
        loadComponent: () => import('./user/pages/contact/contact').then((m) => m.Contact),
      },
      {
        path: 'track-order',
        loadComponent: () =>
          import('./user/pages/account/track-order/track-order').then((m) => m.TrackOrder),
      },
      {
        path: 'terms-conditions',
        loadComponent: () =>
          import('./user/pages/policies/terms-conditions/terms-conditions').then(
            (m) => m.TermsConditions,
          ),
      },
      {
        path: 'privacy-policy',
        loadComponent: () =>
          import('./user/pages/policies/privacy-policy/privacy-policy').then(
            (m) => m.PrivacyPolicy,
          ),
      },
      {
        path: 'forgot-password',
        loadComponent: () =>
          import('./user/pages/forgot-password/forgot-password').then(
            (m) => m.ForgotPasswordComponent,
          ),
      },
      {
        path: 'reset-password',
        loadComponent: () =>
          import('./user/pages/reset-password/reset-password').then(
            (m) => m.ResetPasswordComponent,
          ),
      },
      {
        path: 'wishlist',
        loadComponent: () => import('./user/pages/wishlist/wishlist').then((m) => m.Wishlist),
      },
      {
        path: 'account',
        canActivate: [userGuard],
        loadComponent: () =>
          import('./user/pages/account/account-shell/account-shell').then((m) => m.AccountShell),
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./user/pages/account/account-overview/account-overview').then(
                (m) => m.AccountOverview,
              ),
          },
          {
            path: 'addresses',
            loadComponent: () =>
              import('./user/pages/account/manage-addresses/manage-addresses').then(
                (m) => m.ManageAddresses,
              ),
          },
        ],
      },
    ],
  },

  // Unauthorized Access
  {
    path: 'unauthorized',
    component: UnauthorizedComponent,
  },

  // Wildcard Fallback / 404 Not Found Page
  {
    path: '**',
    loadComponent: () =>
      import('./common/components/error-pages/not-found/not-found').then(
        (m) => m.NotFoundComponent,
      ),
  },
];
