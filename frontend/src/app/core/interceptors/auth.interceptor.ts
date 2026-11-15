import { HttpInterceptorFn } from '@angular/common/http';

import { environment } from '../../../environments/environment';

/** localStorage key where the JWT access token is stored. */
const TOKEN_KEY = 'access_token';

/**
 * URL segments that should NOT receive the Authorization header:
 * - /api/auth/*  — login / refresh endpoints (no token yet)
 * - Any URL not starting with the configured API base (e.g. direct Python AI service calls)
 */
const AUTH_SKIP_PATTERNS: RegExp[] = [/\/api\/auth\//i];

function shouldSkip(url: string): boolean {
  // Always skip requests that go outside the main API (e.g. direct AI-service calls)
  if (!url.startsWith(environment.apiUrl)) {
    return true;
  }
  return AUTH_SKIP_PATTERNS.some(pattern => pattern.test(url));
}

/**
 * Functional interceptor that attaches the JWT Bearer token to every outgoing
 * request that targets the backend API.
 *
 * Requirements: 8.9
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (shouldSkip(req.url)) {
    return next(req);
  }

  const token = localStorage.getItem(TOKEN_KEY);
  if (!token) {
    return next(req);
  }

  const authReq = req.clone({
    setHeaders: { Authorization: `Bearer ${token}` },
  });

  return next(authReq);
};
