import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/login/login-page.component').then(m => m.LoginPageComponent) },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./core/layout/shell.component').then(m => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard-page.component').then(m => m.DashboardPageComponent) },
      { path: 'catalogo', loadComponent: () => import('./features/catalog/catalog-page.component').then(m => m.CatalogPageComponent) },
      { path: 'inventario', loadComponent: () => import('./features/inventory/inventory-page.component').then(m => m.InventoryPageComponent) },
      { path: 'carga-inicial', loadComponent: () => import('./features/initial-load/initial-load-page.component').then(m => m.InitialLoadPageComponent) },
      { path: 'demo-admin', loadComponent: () => import('./features/demo-admin/demo-admin-page.component').then(m => m.DemoAdminPageComponent) }
    ]
  },
  { path: '**', redirectTo: '' }
];
