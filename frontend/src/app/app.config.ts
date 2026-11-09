import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter, withInMemoryScrolling, withViewTransitions } from '@angular/router';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

import { routes } from './app.routes';
import { adminInterceptor } from './admin/admin.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withInMemoryScrolling({ anchorScrolling: 'enabled', scrollPositionRestoration: 'disabled' }),
      withViewTransitions({
        onViewTransitionCreated: ({ transition, from, to }) => {
          const path = (url: string) => url.split(/[?#]/)[0];
          if (path(from.toString()) === path(to.toString())) transition.skipTransition();
        }
      })),
    provideHttpClient(withFetch(), withInterceptors([adminInterceptor])),
    provideClientHydration(withEventReplay())
  ]
};
