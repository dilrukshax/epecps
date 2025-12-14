import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MsalGuard, MsalRedirectComponent } from '@azure/msal-angular';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { AdminDashboardComponent } from './pages/admin-dashboard/admin-dashboard.component';
import { UnauthorizedComponent } from './pages/unauthorized/unauthorized.component';
import { MyApprovalsComponent } from './employee/components/my-approvals/my-approvals.component';
import { EvaluationDetailComponent } from './employee/components/evaluation-detail/evaluation-detail.component';

const routes: Routes = [
  // MSAL redirect callback route - processes the authentication response
  // This route renders MsalRedirectComponent which handles the OAuth redirect
  { 
    path: 'auth-callback', 
    component: MsalRedirectComponent 
  },

  // Public login page
  { 
    path: 'login', 
    component: UnauthorizedComponent 
  },

  // Protected routes - require authentication
  { 
    path: 'dashboard', 
    component: DashboardComponent, 
    canActivate: [MsalGuard] 
  },
  
  // Admin routes
  {
    path: 'admin/dashboard',
    component: AdminDashboardComponent,
    canActivate: [MsalGuard]
  },
  {
    path: 'admin/templates',
    loadChildren: () => import('./admin/admin-templates.module').then(m => m.AdminTemplatesModule),
    canActivate: [MsalGuard]
  },

  // Employee routes
  {
    path: 'employee',
    loadChildren: () => import('./employee/employee.module').then(m => m.EmployeeModule),
    canActivate: [MsalGuard]
  },

  // Evaluation routes
  {
    path: 'evaluations/my-approvals',
    component: MyApprovalsComponent,
    canActivate: [MsalGuard]
  },
  {
    path: 'evaluations/:id',
    component: EvaluationDetailComponent,
    canActivate: [MsalGuard]
  },

  // Default route - redirect to login (MsalGuard will handle redirect to dashboard if authenticated)
  { path: '', redirectTo: '/login', pathMatch: 'full' },

  // Wildcard route
  { path: '**', redirectTo: '/login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {
    // Use hash routing if you have issues with server-side routing
    // useHash: true
  })],
  exports: [RouterModule]
})
export class AppRoutingModule {}
