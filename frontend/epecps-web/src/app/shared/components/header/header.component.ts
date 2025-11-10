import { Component, OnInit, OnDestroy } from '@angular/core';
import { MsalService, MsalBroadcastService } from '@azure/msal-angular';
import { InteractionStatus } from '@azure/msal-browser';
import { Subject, filter, takeUntil } from 'rxjs';
import { Router } from '@angular/router';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css'],
  standalone: false
})
export class HeaderComponent implements OnInit, OnDestroy {
  private readonly _destroying$ = new Subject<void>();
  isUserLoggedIn = false;
  userName = '';
  userEmail = '';
  mobileMenuOpen = false;

  constructor(
    private authService: MsalService,
    private msalBroadcastService: MsalBroadcastService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.msalBroadcastService.inProgress$
      .pipe(
        filter((status: InteractionStatus) => status === InteractionStatus.None),
        takeUntil(this._destroying$)
      )
      .subscribe(() => {
        this.setLoginDisplay();
      });
    
    this.setLoginDisplay();
  }

  setLoginDisplay() {
    this.isUserLoggedIn = this.authService.instance.getAllAccounts().length > 0;
    if (this.isUserLoggedIn) {
      const account = this.authService.instance.getActiveAccount();
      this.userName = account?.name || '';
      this.userEmail = account?.username || '';
    }
  }

  login() {
    this.authService.loginRedirect();
  }

  logout() {
    this.authService.logoutRedirect();
  }

  toggleMobileMenu() {
    this.mobileMenuOpen = !this.mobileMenuOpen;
  }

  navigateTo(path: string) {
    this.router.navigate([path]);
    this.mobileMenuOpen = false;
  }

  ngOnDestroy(): void {
    this._destroying$.next(undefined);
    this._destroying$.complete();
  }
}
