import { environment } from '../../environments/environment';

const apiOrigin = environment.apiUrl.replace(/\/api\/?$/, '');

export function imageSrc(url: string | null | undefined): string {
  if (!url) return '';
  return url.startsWith('/uploads/') ? `${apiOrigin}${url}` : url;
}
