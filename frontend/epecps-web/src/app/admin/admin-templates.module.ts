import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { TemplateListComponent } from './components/template-list/template-list.component';
import { TemplateEditComponent } from './components/template-edit/template-edit.component';
import { CategoryListComponent } from './components/category-list/category-list.component';
import { CategoryItemsComponent } from './components/category-items/category-items.component';
import { TemplateFormDialogComponent } from './components/template-form-dialog/template-form-dialog.component';
import { CategoryFormDialogComponent } from './components/category-form-dialog/category-form-dialog.component';
import { ItemFormDialogComponent } from './components/item-form-dialog/item-form-dialog.component';
import { UserManagementComponent } from './components/user-management/user-management.component';
import { GoalLibraryComponent } from './components/goal-library/goal-library.component';
import { SharedModule } from '../shared/shared.module';
import { AuthGuard } from '../core/auth/auth.guard';
import { RoleGuard } from '../core/auth/role.guard';

const routes: Routes = [
  {
    path: '',
    component: GoalLibraryComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'SuperAdmin'] }
  },
  {
    path: 'templates',
    component: TemplateListComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'SuperAdmin'] }
  },
  {
    path: 'users',
    component: UserManagementComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['SuperAdmin'] }
  },
  {
    path: ':id',
    component: TemplateEditComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'SuperAdmin'] }
  }
];

@NgModule({
  declarations: [
    TemplateListComponent,
    TemplateEditComponent,
    CategoryListComponent,
    CategoryItemsComponent,
    TemplateFormDialogComponent,
    CategoryFormDialogComponent,
    ItemFormDialogComponent,
    UserManagementComponent,
    GoalLibraryComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    SharedModule,
    RouterModule.forChild(routes)
  ]
})
export class AdminTemplatesModule { }
