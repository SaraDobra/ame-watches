import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AdminAuthService } from './admin-auth.service';

export const adminInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(`${environment.apiUrl}/admin`)) {
    return next(req);
  }

  const auth = inject(AdminAuthService);
  const router = inject(Router);
  const token = auth.token();
  const request = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(request).pipe(
    catchError(error => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        auth.logout();
        router.navigate(['/admin/login'], { queryParams: { expired: 1 } });
      }
      return throwError(() => error);
    })
  );
};
