import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MyGoalsComponent } from './components/my-goals/my-goals.component';
import { GoalDetailsComponent } from './components/goal-details/goal-details.component';
import { HrReportsComponent } from './components/hr-reports/hr-reports.component';
import { RmAssignGoalsComponent } from './components/rm-assign-goals/rm-assign-goals.component';
import { MsalGuard } from '@azure/msal-angular';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'goals',
    pathMatch: 'full'
  },
  {
    path: 'goals',
    canActivate: [MsalGuard],
    children: [
      {
        path: '',
        component: MyGoalsComponent
      },
      {
        path: ':id',
        component: GoalDetailsComponent
      }
    ]
  },
  {
    path: 'rm-assign-goals',
    component: RmAssignGoalsComponent,
    canActivate: [MsalGuard]
  },
  {
    path: 'hr-reports',
    component: HrReportsComponent,
    canActivate: [MsalGuard]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class EmployeeRoutingModule { }
