import { Injectable, inject, signal } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

/** Default duration for success toasts (ms). Error toasts stay until closed. */
const SUCCESS_DURATION_MS = 4000;

/** Shared snack-bar base config (RTL direction is set globally in app.config.ts). */
const BASE_CONFIG: MatSnackBarConfig = {
  horizontalPosition: 'center',
  verticalPosition: 'bottom',
};

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  /**
   * Reflects whether any async operation is in progress.
   * Set to `true` by the loading interceptor; reset to `false` when all
   * in-flight requests complete.
   */
  readonly isLoading = signal<boolean>(false);

  /** Display a dismissible success toast at the bottom of the screen. */
  showSuccess(message: string): void {
    this.snackBar.open(message, 'סגור', {
      ...BASE_CONFIG,
      duration: SUCCESS_DURATION_MS,
      panelClass: ['snackbar-success'],
    });
  }

  /**
   * Display a persistent error toast that stays until the user clicks "סגור".
   * Per requirement 9.4, error messages must remain until manually dismissed.
   */
  showError(message: string): void {
    this.snackBar.open(message, 'סגור', {
      ...BASE_CONFIG,
      duration: 0, // persist until dismissed
      panelClass: ['snackbar-error'],
    });
  }
}
