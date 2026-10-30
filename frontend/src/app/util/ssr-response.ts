import { InjectionToken } from '@angular/core';

export interface ServerResponse {
  status(code: number): unknown;
}

export const SSR_RESPONSE = new InjectionToken<ServerResponse>('SSR_RESPONSE');
