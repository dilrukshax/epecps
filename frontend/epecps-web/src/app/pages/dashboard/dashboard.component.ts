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

  stats = [
    { name: 'Pending Reviews', value: '3', icon: 'clock', color: 'text-yellow-600', bgColor: 'bg-yellow-100' },
    { name: 'Completed Evaluations', value: '12', icon: 'check', color: 'text-green-600', bgColor: 'bg-green-100' },
    { name: 'Team Members', value: '8', icon: 'users', color: 'text-blue-600', bgColor: 'bg-blue-100' },
    { name: 'Goals This Quarter', value: '5', icon: 'target', color: 'text-purple-600', bgColor: 'bg-purple-100' }
  ];

  recentActivities = [
    { action: 'Completed self-evaluation', date: '2 hours ago', type: 'success' },
    { action: 'Peer review requested by John Doe', date: '1 day ago', type: 'info' },
    { action: 'New goal assigned: Q1 2024 Targets', date: '3 days ago', type: 'warning' },
    { action: 'Performance review meeting scheduled', date: '5 days ago', type: 'info' }
  ];

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
