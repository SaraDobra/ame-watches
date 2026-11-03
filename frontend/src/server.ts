import { APP_BASE_HREF } from '@angular/common';
import { CommonEngine, isMainModule } from '@angular/ssr/node';
import express from 'express';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import bootstrap from './main.server';
import { SSR_RESPONSE } from './app/util/ssr-response';
import { environment } from './environments/environment';

const serverDistFolder = dirname(fileURLToPath(import.meta.url));
const browserDistFolder = resolve(serverDistFolder, '../browser');
const indexHtml = join(serverDistFolder, 'index.server.html');

const app = express();
const allowedHosts = (process.env['ALLOWED_HOSTS'] ?? 'localhost').split(',').map((host) => host.trim()).filter(Boolean);
const commonEngine = new CommonEngine({ allowedHosts });
const apiBase = process.env['API_URL'] ?? environment.apiUrl;

/**
 * Example Express Rest API endpoints can be defined here.
 * Uncomment and define endpoints as necessary.
 *
 * Example:
 * ```ts
 * app.get('/api/**', (req, res) => {
 *   // Handle API request
 * });
 * ```
 */

app.use((_req, res, next) => {
  res.setHeader('X-Content-Type-Options', 'nosniff');
  res.setHeader('X-Frame-Options', 'DENY');
  res.setHeader('Referrer-Policy', 'strict-origin-when-cross-origin');
  next();
});

/**
 * robots.txt dhe sitemap.xml: domeni merret nga kërkesa, pra nuk ka adresë të shkruar me dorë.
 */
app.get('/robots.txt', (req, res) => {
  const origin = `${req.protocol}://${req.headers.host}`;
  res.type('text/plain').send(
    `User-agent: *\nDisallow: /admin\nDisallow: /porosia\nDisallow: /shporta\nDisallow: /pagesa\n\nSitemap: ${origin}/sitemap.xml\n`);
});

app.get('/sitemap.xml', async (req, res) => {
  const origin = `${req.protocol}://${req.headers.host}`;
  try {
    const response = await fetch(`${apiBase}/products?pageSize=48`);
    if (!response.ok) throw new Error(String(response.status));
    const { items } = (await response.json()) as { items: { slug: string }[] };
    const urls = ['/', '/ora', ...items.map((p) => `/ora/${encodeURIComponent(p.slug)}`)];
    res.type('application/xml').send(
      '<?xml version="1.0" encoding="UTF-8"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">' +
      urls.map((u) => `<url><loc>${origin}${u}</loc></url>`).join('') + '</urlset>');
  } catch {
    res.status(503).send('Sitemap nuk është i gatshëm');
  }
});

/**
 * Serve static files from /browser
 */
app.get(
  '**',
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: 'index.html'
  }),
);

/**
 * Handle all other requests by rendering the Angular application.
 */
app.get('**', (req, res, next) => {
  const { protocol, originalUrl, baseUrl, headers } = req;

  commonEngine
    .render({
      bootstrap,
      documentFilePath: indexHtml,
      url: `${protocol}://${headers.host}${originalUrl}`,
      publicPath: browserDistFolder,
      providers: [
        { provide: APP_BASE_HREF, useValue: baseUrl },
        { provide: SSR_RESPONSE, useValue: res },
      ],
    })
    .then((html) => res.send(html))
    .catch((err) => next(err));
});

/**
 * Start the server if this module is the main entry point.
 * The server listens on the port defined by the `PORT` environment variable, or defaults to 4000.
 */
if (isMainModule(import.meta.url)) {
  const port = process.env['PORT'] || 4000;
  app.listen(port, () => {
    console.log(`Node Express server listening on http://localhost:${port}`);
  });
}

export default app;
