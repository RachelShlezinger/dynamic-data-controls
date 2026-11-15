import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

/** Role claim value that grants admin access. */
const ADMIN_ROLE = 'מנהל_מערכת';

/**
 * Decodes the payload section of a JWT stored in localStorage.
 * Returns null if the token is absent or malformed.
 */
function decodeJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    // Base64-url → standard Base64 → decode
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    // Pad to a multiple of 4
    const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
    const json = atob(padded);
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

/**
 * Functional guard: allows access only when the JWT `role` claim equals
 * 'מנהל_מערכת'. Redirects to /controls and returns false otherwise.
 */
export const adminGuard: CanActivateFn = () => {
  const router = inject(Router);

  const token = localStorage.getItem('token');
  if (!token) {
    router.navigate(['/controls']);
    return false;
  }

  const payload = decodeJwtPayload(token);
  if (payload?.['role'] === ADMIN_ROLE) {
    return true;
  }

  router.navigate(['/controls']);
  return false;
};
