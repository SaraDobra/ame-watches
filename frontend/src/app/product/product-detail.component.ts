import { Component, inject, signal } from '@angular/core';
import { DOCUMENT, DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { SeoService } from '../services/seo.service';
import { catchError, of, switchMap, tap } from 'rxjs';
import { CatalogService } from '../services/catalog.service';
import { ProductDetail } from '../models/catalog.models';
import { imageSrc } from '../util/image';
import { genderLabel, typeLabel } from '../util/labels';
import { WatchArtComponent } from '../shared/watch-art.component';
import { SSR_RESPONSE } from '../util/ssr-response';
import { CartService } from '../services/cart.service';
import { ToastService } from '../services/toast.service';
import { FadeImageDirective } from '../directives/fade-image.directive';
import { ImgPipe } from '../pipes/img.pipe';

interface Spec {
  label: string;
  value: string;
}

@Component({
  selector: 'app-product-detail',
  imports: [DecimalPipe, RouterLink, WatchArtComponent, FadeImageDirective, ImgPipe],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.scss'
})
export class ProductDetailComponent {
  private readonly catalog = inject(CatalogService);
  private readonly route = inject(ActivatedRoute);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly seo = inject(SeoService);
  private readonly document = inject(DOCUMENT);
  private readonly response = inject(SSR_RESPONSE, { optional: true });
  readonly cart = inject(CartService);
  private readonly toast = inject(ToastService);
  added = signal(false);
  origin = signal('50% 50%');

  product = signal<ProductDetail | null>(null);
  loading = signal(true);
  notFound = signal(false);
  selected = signal(0);
  message = signal('');

  readonly trust = [
    'Garanci 1 vit nga amé watches',
    'Dërgesë falas në gjithë vendin',
    'Orë origjinale',
    'Pagesë në dorëzim ose me kartë'
  ];

  constructor() {
    this.route.paramMap.pipe(
      tap(() => { this.loading.set(true); this.notFound.set(false); this.selected.set(0); this.message.set(''); }),
      switchMap(params => this.catalog.getProduct(params.get('slug') ?? '').pipe(
        catchError(() => of(null))
      )),
      takeUntilDestroyed()
    ).subscribe(product => {
      this.product.set(product);
      this.notFound.set(product === null);
      if (product === null) this.response?.status(404);
      this.loading.set(false);
      this.updateHead(product);
    });
  }

  specs(): Spec[] {
    const p = this.product();
    if (!p) return [];

    const specs: Spec[] = [
      { label: 'Brendi', value: p.brand },
      { label: 'Kodi i modelit', value: p.modelCode },
      { label: 'Për', value: genderLabel(p.gender) },
      { label: 'Lloji', value: typeLabel(p.type) }
    ];
    if (p.movement) specs.push({ label: 'Mekanizmi', value: p.movement });
    if (p.caseSizeMm) specs.push({ label: 'Diametri i kasës', value: `${p.caseSizeMm} mm` });
    if (p.waterResistanceM) specs.push({ label: 'Rezistenca ndaj ujit', value: `${p.waterResistanceM} m` });
    return specs;
  }

  addToCart(): void {
    const product = this.product();
    if (!product) return;

    const result = this.cart.add(product);
    this.message.set(result === 'max' ? 'Keni shtuar sasinë maksimale të disponueshme për këtë orë.' : '');

    if (result === 'added') {
      this.toast.show(`${product.brand} ${product.name} u shtua në shportë`, { label: 'Shiko shportën', url: '/shporta' });
      this.added.set(true);
      setTimeout(() => this.added.set(false), 1800);
    }
  }

  zoom(event: MouseEvent): void {
    const box = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const x = ((event.clientX - box.left) / box.width) * 100;
    const y = ((event.clientY - box.top) / box.height) * 100;
    this.origin.set(`${x.toFixed(1)}% ${y.toFixed(1)}%`);
  }

  inCart(): number {
    const product = this.product();
    return product ? this.cart.quantityOf(product.id) : 0;
  }

  stockText(): string {
    const p = this.product();
    if (!p || !p.inStock) return 'Shitur';
    return p.stock <= 3 ? `Vetëm ${p.stock} copë në stok` : 'Në stok';
  }

  private updateHead(product: ProductDetail | null): void {
    if (!product) {
      this.title.setTitle('Ora nuk u gjet | amé watches');
      this.meta.updateTag({ name: 'robots', content: 'noindex' });
      return;
    }

    const heading = `${product.brand} ${product.name} ${product.modelCode}`;
    const description = product.description ?? `${heading} me garanci 1 vit nga amé watches dhe dërgesë falas.`;
    this.title.setTitle(`${heading} | amé watches`);
    this.meta.updateTag({ name: 'description', content: description });
    this.meta.updateTag({ name: 'robots', content: 'index,follow' });
    this.meta.updateTag({ property: 'og:title', content: heading });
    this.meta.updateTag({ property: 'og:description', content: description });
    this.seo.setCanonical(`/ora/${product.slug}`);
    const origin = this.document.location?.origin ?? '';
    const first = imageSrc(product.images[0]);
    this.seo.setImage(first ? (first.startsWith('http') ? first : origin + first) : null);

    const existing = this.document.getElementById('product-jsonld');
    existing?.remove();
    const script = this.document.createElement('script');
    script.id = 'product-jsonld';
    script.type = 'application/ld+json';
    script.text = JSON.stringify({
      '@context': 'https://schema.org',
      '@type': 'Product',
      name: heading,
      sku: product.modelCode,
      brand: { '@type': 'Brand', name: product.brand },
      description,
      offers: {
        '@type': 'Offer',
        priceCurrency: 'EUR',
        price: product.price,
        availability: product.inStock ? 'https://schema.org/InStock' : 'https://schema.org/OutOfStock'
      }
    });
    this.document.head.appendChild(script);
  }
}
