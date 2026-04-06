import { Injectable } from '@angular/core';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, finalize, map, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AuthUser {
  userId: number;
  fullName: string;
  email: string;
  status: string;
  isActive: boolean;
  departmentId: number;
  departmentName: string;
  roles: string[];
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: AuthUser;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface SetupPasswordRequest {
  email: string;
  password: string;
  confirmPassword: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  confirmPassword: string;
  departmentId?: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly baseUrl = `${environment.apiUrl}/api/v1/auth`;
  private readonly accessTokenKey = 'epecps.access_token';
  private readonly accessExpiryKey = 'epecps.access_expiry_utc';
  private readonly refreshTokenKey = 'epecps.refresh_token';
  private readonly refreshExpiryKey = 'epecps.refresh_expiry_utc';
  private readonly userKey = 'epecps.user';

  private readonly userSubject = new BehaviorSubject<AuthUser | null>(this.loadUserFromStorage());
  readonly user$ = this.userSubject.asObservable();

  private refreshing = false;
  private rawHttp: HttpClient;

  constructor(
    private http: HttpClient,
    httpBackend: HttpBackend
  ) {
    this.rawHttp = new HttpClient(httpBackend);
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, request).pipe(
      tap(response => this.applySession(response))
    );
  }

  setupPassword(request: SetupPasswordRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/setup-password`, request).pipe(
      tap(response => this.applySession(response))
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, request).pipe(
      tap(response => this.applySession(response))
    );
  }

  logout(): Observable<void> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      this.clearSession();
      return of(void 0);
    }

    return this.rawHttp.post<void>(`${this.baseUrl}/logout`, { refreshToken }, {
      headers: this.buildAuthHeader()
    }).pipe(
      catchError(() => of(void 0)),
      finalize(() => this.clearSession())
    );
  }

  getMe(): Observable<AuthUser> {
    return this.http.get<AuthUser>(`${this.baseUrl}/me`).pipe(
      tap(user => {
        const normalizedUser: AuthUser = {
          ...user,
          roles: this.normalizeRoles(user?.roles)
        };
        this.userSubject.next(normalizedUser);
        localStorage.setItem(this.userKey, JSON.stringify(normalizedUser));
      })
    );
  }

  refreshAccessToken(): Observable<boolean> {
    if (this.refreshing) {
      return of(false);
    }

    const refreshToken = this.getRefreshToken();
    if (!refreshToken || this.isRefreshTokenExpired()) {
      this.clearSession();
      return of(false);
    }

    this.refreshing = true;
    return this.rawHttp.post<AuthResponse>(`${this.baseUrl}/refresh`, { refreshToken }).pipe(
      tap(response => this.applySession(response)),
      map(() => true),
      catchError(() => {
        this.clearSession();
        return of(false);
      }),
      finalize(() => {
        this.refreshing = false;
      })
    );
  }

  getAccessToken(): string | null {
    const token = localStorage.getItem(this.accessTokenKey);
    if (!token || this.isAccessTokenExpired()) {
      return null;
    }

    return token;
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.refreshTokenKey);
  }

  isAuthenticated(): boolean {
    return !!this.getAccessToken() || (!!this.getRefreshToken() && !this.isRefreshTokenExpired());
  }

  hasRole(role: string): boolean {
    const normalizedTarget = role?.trim().toUpperCase();
    if (!normalizedTarget) {
      return false;
    }

    const userRoles = this.normalizeRoles(this.userSubject.value?.roles);
    return userRoles.includes(normalizedTarget);
  }

  hasAnyRole(roles: string[]): boolean {
    const normalizedTargets = this.normalizeRoles(roles);
    if (normalizedTargets.length === 0) {
      return false;
    }

    const userRoles = this.normalizeRoles(this.userSubject.value?.roles);
    return normalizedTargets.some(role => userRoles.includes(role));
  }

  getCurrentUser(): AuthUser | null {
    return this.userSubject.value;
  }

  private applySession(response: AuthResponse): void {
    const normalizedUser: AuthUser = {
      ...response.user,
      roles: this.normalizeRoles(response.user?.roles)
    };

    localStorage.setItem(this.accessTokenKey, response.accessToken);
    localStorage.setItem(this.accessExpiryKey, response.accessTokenExpiresAtUtc);
    localStorage.setItem(this.refreshTokenKey, response.refreshToken);
    localStorage.setItem(this.refreshExpiryKey, response.refreshTokenExpiresAtUtc);
    localStorage.setItem(this.userKey, JSON.stringify(normalizedUser));
    this.userSubject.next(normalizedUser);
  }

  private clearSession(): void {
    localStorage.removeItem(this.accessTokenKey);
    localStorage.removeItem(this.accessExpiryKey);
    localStorage.removeItem(this.refreshTokenKey);
    localStorage.removeItem(this.refreshExpiryKey);
    localStorage.removeItem(this.userKey);
    this.userSubject.next(null);
  }

  private loadUserFromStorage(): AuthUser | null {
    try {
      const raw = localStorage.getItem(this.userKey);
      if (!raw) {
        return null;
      }

      const parsed = JSON.parse(raw) as Partial<AuthUser> | null;
      if (!parsed) {
        return null;
      }

      return {
        userId: parsed.userId ?? 0,
        fullName: parsed.fullName ?? '',
        email: parsed.email ?? '',
        status: parsed.status ?? '',
        isActive: parsed.isActive ?? true,
        departmentId: parsed.departmentId ?? 0,
        departmentName: parsed.departmentName ?? '',
        roles: this.normalizeRoles(parsed.roles)
      };
    } catch {
      return null;
    }
  }

  private normalizeRoles(roles: unknown): string[] {
    if (!Array.isArray(roles)) {
      return [];
    }

    return roles
      .map(role => {
        if (typeof role === 'string') {
          return role.trim().toUpperCase();
        }

        if (role && typeof role === 'object') {
          const candidate = (role as { name?: unknown; roleName?: unknown }).name
            ?? (role as { name?: unknown; roleName?: unknown }).roleName;
          if (typeof candidate === 'string') {
            return candidate.trim().toUpperCase();
          }
        }

        return '';
      })
      .filter(role => role.length > 0);
  }

  private isAccessTokenExpired(): boolean {
    const expiry = localStorage.getItem(this.accessExpiryKey);
    if (!expiry) {
      return true;
    }

    return new Date(expiry).getTime() <= Date.now() + 5000;
  }

  private isRefreshTokenExpired(): boolean {
    const expiry = localStorage.getItem(this.refreshExpiryKey);
    if (!expiry) {
      return true;
    }

    return new Date(expiry).getTime() <= Date.now();
  }

  private buildAuthHeader(): { [header: string]: string } {
    const token = localStorage.getItem(this.accessTokenKey);
    if (!token) {
      return {};
    }

    return {
      Authorization: `Bearer ${token}`
    };
  }
}
