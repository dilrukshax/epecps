import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { AdminDashboardComponent } from './pages/admin-dashboard/admin-dashboard.component';
import { UnauthorizedComponent } from './pages/unauthorized/unauthorized.component';
import { MyApprovalsComponent } from './employee/components/my-approvals/my-approvals.component';
import { EvaluationDetailComponent } from './employee/components/evaluation-detail/evaluation-detail.component';
import { AuthGuard } from './core/auth/auth.guard';
import { RoleGuard } from './core/auth/role.guard';

const routes: Routes = [
  { path: 'login', component: UnauthorizedComponent },
  { path: 'register', component: UnauthorizedComponent, data: { mode: 'register' } },
  { path: 'setup-password', component: UnauthorizedComponent, data: { mode: 'setup' } },

  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'admin/dashboard',
    component: AdminDashboardComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'SuperAdmin', 'HOD', 'GM'] }
  },
  {
    path: 'admin/templates',
    loadChildren: () => import('./admin/admin-templates.module').then(m => m.AdminTemplatesModule),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'SuperAdmin', 'HOD', 'GM'] }
  },
  {
    path: 'employee',
    loadChildren: () => import('./employee/employee.module').then(m => m.EmployeeModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'evaluations/my-approvals',
    component: MyApprovalsComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'evaluations/:id',
    component: EvaluationDetailComponent,
    canActivate: [AuthGuard]
  },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: '/dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
