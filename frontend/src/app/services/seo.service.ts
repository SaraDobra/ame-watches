import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Meta } from '@angular/platform-browser';

@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly meta = inject(Meta);
  private readonly doc = inject(DOCUMENT);

  /** Adresa zyrtare e faqes; filtrat në URL nuk llogariten si faqe të reja. */
  setCanonical(path: string): void {
    this.clearProduct();
    const origin = this.doc.location?.origin ?? '';
    // Gjatë paraprodhimit (build) nuk ka domen të vërtetë; vendoset më vonë në shfletues.
    if (!origin || origin.includes('ng-localhost')) return;
    let link = this.doc.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!link) {
      link = this.doc.createElement('link');
      link.rel = 'canonical';
      this.doc.head.appendChild(link);
    }
    link.href = origin + path;
    this.meta.updateTag({ property: 'og:url', content: origin + path });
    this.meta.updateTag({ property: 'og:site_name', content: 'amé watches' });
    this.meta.updateTag({ property: 'og:locale', content: 'sq_AL' });
  }

  /** Tag-et e produktit (Meta/Facebook katalog): id, çmimi me valutë, disponueshmëria. */
  setProduct(p: { sku: string; price: number; inStock: boolean; brand: string }): void {
    const set = (property: string, content: string) => this.meta.updateTag({ property, content });
    set('og:type', 'product');
    set('product:retailer_item_id', p.sku);
    set('product:price:amount', p.price.toFixed(2));
    set('product:price:currency', 'EUR');
    set('product:availability', p.inStock ? 'in stock' : 'out of stock');
    set('product:condition', 'new');
    set('product:brand', p.brand);
  }

  /** Faqet që nuk janë produkt (SPA): hiq tag-et e produktit të faqes së mëparshme. */
  private clearProduct(): void {
    this.meta.removeTag("property^='product:'");
    this.meta.updateTag({ property: 'og:type', content: 'website' });
  }

  setImage(url: string | null): void {
    if (url) this.meta.updateTag({ property: 'og:image', content: url });
  }
}
