import { Component } from '@angular/core';
import { MsalService } from '@azure/msal-angular';

@Component({
  selector: 'app-unauthorized',
  templateUrl: './unauthorized.component.html',
  styleUrls: ['./unauthorized.component.css'],
  standalone: false
})
export class UnauthorizedComponent {
  constructor(private authService: MsalService) {}

  login() {
    this.authService.loginRedirect();
  }
}
