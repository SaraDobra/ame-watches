import { Component, computed, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProductListItem } from '../models/catalog.models';
import { WatchArtComponent } from './watch-art.component';

const TONES = ['black', 'navy', 'ivory', 'green'] as const;

@Component({
  selector: 'app-product-card',
  imports: [DecimalPipe, RouterLink, WatchArtComponent],
  template: `
    <a class="card" [routerLink]="['/ora', product().slug]" [attr.aria-label]="product().brand + ' ' + product().name">
      <div class="media" [class.media--photo]="!!product().imageUrl">
        @if (product().imageUrl) {
          <img [src]="product().imageUrl" [alt]="product().brand + ' ' + product().name" loading="lazy">
        } @else {
          <app-watch-art [tone]="tone()" [label]="product().brand + ' ' + product().name" />
        }
        @if (!product().inStock) {
          <span class="tag">Shitur</span>
        }
      </div>
      <div class="info">
        <span class="brand">{{ product().brand }}</span>
        <h3>{{ product().name }}</h3>
        <span class="code">{{ product().modelCode }}</span>
        <span class="price">{{ product().price | number: '1.0-2' }} €</span>
      </div>
    </a>
  `,
  styles: `
    .card { display: block; text-decoration: none; color: inherit; background: var(--paper); border: 1px solid var(--line); transition: box-shadow 0.35s, transform 0.35s; height: 100%; display: flex; flex-direction: column; }
    .card:hover { box-shadow: 0 22px 44px -22px rgba(12, 12, 13, 0.35); transform: translateY(-4px); }
    .media { position: relative; background: radial-gradient(circle at 50% 40%, #fbf9f4, #efe9dc); padding: 1.6rem 3.2rem 1rem; }
    .media--photo { background: #fff; padding: 1rem; }
    .media img { width: 100%; aspect-ratio: 4 / 5; object-fit: contain; }
    .tag { position: absolute; top: 0.9rem; left: 0.9rem; background: var(--ink); color: #fff; font-size: 0.62rem; letter-spacing: 0.18em; text-transform: uppercase; padding: 0.3rem 0.6rem; }
    .info { padding: 1.2rem 1.4rem 1.6rem; text-align: center; display: flex; flex-direction: column; gap: 0.25rem; }
    .brand { font-size: 0.66rem; letter-spacing: 0.26em; text-transform: uppercase; color: var(--gold-dark); }
    h3 { font-size: 1.55rem; margin: 0.1rem 0 0; }
    .code { font-size: 0.74rem; color: var(--muted); letter-spacing: 0.08em; }
    .price { font-family: var(--font-display); font-variant-numeric: lining-nums; font-size: 1.35rem; font-weight: 500; margin-top: 0.7rem; }
  `
})
export class ProductCardComponent {
  product = input.required<ProductListItem>();
  readonly tone = computed(() => TONES[this.product().id % TONES.length]);
}
