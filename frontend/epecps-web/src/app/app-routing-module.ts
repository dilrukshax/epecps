import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MsalGuard, MsalRedirectComponent } from '@azure/msal-angular';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { AdminDashboardComponent } from './pages/admin-dashboard/admin-dashboard.component';
import { UnauthorizedComponent } from './pages/unauthorized/unauthorized.component';

const routes: Routes = [
  // MSAL processes the hash on this route after login
  { path: 'auth-callback', component: MsalRedirectComponent },

  // Public (unauthorized) route
  { path: 'login', component: UnauthorizedComponent },

  // Protected routes - require authentication
  { 
    path: 'dashboard', 
    component: DashboardComponent, 
    canActivate: [MsalGuard] 
  },
  
  // Admin routes - authentication only (no role restrictions)
  {
    path: 'admin/dashboard',
    component: AdminDashboardComponent,
    canActivate: [MsalGuard]
  },
  {
    path: 'admin/templates',
    loadChildren: () => import('./admin/admin-templates.module').then(m => m.AdminTemplatesModule)
  },

  // Employee routes - personal goal management
  {
    path: 'employee',
    loadChildren: () => import('./employee/employee.module').then(m => m.EmployeeModule),
    canActivate: [MsalGuard]
  },

  // Redirect root to dashboard if authenticated, otherwise to login
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },

  // Wildcard route
  { path: '**', redirectTo: '/dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
