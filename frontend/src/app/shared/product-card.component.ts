import { Component, computed, inject, input, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProductListItem } from '../models/catalog.models';
import { WatchArtComponent } from './watch-art.component';
import { FadeImageDirective } from '../directives/fade-image.directive';
import { ImgPipe } from '../pipes/img.pipe';
import { QuickBuyService } from '../services/quick-buy.service';

const TONES = ['black', 'navy', 'ivory', 'green'] as const;

@Component({
  selector: 'app-product-card',
  imports: [DecimalPipe, RouterLink, WatchArtComponent, FadeImageDirective, ImgPipe],
  template: `
    <div class="card">
    <a class="link" [routerLink]="['/ora', product().slug]" [attr.aria-label]="product().brand + ' ' + product().name">
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
    <div class="actions">
      @if (product().inStock) {
        <button type="button" class="buy" [disabled]="busy()" (click)="buyNow()">
          <svg viewBox="0 0 24 24" width="17" height="17" fill="currentColor" aria-hidden="true"><path d="M13.2 2 4.5 13.4c-.3.4 0 1 .5 1h5.1l-1.1 7.2c-.1.6.7.9 1 .4l8.6-11.5c.3-.4 0-1-.5-1h-5l1.1-6.9c.1-.6-.7-.9-1-.4Z"/></svg>
          Blej tani
        </button>
        <button type="button" class="cartbtn" [class.cartbtn--done]="added()" [disabled]="busy()" (click)="addToCart()" aria-label="Shto në shportë">
          @if (added()) {
            <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m5 12.5 4.5 4.5L19 7.5"/></svg>
          } @else {
            <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 4h2.2l2.3 11h10.3l2-8H6.3"/><circle cx="9" cy="19.5" r="1.4"/><circle cx="17" cy="19.5" r="1.4"/></svg>
          }
        </button>
      } @else {
        <button type="button" class="buy" disabled>Shitur</button>
      }
    </div>
    </div>
  `,
  styles: `
    .card { color: inherit; background: var(--paper); border: 1px solid var(--line); transition: box-shadow 0.5s var(--ease), transform 0.5s var(--ease), border-color 0.5s var(--ease); height: 100%; border-radius: var(--radius-lg); overflow: hidden; display: flex; flex-direction: column; }
    .link { display: block; text-decoration: none; color: inherit; flex: 1; }
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
    .info { padding: 1.2rem 1.4rem 1rem; text-align: center; display: flex; flex-direction: column; gap: 0.25rem; }
    .brand { font-size: 0.72rem; font-weight: 600; letter-spacing: 0.14em; text-transform: uppercase; color: var(--gold-dark); }
    h3 { font-size: 1.55rem; margin: 0.1rem 0 0; }
    .code { font-size: 0.74rem; color: var(--muted); letter-spacing: 0.08em; }
    .actions { display: flex; gap: 0.5rem; padding: 0 1.1rem 1.2rem; }
    .buy { flex: 1; display: inline-flex; align-items: center; justify-content: center; gap: 0.45rem; height: 44px; border: 1px solid var(--ink); border-radius: var(--radius-pill); background: var(--ink); color: #fff; font: inherit; font-size: 0.82rem; font-weight: 600; letter-spacing: 0.04em; cursor: pointer; transition: background 0.25s var(--ease), border-color 0.25s var(--ease), transform 0.25s var(--ease); }
    .buy svg { color: var(--gold-light); }
    .buy:hover:not(:disabled) { background: #2a2a2e; transform: translateY(-1px); }
    .buy:disabled { opacity: 0.5; cursor: not-allowed; }
    .cartbtn { flex: none; width: 44px; height: 44px; display: inline-flex; align-items: center; justify-content: center; border: 1px solid var(--line); border-radius: var(--radius-md); background: #fff; color: var(--ink); cursor: pointer; transition: border-color 0.25s var(--ease), background 0.25s var(--ease), color 0.25s var(--ease); }
    .cartbtn:hover:not(:disabled) { border-color: var(--ink); }
    .cartbtn--done, .cartbtn--done:hover:not(:disabled) { background: #2f6b45; border-color: #2f6b45; color: #fff; }
    .cartbtn:disabled { opacity: 0.55; cursor: wait; }
    @media (max-width: 520px) { .actions { padding: 0 0.7rem 0.9rem; gap: 0.4rem; } .buy { font-size: 0.76rem; gap: 0.3rem; } }
    .price { font-family: var(--font-display); font-variant-numeric: lining-nums; font-size: 1.35rem; font-weight: 500; margin-top: 0.7rem; }
  `
})
export class ProductCardComponent {
  product = input.required<ProductListItem>();
  readonly tone = computed(() => TONES[this.product().id % TONES.length]);

  private readonly quickBuy = inject(QuickBuyService);
  busy = signal(false);
  added = signal(false);

  addToCart(): void {
    this.busy.set(true);
    this.quickBuy.add(this.product().slug).subscribe(ok => {
      this.busy.set(false);
      if (ok) {
        this.added.set(true);
        setTimeout(() => this.added.set(false), 1600);
      }
    });
  }

  buyNow(): void {
    this.busy.set(true);
    this.quickBuy.add(this.product().slug, true).subscribe(() => this.busy.set(false));
  }
}
