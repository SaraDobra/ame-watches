import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LogoComponent } from './logo.component';
import { CartService } from '../services/cart.service';

@Component({
  selector: 'app-header',
  imports: [RouterLink, LogoComponent],
  template: `
    <div class="topbar">Dërgesë falas në gjithë vendin &nbsp;·&nbsp; Garanci 1 vit nga amé watches</div>
    <header>
      <div class="container bar">
        <nav class="left">
          <a routerLink="/ora">Koleksioni</a>
          <a routerLink="/" fragment="brendet">Brendet</a>
        </nav>
        <a routerLink="/" class="brand" aria-label="amé watches, ballina">
          <app-logo [size]="24" />
        </a>
        <nav class="right">
          <a routerLink="/" fragment="pse-ame">Pse amé</a>
          <a routerLink="/" fragment="kontakti">Kontakti</a>
          <a routerLink="/shporta" class="cart" aria-label="Shporta">
            Shporta
            @if (cart.count() > 0) {
              <span class="badge">{{ cart.count() }}</span>
            }
          </a>
        </nav>
      </div>
    </header>
  `,
  styles: `
    .topbar { background: var(--ink); color: var(--gold-light); text-align: center; font-size: 0.7rem; letter-spacing: 0.22em; text-transform: uppercase; padding: 0.6rem 1rem; }
    header { position: sticky; top: 0; z-index: 20; background: rgba(247, 244, 238, 0.94); backdrop-filter: blur(10px); border-bottom: 1px solid var(--line); }
    .bar { display: grid; grid-template-columns: 1fr auto 1fr; align-items: center; padding-top: 1rem; padding-bottom: 1rem; }
    nav { display: flex; align-items: center; gap: 2rem; }
    .right { justify-content: flex-end; }
    nav a { display: inline-flex; align-items: center; font-size: 0.72rem; letter-spacing: 0.2em; text-transform: uppercase; text-decoration: none; color: var(--ink); position: relative; padding: 0.3rem 0; }
    nav a::after { content: ''; position: absolute; left: 0; right: 0; bottom: 0; height: 1px; background: var(--gold); transform: scaleX(0); transform-origin: left; transition: transform 0.3s; }
    nav a:hover::after { transform: scaleX(1); }
    .brand { text-decoration: none; }
    .cart { display: inline-flex; align-items: center; gap: 0.45rem; }
    .badge { display: inline-flex; align-items: center; justify-content: center; min-width: 1.35rem; height: 1.35rem; padding: 0 0.3rem; background: var(--ink); color: #fff; border-radius: 999px; font-size: 0.66rem; letter-spacing: 0; }
    @media (max-width: 820px) {
      .bar { display: flex; flex-wrap: wrap; justify-content: center; gap: 0.7rem 1.4rem; }
      .brand { order: 1; width: 100%; display: flex; justify-content: center; }
      .left { order: 2; }
      .right { order: 3; }
      nav { gap: 1.4rem; }
      .right { justify-content: center; }
      nav a { font-size: 0.66rem; letter-spacing: 0.16em; }
    }
  `
})
export class HeaderComponent {
  readonly cart = inject(CartService);
}
