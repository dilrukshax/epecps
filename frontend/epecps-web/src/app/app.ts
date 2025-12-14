import { Component, OnInit, OnDestroy, effect } from '@angular/core';
import { Router } from '@angular/router';
import { MsalService, MsalBroadcastService } from '@azure/msal-angular';
import { InteractionStatus, EventMessage, EventType } from '@azure/msal-browser';
import { Subject } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.css'
})
export class App implements OnInit, OnDestroy {
  private readonly _destroying$ = new Subject<void>();

  constructor(
    private msal: MsalService,
    private msalBroadcastService: MsalBroadcastService,
    private router: Router
  ) {
    // Ensure an account is active after redirect
    effect(() => {
      const accounts = this.msal.instance.getAllAccounts();
      if (accounts.length && !this.msal.instance.getActiveAccount()) {
        this.msal.instance.setActiveAccount(accounts[0]);
      }
    });
  }

  ngOnInit(): void {
    // CRITICAL: Handle the redirect observable to process login callbacks
    // This must be called in the root component
    this.msal.handleRedirectObservable().subscribe({
      next: (result) => {
        if (result) {
          console.log('[MSAL] Login successful:', result.account?.username);
          this.msal.instance.setActiveAccount(result.account);
          // Navigate to dashboard after successful login
          this.router.navigate(['/dashboard']);
        }
      },
      error: (error) => {
        console.error('[MSAL] Redirect error:', error);
      }
    });

    // Listen for MSAL events to set active account
    this.msalBroadcastService.msalSubject$
      .pipe(
        filter((msg: EventMessage) => msg.eventType === EventType.LOGIN_SUCCESS),
        takeUntil(this._destroying$)
      )
      .subscribe((result: EventMessage) => {
        console.log('[MSAL] Login success event received');
        this.setActiveAccountIfNeeded();
      });

    // Set active account when interaction is complete
    this.msalBroadcastService.inProgress$
      .pipe(
        filter((status: InteractionStatus) => status === InteractionStatus.None),
        takeUntil(this._destroying$)
      )
      .subscribe(() => {
        this.setActiveAccountIfNeeded();
      });
  }

  private setActiveAccountIfNeeded(): void {
    const accounts = this.msal.instance.getAllAccounts();
    if (accounts.length > 0) {
      if (!this.msal.instance.getActiveAccount()) {
        this.msal.instance.setActiveAccount(accounts[0]);
      }
      console.log('[MSAL] Active account:', this.msal.instance.getActiveAccount()?.username);
    }
  }

  ngOnDestroy(): void {
    this._destroying$.next();
    this._destroying$.complete();
  }
}
