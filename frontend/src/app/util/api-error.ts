import { HttpErrorResponse } from '@angular/common/http';

export function apiErrors(err: unknown): string[] {
  if (!(err instanceof HttpErrorResponse)) {
    return ['Ndodhi një gabim i papritur. Provoni përsëri.'];
  }
  if (err.status === 0) {
    return ['Serveri nuk përgjigjet. Kontrolloni lidhjen dhe provoni përsëri.'];
  }
  if (err.status === 429) {
    return ['Shumë përpjekje në pak kohë. Prisni një minutë dhe provoni përsëri.'];
  }

  const body = err.error;
  if (body?.errors && typeof body.errors === 'object') {
    return Object.values(body.errors).flatMap(messages => messages as string[]);
  }
  if (typeof body?.message === 'string') {
    return [body.message];
  }
  return [`Gabim ${err.status}. Provoni përsëri.`];
}
