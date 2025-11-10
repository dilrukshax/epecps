import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';

import {
  MsalModule,
  MsalGuard,
  MsalInterceptor,
  MsalRedirectComponent,
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
import { UnauthorizedComponent } from './pages/unauthorized/unauthorized.component';
import { EvaluationsComponent } from './pages/evaluations/evaluations.component';
import { ReviewsComponent } from './pages/reviews/reviews.component';

@NgModule({
  declarations: [
    App,
    DashboardComponent,
    UnauthorizedComponent,
    EvaluationsComponent,
    ReviewsComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
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
  bootstrap: [App, MsalRedirectComponent]
})
export class AppModule {}
