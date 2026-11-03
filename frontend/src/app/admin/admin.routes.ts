import { Routes } from '@angular/router';
import { adminGuard } from './admin.guard';

export const ADMIN_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./login/admin-login.component').then(m => m.AdminLoginComponent)
  },
  {
    path: '',
    canActivate: [adminGuard],
    loadComponent: () => import('./admin-shell.component').then(m => m.AdminShellComponent),
    children: [
      { path: '', pathMatch: 'full', loadComponent: () => import('./dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent) },
      { path: 'porosite', loadComponent: () => import('./orders/admin-orders.component').then(m => m.AdminOrdersComponent) },
      { path: 'porosite/:orderNo', loadComponent: () => import('./orders/admin-order-detail.component').then(m => m.AdminOrderDetailComponent) },
      { path: 'produktet', loadComponent: () => import('./products/admin-products.component').then(m => m.AdminProductsComponent) },
      { path: 'produktet/new', loadComponent: () => import('./products/admin-product-form.component').then(m => m.AdminProductFormComponent) },
      { path: 'produktet/:id', loadComponent: () => import('./products/admin-product-form.component').then(m => m.AdminProductFormComponent) },
      { path: '**', redirectTo: '' }
    ]
  }
];
