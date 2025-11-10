import { Component, effect } from '@angular/core';
import { MsalService } from '@azure/msal-angular';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.css'
})
export class App {
  constructor(private msal: MsalService) {
    // Ensure an account is active after redirect
    effect(() => {
      const accounts = this.msal.instance.getAllAccounts();
      if (accounts.length && !this.msal.instance.getActiveAccount()) {
        this.msal.instance.setActiveAccount(accounts[0]);
      }
    });
  }
}
