import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MyGoalsComponent } from './components/my-goals/my-goals.component';
import { SetNewGoalComponent } from './components/set-new-goal/set-new-goal.component';
import { GoalDetailsComponent } from './components/goal-details/goal-details.component';
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
        path: 'new',
        component: SetNewGoalComponent
      },
      {
        path: ':id',
        component: GoalDetailsComponent
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class EmployeeRoutingModule { }
