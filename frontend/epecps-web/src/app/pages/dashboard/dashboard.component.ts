import { Component, OnInit } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { HttpClient } from '@angular/common/http';

interface MeResponse {
  name: string;
  userId: string;
  roles: string[];
  claims: Array<{ type: string; value: string }>;
}

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
  standalone: false
})
export class DashboardComponent implements OnInit {
  userName = '';
  userEmail = '';
  userRoles: string[] = [];
  meData: MeResponse | null = null;
  loading = false;
  error: string | null = null;

  constructor(
    private authService: MsalService,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    const account = this.authService.instance.getActiveAccount();
    if (account) {
      this.userName = account.name || '';
      this.userEmail = account.username || '';
      this.userRoles = account.idTokenClaims?.['roles'] as string[] || [];
    }
  }

  callApiMe(): void {
    this.loading = true;
    this.error = null;
    this.meData = null;

    this.http.get<MeResponse>('https://localhost:7275/api/v1/auth/me')
      .subscribe({
        next: (response) => {
          this.meData = response;
          this.loading = false;
        },
        error: (err) => {
          this.error = err?.error?.detail || err?.message || 'Request failed';
          this.loading = false;
        }
      });
  }
}
