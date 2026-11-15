import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { NotificationService } from '../services/notification.service';

/** localStorage key where the JWT access token is stored. */
const TOKEN_KEY = 'access_token';

/**
 * Hebrew error messages per HTTP status code (Requirement 9.4).
 */
const ERROR_MESSAGES: Record<number, string> = {
  403: 'אין הרשאה לביצוע פעולה זו',
  404: 'הפריט המבוקש לא נמצא',
  503: 'שירות ה-AI אינו זמין כרגע. אנא נסה שנית',
};

const DEFAULT_ERROR_MESSAGE = 'אירעה שגיאה. אנא פנה למנהל המערכת';

/**
 * Functional interceptor that handles HTTP error responses globally:
 *
 * - 401: clears the stored token and redirects to /controls (unauthenticated)
 * - 403: "אין הרשאה לביצוע פעולה זו"
 * - 404: "הפריט המבוקש לא נמצא"
 * - 503: "שירות ה-AI אינו זמין כרגע. אנא נסה שנית"
 * - 500 / other: "אירעה שגיאה. אנא פנה למנהל המערכת"
 *
 * The error is always rethrown so individual components can add local handling.
 *
 * Requirements: 9.4, 8.9
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notificationService = inject(NotificationService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (error.status === 401) {
          // Clear credentials and redirect to the main controls page
          localStorage.removeItem(TOKEN_KEY);
          router.navigate(['/controls']);
        } else {
          const message = ERROR_MESSAGES[error.status] ?? DEFAULT_ERROR_MESSAGE;
          notificationService.showError(message);
        }
      }

      // Always rethrow — components may need to react locally (e.g. 409 Conflict)
      return throwError(() => error);
    }),
  );
};
