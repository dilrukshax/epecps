import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { MsalGuard } from '@azure/msal-angular';

import { TemplateListComponent } from './components/template-list/template-list.component';
import { TemplateEditComponent } from './components/template-edit/template-edit.component';
import { CategoryListComponent } from './components/category-list/category-list.component';
import { TemplateFormDialogComponent } from './components/template-form-dialog/template-form-dialog.component';
import { CategoryFormDialogComponent } from './components/category-form-dialog/category-form-dialog.component';
import { SharedModule } from '../shared/shared.module';

const routes: Routes = [
  {
    path: '',
    component: TemplateListComponent,
    canActivate: [MsalGuard]
  },
  {
    path: ':id',
    component: TemplateEditComponent,
    canActivate: [MsalGuard]
  }
];

@NgModule({
  declarations: [
    TemplateListComponent,
    TemplateEditComponent,
    CategoryListComponent,
    TemplateFormDialogComponent,
    CategoryFormDialogComponent
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
