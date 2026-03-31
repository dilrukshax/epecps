import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

type AuthMode = 'login' | 'setup' | 'register';

@Component({
  selector: 'app-unauthorized',
  templateUrl: './unauthorized.component.html',
  styleUrls: ['./unauthorized.component.css'],
  standalone: false
})
export class UnauthorizedComponent implements OnInit {
  mode: AuthMode = 'login';
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  loginEmail = '';
  loginPassword = '';

  setupEmail = '';
  setupPassword = '';
  setupConfirmPassword = '';

  registerFullName = '';
  registerEmail = '';
  registerPassword = '';
  registerConfirmPassword = '';

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
      return;
    }

    const routeMode = this.route.snapshot.data['mode'] as AuthMode | undefined;
    if (routeMode) {
      this.mode = routeMode;
    }

    const emailQuery = this.route.snapshot.queryParamMap.get('email');
    if (emailQuery) {
      this.setupEmail = emailQuery;
      this.mode = 'setup';
    }
  }

  switchMode(mode: AuthMode): void {
    this.mode = mode;
    this.error = null;
    this.successMessage = null;
  }

  submitLogin(): void {
    this.loading = true;
    this.error = null;
    this.successMessage = null;

    this.authService.login({
      email: this.loginEmail,
      password: this.loginPassword
    }).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading = false;
        if (err?.status === 409 && err?.error?.code === 'PASSWORD_SETUP_REQUIRED') {
          this.setupEmail = this.loginEmail;
          this.switchMode('setup');
          this.successMessage = 'Account exists but password is not created yet. Set your password to continue.';
          return;
        }

        this.error = err?.error?.message || 'Login failed. Please check your credentials.';
      }
    });
  }

  submitSetupPassword(): void {
    this.loading = true;
    this.error = null;
    this.successMessage = null;

    this.authService.setupPassword({
      email: this.setupEmail,
      password: this.setupPassword,
      confirmPassword: this.setupConfirmPassword
    }).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading = false;
        if (err?.status === 409 && err?.error?.code === 'PASSWORD_ALREADY_SET') {
          this.switchMode('login');
          this.successMessage = 'Password is already created for this account. Please login.';
          this.loginEmail = this.setupEmail;
          return;
        }

        this.error = err?.error?.message || 'Password setup failed.';
      }
    });
  }

  submitRegister(): void {
    this.loading = true;
    this.error = null;
    this.successMessage = null;

    this.authService.register({
      fullName: this.registerFullName,
      email: this.registerEmail,
      password: this.registerPassword,
      confirmPassword: this.registerConfirmPassword
    }).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.message || 'Registration failed.';
      }
    });
  }
}
