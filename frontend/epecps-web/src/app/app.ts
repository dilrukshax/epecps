import { Component, signal, effect } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { ApiService, MeResponse } from './core/api/api.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('epecps-web');
  me: MeResponse | null = null;
  loading = false;
  error: string | null = null;

  constructor(private msal: MsalService, private api: ApiService) {
    // Ensure an account is active after redirect
    effect(() => {
      const accounts = this.msal.instance.getAllAccounts();
      if (accounts.length && !this.msal.instance.getActiveAccount()) {
        this.msal.instance.setActiveAccount(accounts[0]);
      }
    });
  }

  get isLoggedIn(): boolean {
    return this.msal.instance.getAllAccounts().length > 0;
  }

  get displayName(): string {
    return this.msal.instance.getActiveAccount()?.name ?? '';
  }

  login() {
    this.msal.loginRedirect();
  }

  logout() {
    this.msal.logoutRedirect();
  }

  callMe() {
    this.loading = true;
    this.error = null;
    this.me = null;
    this.api.getMe().subscribe({
      next: (res) => {
        this.me = res;
        this.loading = false;
      },
      error: (err) => {
        this.error = (err?.error?.detail || err?.message || 'Request failed');
        this.loading = false;
      }
    });
  }
}
