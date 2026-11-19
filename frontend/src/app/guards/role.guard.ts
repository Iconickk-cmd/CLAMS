import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isLoggedIn()) {
    return router.createUrlTree(['/login']);
  }

  const currentUser = authService.getCurrentUser();
  const allowedRoles = route.data?.['roles'] as string[] | undefined;

  if (!allowedRoles || allowedRoles.length === 0) {
    return true;
  }

  const userRoles = currentUser?.roles ?? [];

  const hasAccess = allowedRoles.some(role =>
    userRoles.includes(role)
  );

  if (hasAccess) {
    return true;
  }

  return router.createUrlTree(['/access-denied']);
};
