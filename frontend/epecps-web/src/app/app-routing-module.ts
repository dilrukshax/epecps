import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MsalRedirectComponent } from '@azure/msal-angular';

const routes: Routes = [
  // MSAL processes the hash on this route after login
  { path: 'auth-callback', component: MsalRedirectComponent },

  // later add feature routes; you can guard them with MsalGuard like:
  // { path: 'secure', component: SomeSecurePageComponent, canActivate: [MsalGuard] },

  { path: '', pathMatch: 'full', redirectTo: 'auth-callback' } // or to a real home page
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
