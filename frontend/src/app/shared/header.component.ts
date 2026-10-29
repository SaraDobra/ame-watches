import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-header',
  imports: [RouterLink],
  template: `
    <header>
      <div class="container bar">
        <a routerLink="/" class="brand">
          <img src="img/logo.png" alt="amé watches" width="180" height="68">
        </a>
        <nav>
          <a routerLink="/">Ballina</a>
        </nav>
      </div>
    </header>
  `,
  styles: `
    header { background: var(--surface); border-bottom: 1px solid var(--border); }
    .bar { display: flex; align-items: center; justify-content: space-between; padding-top: 0.5rem; padding-bottom: 0.5rem; }
    .brand img { height: 56px; width: auto; }
    nav a { text-decoration: none; margin-left: 1.5rem; font-size: 0.95rem; color: var(--muted); }
    nav a:hover { color: var(--text); }
  `
})
export class HeaderComponent {}
