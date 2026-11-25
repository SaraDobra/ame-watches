import { Component, HostListener, effect, inject, signal } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LogoComponent } from './logo.component';
import { CartService } from '../services/cart.service';

@Component({
  selector: 'app-header',
  imports: [RouterLink, LogoComponent],
  template: `
    <div class="topbar">Dërgesë falas<span class="more"> në gjithë vendin</span> &nbsp;·&nbsp; Garanci 1 vit<span class="more"> nga amé watches</span></div>
    <header [class.header--small]="scrolled()">
      <div class="container bar">
        <button type="button" class="burger" (click)="open.set(true)" aria-label="Hap menunë" [attr.aria-expanded]="open()">
          <svg viewBox="0 0 24 24" width="26" height="26" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" aria-hidden="true"><path d="M4 7h16M4 12h16M4 17h16"/></svg>
        </button>
        <nav class="left">
          <a routerLink="/ora">Koleksioni</a>
          <a routerLink="/" fragment="brendet">Brendet</a>
        </nav>
        <a routerLink="/" class="brand" aria-label="amé watches, ballina">
          <app-logo [size]="scrolled() ? 19 : 24" />
        </a>
        <a routerLink="/shporta" class="cart-m" aria-label="Shporta">
          <svg viewBox="0 0 24 24" width="25" height="25" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 4h2.2l2.3 11h10.3l2-8H6.3"/><circle cx="9" cy="19.5" r="1.4"/><circle cx="17" cy="19.5" r="1.4"/></svg>
          @if (cart.count() > 0) { <span class="badge">{{ cart.count() }}</span> }
        </a>
        <nav class="right">
          <a routerLink="/" fragment="pse-ame">Pse amé</a>
          <a routerLink="/" fragment="kontakti">Kontakti</a>
          <a routerLink="/shporta" class="cart" aria-label="Shporta">
            Shporta
            @for (count of [cart.count()]; track count) {
              @if (count > 0) {
                <span class="badge">{{ count }}</span>
              }
            }
          </a>
        </nav>
      </div>
    </header>
    @if (open()) {
      <div class="scrim" (click)="close()"></div>
      <aside class="drawer" role="dialog" aria-modal="true" aria-label="Menuja">
        <div class="drawer-head">
          <app-logo [size]="21" />
          <button type="button" class="x" (click)="close()" aria-label="Mbyll menunë">
            <svg viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" aria-hidden="true"><path d="M6 6l12 12M18 6 6 18"/></svg>
          </button>
        </div>
        <nav class="drawer-links" (click)="close()">
          <a routerLink="/">Ballina</a>
          <a routerLink="/ora">Koleksioni</a>
          <a routerLink="/" fragment="brendet">Brendet</a>
          <a routerLink="/" fragment="pse-ame">Pse amé</a>
          <a routerLink="/" fragment="kontakti">Kontakti</a>
          <a routerLink="/shporta">Shporta @if (cart.count() > 0) { <span class="badge">{{ cart.count() }}</span> }</a>
        </nav>
      </aside>
    }
  `,
  styles: `
    .topbar { background: var(--ink); color: var(--gold-light); text-align: center; font-size: 0.7rem; font-weight: 500; letter-spacing: 0.16em; text-transform: uppercase; padding: 0.6rem 1rem; }
    header { position: sticky; top: 0; z-index: 20; background: rgba(247, 244, 238, 0.94); backdrop-filter: blur(10px); border-bottom: 1px solid var(--line); transition: box-shadow 0.35s var(--ease); }
    .header--small { box-shadow: 0 10px 30px -20px rgba(12, 12, 13, 0.45); }
    .header--small .bar { padding-top: 0.55rem; padding-bottom: 0.55rem; }
    .bar { display: grid; grid-template-columns: 1fr auto 1fr; align-items: center; padding-top: 1rem; padding-bottom: 1rem; transition: padding 0.35s var(--ease); }
    nav { display: flex; align-items: center; gap: 2rem; }
    .right { justify-content: flex-end; }
    nav a { display: inline-flex; align-items: center; font-size: 0.74rem; font-weight: 600; letter-spacing: 0.14em; text-transform: uppercase; text-decoration: none; color: var(--ink); position: relative; padding: 0.3rem 0; }
    nav a::after { content: ''; position: absolute; left: 0; right: 0; bottom: 0; height: 1px; background: var(--gold); transform: scaleX(0); transform-origin: left; transition: transform 0.3s; }
    nav a:hover::after { transform: scaleX(1); }
    .brand { text-decoration: none; }
    .cart { display: inline-flex; align-items: center; gap: 0.45rem; }
    .badge { display: inline-flex; align-items: center; justify-content: center; min-width: 1.35rem; height: 1.35rem; padding: 0 0.3rem; background: var(--ink); color: #fff; border-radius: 999px; font-size: 0.66rem; letter-spacing: 0; animation: pop 0.5s var(--ease); }
    @keyframes pop { 0% { transform: scale(0.4); } 60% { transform: scale(1.3); } 100% { transform: scale(1); } }
    .burger, .cart-m { display: none; }
    .burger { background: none; border: 0; padding: 0.3rem; margin-left: -0.3rem; color: var(--ink); cursor: pointer; justify-self: start; }
    .cart-m { position: relative; color: var(--ink); justify-self: end; padding: 0.3rem; margin-right: -0.3rem; }
    .cart-m .badge { position: absolute; top: -0.15rem; right: -0.3rem; }
    .scrim { position: fixed; inset: 0; z-index: 90; background: rgba(12, 12, 13, 0.5); animation: fadein 0.25s ease both; }
    .drawer { position: fixed; top: 0; bottom: 0; left: 0; z-index: 100; width: min(320px, 86vw); background: var(--paper, #f7f4ee); box-shadow: 20px 0 50px -20px rgba(0, 0, 0, 0.45); display: flex; flex-direction: column; animation: slidein 0.3s var(--ease) both; padding-bottom: env(safe-area-inset-bottom); }
    .drawer-head { display: flex; align-items: center; justify-content: space-between; padding: 1.1rem 1.3rem; border-bottom: 1px solid var(--line); }
    .x { background: none; border: 0; padding: 0.3rem; color: var(--ink); cursor: pointer; }
    .drawer-links { flex-direction: column; align-items: stretch; gap: 0; padding: 0.6rem 0; overflow-y: auto; }
    .drawer-links a { font-size: 0.92rem; letter-spacing: 0.14em; padding: 1.05rem 1.4rem; border-bottom: 1px solid var(--line); gap: 0.6rem; }
    .drawer-links a::after { display: none; }
    @keyframes slidein { from { transform: translateX(-100%); } to { transform: none; } }
    @keyframes fadein { from { opacity: 0; } to { opacity: 1; } }
    @media (max-width: 820px) {
      .more { display: none; }
      .topbar { padding: 0.45rem 1rem; letter-spacing: 0.14em; font-size: 0.62rem; }
      .bar { padding-top: 0.55rem; padding-bottom: 0.55rem; }
      .header--small .bar { padding-top: 0.4rem; padding-bottom: 0.4rem; }
      .bar > .left, .bar > .right { display: none; }
      .burger, .cart-m { display: inline-flex; align-items: center; }
    }
  `
})
export class HeaderComponent {
  readonly cart = inject(CartService);
  scrolled = signal(false);
  open = signal(false);
  private readonly doc = inject(DOCUMENT);

  constructor() {
    // kur menuja është e hapur, faqja pas saj nuk lëviz
    effect(() => { this.doc.body.style.overflow = this.open() ? 'hidden' : ''; });
  }

  close(): void { this.open.set(false); }

  @HostListener('document:keydown.escape')
  onEscape(): void { this.close(); }

  @HostListener('window:scroll')
  onScroll(): void {
    const next = window.scrollY > 40;
    if (next !== this.scrolled()) this.scrolled.set(next);
  }
}
