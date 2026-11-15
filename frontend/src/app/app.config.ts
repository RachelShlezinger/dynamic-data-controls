import { ApplicationConfig, LOCALE_ID } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { MAT_DATE_LOCALE } from '@angular/material/core';
import { MAT_DIALOG_DEFAULT_OPTIONS, MatDialogConfig } from '@angular/material/dialog';
import { MAT_SNACK_BAR_DEFAULT_OPTIONS } from '@angular/material/snack-bar';

import { routes } from './app.routes';
import {
  authInterceptor,
  errorInterceptor,
  loadingInterceptor,
} from './core/interceptors';

export const appConfig: ApplicationConfig = {
  providers: [
    // Router
    provideRouter(routes, withComponentInputBinding()),

    // Animations (required by Angular Material)
    provideAnimations(),

    // HTTP client with interceptor chain:
    //   1. authInterceptor  — attaches Bearer token
    //   2. errorInterceptor — translates HTTP errors to Hebrew toasts / redirects
    //   3. loadingInterceptor — drives the global loading spinner
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor, loadingInterceptor])),

    // Hebrew locale
    { provide: LOCALE_ID, useValue: 'he-IL' },
    { provide: MAT_DATE_LOCALE, useValue: 'he-IL' },

    // All Material dialogs default to RTL
    {
      provide: MAT_DIALOG_DEFAULT_OPTIONS,
      useValue: {
        direction: 'rtl',
        hasBackdrop: true,
        disableClose: false,
        autoFocus: 'first-tabbable',
      } satisfies MatDialogConfig,
    },

    // Snackbars: show at bottom-center, 4-second default
    {
      provide: MAT_SNACK_BAR_DEFAULT_OPTIONS,
      useValue: {
        duration: 4000,
        horizontalPosition: 'center',
        verticalPosition: 'bottom',
        direction: 'rtl',
      },
    },
  ],
};
