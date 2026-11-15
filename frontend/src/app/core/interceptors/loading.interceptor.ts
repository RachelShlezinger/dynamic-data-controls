import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';

import { NotificationService } from '../services/notification.service';

/**
 * Delay before the loading indicator becomes visible (ms).
 * Requests that complete faster than this threshold will not show a spinner,
 * preventing flicker for fast responses.
 */
const LOADING_DELAY_MS = 300;

/**
 * Module-level counter that tracks the number of in-flight requests.
 * Using a plain counter (instead of a signal) is intentional: we only
 * want to toggle the signal once per batch start/end, not on every tick.
 */
let activeRequests = 0;

/**
 * Functional interceptor that drives `NotificationService.isLoading`:
 *
 * - Increments the in-flight counter when a request starts.
 * - After {@link LOADING_DELAY_MS} ms, sets `isLoading = true` only if the
 *   request is still in flight (prevents flicker for fast responses).
 * - Decrements the counter on completion or error; sets `isLoading = false`
 *   once all concurrent requests have finished.
 *
 * Requirements: 9.3 (spinner), 9.4 (loading state reflects in-flight HTTP)
 */
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const notificationService = inject(NotificationService);

  activeRequests++;

  // Schedule the loading indicator; cancel if the request finishes first
  const showTimer = setTimeout(() => {
    if (activeRequests > 0) {
      notificationService.isLoading.set(true);
    }
  }, LOADING_DELAY_MS);

  return next(req).pipe(
    finalize(() => {
      clearTimeout(showTimer);
      activeRequests = Math.max(0, activeRequests - 1);

      if (activeRequests === 0) {
        notificationService.isLoading.set(false);
      }
    }),
  );
};
