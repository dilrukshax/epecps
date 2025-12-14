import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';

import {
  MsalModule,
  MsalGuard,
  MsalInterceptor,
  MsalService,
  MsalBroadcastService
} from '@azure/msal-angular';

import {
  MSALInstanceFactory,
  MSALGuardConfigFactory,
  MSALInterceptorConfigFactory,
  MSAL_INSTANCE_TOKEN,
  MSAL_GUARD_TOKEN,
  MSAL_INTERCEPTOR_TOKEN
} from './core/auth/msal-config';

import { SharedModule } from './shared/shared.module';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { AdminDashboardComponent } from './pages/admin-dashboard/admin-dashboard.component';
import { UnauthorizedComponent } from './pages/unauthorized/unauthorized.component';
import { MyApprovalsComponent } from './employee/components/my-approvals/my-approvals.component';
import { EvaluationDetailComponent } from './employee/components/evaluation-detail/evaluation-detail.component';
import { FormsModule } from '@angular/forms';

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
    MsalModule,
    SharedModule
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: MSAL_INSTANCE_TOKEN, useFactory: MSALInstanceFactory },
    { provide: MSAL_GUARD_TOKEN, useFactory: MSALGuardConfigFactory },
    { provide: MSAL_INTERCEPTOR_TOKEN, useFactory: MSALInterceptorConfigFactory },
    MsalService,
    MsalBroadcastService,
    MsalGuard,
    { provide: HTTP_INTERCEPTORS, useClass: MsalInterceptor, multi: true }
  ],
  bootstrap: [App]
})
export class AppModule {}
