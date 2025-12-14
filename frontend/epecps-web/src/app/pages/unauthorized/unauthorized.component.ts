import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { MsalService, MsalBroadcastService } from '@azure/msal-angular';
import { InteractionStatus } from '@azure/msal-browser';
import { Subject } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-unauthorized',
  templateUrl: './unauthorized.component.html',
  styleUrls: ['./unauthorized.component.css'],
  standalone: false
})
export class UnauthorizedComponent implements OnInit, OnDestroy {
  private readonly _destroying$ = new Subject<void>();
  isLoading = true;

  constructor(
    private authService: MsalService,
    private msalBroadcastService: MsalBroadcastService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Wait for MSAL to finish any interaction before checking login status
    this.msalBroadcastService.inProgress$
      .pipe(
        filter((status: InteractionStatus) => status === InteractionStatus.None),
        takeUntil(this._destroying$)
      )
      .subscribe(() => {
        this.isLoading = false;
        this.checkAndRedirectIfLoggedIn();
      });
  }

  private checkAndRedirectIfLoggedIn(): void {
    const accounts = this.authService.instance.getAllAccounts();
    if (accounts.length > 0) {
      // User is already logged in, redirect to dashboard
      console.log('[Login] User already authenticated, redirecting to dashboard');
      this.router.navigate(['/dashboard']);
    }
  }

  login(): void {
    this.authService.loginRedirect();
  }

  ngOnDestroy(): void {
    this._destroying$.next();
    this._destroying$.complete();
  }
}
