import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LogoComponent } from './logo.component';

@Component({
  selector: 'app-header',
  imports: [RouterLink, LogoComponent],
  template: `
    <div class="topbar">Dërgesë falas në gjithë vendin &nbsp;·&nbsp; Garanci 1 vit nga amé watches</div>
    <header>
      <div class="container bar">
        <nav class="left">
          <a routerLink="/" fragment="koleksioni">Koleksioni</a>
          <a routerLink="/" fragment="brendet">Brendet</a>
        </nav>
        <a routerLink="/" class="brand" aria-label="amé watches, ballina">
          <app-logo [size]="24" />
        </a>
        <nav class="right">
          <a routerLink="/" fragment="pse-ame">Pse amé</a>
          <a routerLink="/" fragment="kontakti">Kontakti</a>
        </nav>
      </div>
    </header>
  `,
  styles: `
    .topbar { background: var(--ink); color: var(--gold-light); text-align: center; font-size: 0.7rem; letter-spacing: 0.22em; text-transform: uppercase; padding: 0.6rem 1rem; }
    header { position: sticky; top: 0; z-index: 20; background: rgba(247, 244, 238, 0.94); backdrop-filter: blur(10px); border-bottom: 1px solid var(--line); }
    .bar { display: grid; grid-template-columns: 1fr auto 1fr; align-items: center; padding-top: 1rem; padding-bottom: 1rem; }
    nav { display: flex; gap: 2rem; }
    .right { justify-content: flex-end; }
    nav a { font-size: 0.72rem; letter-spacing: 0.2em; text-transform: uppercase; text-decoration: none; color: var(--ink); position: relative; padding: 0.3rem 0; }
    nav a::after { content: ''; position: absolute; left: 0; right: 0; bottom: 0; height: 1px; background: var(--gold); transform: scaleX(0); transform-origin: left; transition: transform 0.3s; }
    nav a:hover::after { transform: scaleX(1); }
    .brand { text-decoration: none; }
    @media (max-width: 820px) {
      .bar { grid-template-columns: 1fr; justify-items: center; gap: 0.9rem; }
      nav { gap: 1.4rem; }
      .left { order: 2; } .right { order: 3; justify-content: center; } .brand { order: 1; }
      .bar { grid-template-rows: auto; }
      nav.left, nav.right { display: none; }
    }
  `
})
export class HeaderComponent {}
