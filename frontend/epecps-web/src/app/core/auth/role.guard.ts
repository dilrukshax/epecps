import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, UrlTree } from '@angular/router';
import { Observable, catchError, map, of } from 'rxjs';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree | Observable<boolean | UrlTree> {
    if (!this.authService.isAuthenticated()) {
      return this.router.parseUrl('/login');
    }

    const requiredRoles = (route.data['roles'] as string[] | undefined) ?? [];
    if (requiredRoles.length === 0) {
      return true;
    }

    if (this.authService.hasAnyRole(requiredRoles)) {
      return true;
    }

    // Refresh roles from backend once before denying access.
    return this.authService.getMe().pipe(
      map(() => this.authService.hasAnyRole(requiredRoles) ? true : this.router.parseUrl('/dashboard')),
      catchError(() => of(this.router.parseUrl('/dashboard')))
    );
  }
}
