import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MsalGuard, MsalRedirectComponent } from '@azure/msal-angular';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { AdminDashboardComponent } from './pages/admin-dashboard/admin-dashboard.component';
import { UnauthorizedComponent } from './pages/unauthorized/unauthorized.component';
import { EvaluationsComponent } from './pages/evaluations/evaluations.component';
import { ReviewsComponent } from './pages/reviews/reviews.component';

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
  { 
    path: 'evaluations', 
    component: EvaluationsComponent, 
    canActivate: [MsalGuard] 
  },
  { 
    path: 'reviews', 
    component: ReviewsComponent, 
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
