import { HttpInterceptorFn } from '@angular/common/http';
import { InjectionToken, inject } from '@angular/core';

/** Adresa e brendshme e API-t për SSR (p.sh. http://127.0.0.1:5260/api); nuk përdoret në shfletues. */
export const API_INTERNAL_URL = new InjectionToken<string | undefined>('API_INTERNAL_URL');

/** Në server, kërkesat drejt /api shkojnë direkt te API-ja, jo përmes internetit. */
export const ssrApiInterceptor: HttpInterceptorFn = (req, next) => {
  const internal = inject(API_INTERNAL_URL, { optional: true });
  if (internal && req.url.startsWith('/api/')) {
    return next(req.clone({ url: internal + req.url.slice('/api'.length) }));
  }
  return next(req);
};
