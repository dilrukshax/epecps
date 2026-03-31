import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { SharedModule } from './shared/shared.module';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { AdminDashboardComponent } from './pages/admin-dashboard/admin-dashboard.component';
import { UnauthorizedComponent } from './pages/unauthorized/unauthorized.component';
import { MyApprovalsComponent } from './employee/components/my-approvals/my-approvals.component';
import { EvaluationDetailComponent } from './employee/components/evaluation-detail/evaluation-detail.component';
import { AuthInterceptor } from './core/auth/auth.interceptor';

@NgModule({
  declarations: [
    App,
    DashboardComponent,
    AdminDashboardComponent,
    UnauthorizedComponent,
    MyApprovalsComponent,
    EvaluationDetailComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    FormsModule,
    AppRoutingModule,
    SharedModule
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
  bootstrap: [App]
})
export class AppModule {}
