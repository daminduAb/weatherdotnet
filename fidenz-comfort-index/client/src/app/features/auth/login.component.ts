import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '@auth0/auth0-angular';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  // Inject AuthService directly in the property declaration
  public auth = inject(AuthService);

  // Now 'this.auth' is fully initialized and valid here
  isAuthenticated$ = this.auth.isAuthenticated$;
  user$ = this.auth.user$;
  isLoading$ = this.auth.isLoading$;

  login(): void {
    this.auth.loginWithRedirect();
  }

  logout(): void {
    this.auth.logout({
      logoutParams: { returnTo: window.location.origin }
    });
  }
}