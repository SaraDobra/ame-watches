import { mergeApplicationConfig, ApplicationConfig } from '@angular/core';
import { provideServerRendering } from '@angular/platform-server';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { appConfig } from './app.config';
import { adminInterceptor } from './admin/admin.interceptor';
import { ssrApiInterceptor } from './util/ssr-api';

const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(),
    provideHttpClient(withFetch(), withInterceptors([adminInterceptor, ssrApiInterceptor])),
  ]
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
