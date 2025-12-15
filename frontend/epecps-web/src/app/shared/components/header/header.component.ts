import { Component, OnInit, OnDestroy } from '@angular/core';
import { MsalService, MsalBroadcastService } from '@azure/msal-angular';
import { InteractionStatus } from '@azure/msal-browser';
import { Subject, filter, takeUntil } from 'rxjs';
import { Router } from '@angular/router';
import { GraphService } from '../../../services/graph.service';

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
  userPhotoUrl: string | null = null;
  mobileMenuOpen = false;
  photoLoadError = false;
  userRoles: string[] = [];

  constructor(
    private authService: MsalService,
    private msalBroadcastService: MsalBroadcastService,
    private router: Router,
    private graphService: GraphService
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
      this.userRoles = account?.idTokenClaims?.['roles'] as string[] || [];
      
      console.log('User logged in:', this.userName, this.userEmail);
      console.log('User roles:', this.userRoles);
      
      // Fetch user's profile photo
      this.loadUserPhoto();
    }
  }

  loadUserPhoto(): void {
    console.log('Attempting to load user photo from Microsoft Graph...');
    this.graphService.getUserPhoto()
      .pipe(takeUntil(this._destroying$))
      .subscribe({
        next: (photoUrl) => {
          if (photoUrl) {
            console.log('✅ User photo loaded successfully');
            this.userPhotoUrl = photoUrl;
            this.photoLoadError = false;
          } else {
            console.log('⚠️ No photo URL returned (user may not have a profile photo)');
            this.userPhotoUrl = null;
            this.photoLoadError = true;
          }
        },
        error: (err) => {
          console.error('❌ Error loading user photo:', err);
          console.error('Error status:', err.status);
          console.error('Error message:', err.message);
          
          if (err.status === 403) {
            console.error('🔒 Permission denied. Please add "User.Read" permission in Azure AD and grant admin consent.');
          } else if (err.status === 404) {
            console.log('ℹ️ User does not have a profile photo set in Microsoft 365.');
          }
          
          this.userPhotoUrl = null;
          this.photoLoadError = true;
        }
      });
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

  hasHrRole(): boolean {
    return this.userRoles.includes('HR');
  }

  hasAdminRole(): boolean {
    return this.userRoles.includes('Admin') || this.userRoles.includes('HOD') || this.userRoles.includes('GM');
  }

  ngOnDestroy(): void {
    this._destroying$.next(undefined);
    this._destroying$.complete();
  }

  /**
   * Get user initials for avatar fallback
   */
  getUserInitials(): string {
    if (!this.userName) return '?';
    const names = this.userName.split(' ');
    if (names.length >= 2) {
      return names[0].charAt(0).toUpperCase() + names[names.length - 1].charAt(0).toUpperCase();
    }
    return this.userName.charAt(0).toUpperCase();
  }
}
