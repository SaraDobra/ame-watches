import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Meta } from '@angular/platform-browser';

@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly meta = inject(Meta);
  private readonly doc = inject(DOCUMENT);

  /** Adresa zyrtare e faqes; filtrat në URL nuk llogariten si faqe të reja. */
  setCanonical(path: string): void {
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

  setImage(url: string | null): void {
    if (url) this.meta.updateTag({ property: 'og:image', content: url });
  }
}
