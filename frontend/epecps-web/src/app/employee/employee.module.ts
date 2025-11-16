import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EmployeeRoutingModule } from './employee-routing.module';

// Components
import { MyGoalsComponent } from './components/my-goals/my-goals.component';
import { SetNewGoalComponent } from './components/set-new-goal/set-new-goal.component';
import { GoalDetailsComponent } from './components/goal-details/goal-details.component';

// Shared Module (contains HeaderComponent)
import { SharedModule } from '../shared/shared.module';

/**
 * Employee module for goal-setting functionality
 */
@NgModule({
  declarations: [
    MyGoalsComponent,
    SetNewGoalComponent,
    GoalDetailsComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    EmployeeRoutingModule,
    SharedModule
  ]
})
export class EmployeeModule { }
