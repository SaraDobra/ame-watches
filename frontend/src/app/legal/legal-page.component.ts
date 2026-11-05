import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { SeoService } from '../services/seo.service';
import { LEGAL_PAGES } from './legal-pages';

@Component({
  selector: 'app-legal-page',
  template: `
    <article class="container legal">
      <h1>{{ page.title }}</h1>
      <p class="upd">Përditësuar: {{ page.updated }}</p>
      @for (s of page.sections; track s.h) {
        <h2>{{ s.h }}</h2>
        @for (p of s.p; track p) { <p>{{ p }}</p> }
        @if (s.list) {
          <ul>@for (i of s.list; track i) { <li>{{ i }}</li> }</ul>
        }
      }
    </article>
  `,
  styles: `
    .legal { max-width: 760px; padding-top: 3rem; padding-bottom: 5rem; line-height: 1.75; }
    h1 { font-size: clamp(1.9rem, 4vw, 2.6rem); margin: 0 0 0.4rem; }
    .upd { color: var(--muted); font-size: 0.85rem; margin: 0 0 2rem; }
    h2 { font-size: 1.2rem; margin: 2rem 0 0.5rem; }
    p { margin: 0 0 0.8rem; }
    ul { margin: 0 0 0.8rem; padding-left: 1.3rem; }
    li { margin-bottom: 0.3rem; }
  `
})
export class LegalPageComponent {
  private readonly slug: string = inject(ActivatedRoute).snapshot.data['slug'];
  readonly page = LEGAL_PAGES[this.slug];

  constructor() {
    inject(Title).setTitle(`${this.page.title} | amé watches`);
    const meta = inject(Meta);
    meta.updateTag({ name: 'description', content: this.page.description });
    meta.updateTag({ name: 'robots', content: 'index,follow' });
    inject(SeoService).setCanonical(`/${this.slug}`);
  }
}
