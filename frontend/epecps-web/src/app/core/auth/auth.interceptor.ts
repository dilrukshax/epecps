import { Injectable } from '@angular/core';
import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Observable, catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private readonly authEndpoints = [
    '/api/v1/auth/login',
    '/api/v1/auth/register',
    '/api/v1/auth/setup-password',
    '/api/v1/auth/refresh'
  ];

  constructor(private authService: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const withToken = this.attachTokenIfNeeded(req);

    return next.handle(withToken).pipe(
      catchError((error: HttpErrorResponse) => {
        if (this.shouldAttemptRefresh(withToken, error)) {
          return this.authService.refreshAccessToken().pipe(
            switchMap(refreshed => {
              if (!refreshed) {
                return throwError(() => error);
              }

              const retried = this.attachTokenIfNeeded(req);
              return next.handle(retried);
            })
          );
        }

        return throwError(() => error);
      })
    );
  }

  private attachTokenIfNeeded(req: HttpRequest<any>): HttpRequest<any> {
    if (!this.shouldAttachToken(req)) {
      return req;
    }

    const token = this.authService.getAccessToken();
    if (!token) {
      return req;
    }

    return req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  private shouldAttachToken(req: HttpRequest<any>): boolean {
    const apiUrl = environment.apiUrl.replace(/\/+$/, '');
    if (!req.url.startsWith(apiUrl)) {
      return false;
    }

    return true;
  }

  private shouldAttemptRefresh(req: HttpRequest<any>, error: HttpErrorResponse): boolean {
    if (error.status !== 401) {
      return false;
    }

    if (this.authEndpoints.some(path => req.url.includes(path))) {
      return false;
    }

    return this.authService.getRefreshToken() != null;
  }
}
