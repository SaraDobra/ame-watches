import { Component, computed, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProductListItem } from '../models/catalog.models';
import { WatchArtComponent } from './watch-art.component';
import { FadeImageDirective } from '../directives/fade-image.directive';
import { ImgPipe } from '../pipes/img.pipe';

const TONES = ['black', 'navy', 'ivory', 'green'] as const;

@Component({
  selector: 'app-product-card',
  imports: [DecimalPipe, RouterLink, WatchArtComponent, FadeImageDirective, ImgPipe],
  template: `
    <a class="card" [routerLink]="['/ora', product().slug]" [attr.aria-label]="product().brand + ' ' + product().name">
      <div class="media" [class.media--photo]="!!product().imageUrl">
        @if (product().imageUrl) {
          <img appFade class="main" [src]="product().imageUrl | appImg" [alt]="product().brand + ' ' + product().name" width="480" height="600" loading="lazy" decoding="async">
          @if (product().secondImageUrl) {
            <img appFade class="alt" [src]="product().secondImageUrl | appImg" alt="" width="480" height="600" loading="lazy" decoding="async" aria-hidden="true">
          }
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
        <span class="view">Shiko detajet <span aria-hidden="true">→</span></span>
      </div>
    </a>
  `,
  styles: `
    .card { display: block; text-decoration: none; color: inherit; background: var(--paper); border: 1px solid var(--line); transition: box-shadow 0.5s var(--ease), transform 0.5s var(--ease), border-color 0.5s var(--ease); height: 100%; border-radius: var(--radius-lg); overflow: hidden; display: flex; flex-direction: column; }
    .card:hover { box-shadow: var(--shadow-lift); transform: translateY(-5px); border-color: #d9cfb8; }
    .media { position: relative; background: radial-gradient(circle at 50% 40%, #fbf9f4, #efe9dc); padding: 1.6rem 3.2rem 1rem; }
    .media--photo { background: #fff; padding: 1rem; overflow: hidden; }
    .media img.main { width: 100%; height: auto; aspect-ratio: 4 / 5; object-fit: contain; transition: transform 0.9s var(--ease), opacity 0.5s var(--ease); }
    .media img.alt { position: absolute; left: 1rem; top: 1rem; width: calc(100% - 2rem); height: calc(100% - 2rem); object-fit: contain; background: #fff; opacity: 0; transition: opacity 0.6s var(--ease), transform 0.9s var(--ease); }
    @media (hover: hover) {
      .card:hover .media img.main { transform: scale(1.05); }
      .card:hover .media img.alt { opacity: 1; transform: scale(1.05); }
      .card:hover .view { opacity: 1; transform: none; }
    }
    .view { font-size: 0.7rem; letter-spacing: 0.18em; text-transform: uppercase; color: var(--gold-dark); margin-top: 0.5rem; opacity: 0; transform: translateY(6px); transition: opacity 0.4s var(--ease), transform 0.4s var(--ease); }
    @media (hover: none) { .view { opacity: 1; transform: none; } }
    .tag { position: absolute; top: 0.9rem; left: 0.9rem; background: var(--ink); color: #fff; font-size: 0.62rem; letter-spacing: 0.12em; text-transform: uppercase; padding: 0.32rem 0.75rem; border-radius: var(--radius-pill); }
    .info { padding: 1.2rem 1.4rem 1.6rem; text-align: center; display: flex; flex-direction: column; gap: 0.25rem; }
    .brand { font-size: 0.72rem; font-weight: 600; letter-spacing: 0.14em; text-transform: uppercase; color: var(--gold-dark); }
    h3 { font-size: 1.55rem; margin: 0.1rem 0 0; }
    .code { font-size: 0.74rem; color: var(--muted); letter-spacing: 0.08em; }
    .price { font-family: var(--font-display); font-variant-numeric: lining-nums; font-size: 1.35rem; font-weight: 500; margin-top: 0.7rem; }
  `
})
export class ProductCardComponent {
  product = input.required<ProductListItem>();
  readonly tone = computed(() => TONES[this.product().id % TONES.length]);
}
