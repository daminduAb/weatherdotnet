import { ApplicationConfig } from '@angular/core';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { AuthModule, AuthHttpInterceptor } from '@auth0/auth0-angular';
import { importProvidersFrom } from '@angular/core';
import { routes } from './app.routes';

// npm install @auth0/auth0-angular

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    importProvidersFrom(
      AuthModule.forRoot({
        domain: 'dev-0jwn062hddni5evr.us.auth0.com',
        clientId: 'IgfNDjkMwScjpdSCnePHDr6Bwjm2UnRK',
        authorizationParams: {
          redirect_uri: window.location.origin,
          audience: 'https://fidenz-comfort-index-api' // must match .NET Auth0:Audience
        },
        // Only calls to our own API get the access token attached —
        // avoids leaking the token to third-party requests.
        httpInterceptor: {
          allowedList: ['/api/*']
        }
      })
    ),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthHttpInterceptor,
      multi: true
    }
  ]
};
