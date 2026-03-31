import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, UrlTree } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree {
    if (!this.authService.isAuthenticated()) {
      return this.router.parseUrl('/login');
    }

    const requiredRoles = (route.data['roles'] as string[] | undefined) ?? [];
    if (requiredRoles.length === 0 || this.authService.hasAnyRole(requiredRoles)) {
      return true;
    }

    return this.router.parseUrl('/dashboard');
  }
}
