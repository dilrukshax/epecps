import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css'],
  standalone: false
})
export class HeaderComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();

  isUserLoggedIn = false;
  userName = '';
  userEmail = '';
  mobileMenuOpen = false;
  userRoles: string[] = [];

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.authService.user$
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        this.isUserLoggedIn = !!user;
        this.userName = user?.fullName || '';
        this.userEmail = user?.email || '';
        this.userRoles = user?.roles || [];
      });

    if (this.authService.isAuthenticated()) {
      this.authService.getMe().subscribe({
        error: () => {
          // Keep existing session state if profile refresh fails transiently.
        }
      });
    }
  }

  login(): void {
    this.router.navigate(['/login']);
  }

  logout(): void {
    this.authService.logout().subscribe({
      complete: () => this.router.navigate(['/login']),
      error: () => this.router.navigate(['/login'])
    });
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen = !this.mobileMenuOpen;
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
    this.mobileMenuOpen = false;
  }

  hasHrReportsAccess(): boolean {
    return this.authService.hasAnyRole(['HR', 'SuperAdmin', 'Admin']);
  }

  hasHrPipCasesAccess(): boolean {
    return this.authService.hasAnyRole(['HR', 'SuperAdmin']);
  }

  hasAdminRole(): boolean {
    return this.authService.hasAnyRole(['Admin', 'SuperAdmin']);
  }

  hasRmRole(): boolean {
    return this.authService.hasAnyRole(['RM', 'SuperAdmin', 'Admin']);
  }

  getUserInitials(): string {
    if (!this.userName) return '?';
    const names = this.userName.split(' ');
    if (names.length >= 2) {
      return names[0].charAt(0).toUpperCase() + names[names.length - 1].charAt(0).toUpperCase();
    }
    return this.userName.charAt(0).toUpperCase();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
