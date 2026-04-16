import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MyGoalsComponent } from './components/my-goals/my-goals.component';
import { HrReportsComponent } from './components/hr-reports/hr-reports.component';
import { HrPipCasesComponent } from './components/hr-pip-cases/hr-pip-cases.component';
import { RmAssignGoalsComponent } from './components/rm-assign-goals/rm-assign-goals.component';
import { AuthGuard } from '../core/auth/auth.guard';
import { RoleGuard } from '../core/auth/role.guard';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'goals',
    pathMatch: 'full'
  },
  {
    path: 'goals',
    canActivate: [AuthGuard],
    children: [
      {
        path: '',
        component: MyGoalsComponent
      }
    ]
  },
  {
    path: 'rm-assign-goals',
    component: RmAssignGoalsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['RM', 'Admin', 'SuperAdmin'] }
  },
  {
    path: 'hr-reports',
    component: HrReportsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['HR', 'SuperAdmin', 'Admin'] }
  },
  {
    path: 'hr-pip-cases',
    component: HrPipCasesComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['HR', 'SuperAdmin'] }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class EmployeeRoutingModule { }
