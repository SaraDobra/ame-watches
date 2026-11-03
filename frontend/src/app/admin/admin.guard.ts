import { PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { AdminAuthService } from './admin-auth.service';

export const adminGuard: CanActivateFn = () => {
  if (!isPlatformBrowser(inject(PLATFORM_ID))) return true;

  const auth = inject(AdminAuthService);
  return auth.isLoggedIn() || inject(Router).createUrlTree(['/admin/login']);
};
