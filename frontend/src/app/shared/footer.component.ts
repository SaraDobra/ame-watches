import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LogoComponent } from './logo.component';

@Component({
  selector: 'app-footer',
  imports: [RouterLink, LogoComponent],
  template: `
    <footer id="kontakti">
      <div class="container grid">
        <div class="about">
          <app-logo variant="light" [size]="24" />
          <p>Ora origjinale Casio dhe Q&amp;Q, të përzgjedhura me kujdes dhe të dërguara direkt te dera juaj.</p>
        </div>
        <div>
          <h4>Koleksioni</h4>
          <a routerLink="/" fragment="koleksioni">Të zgjedhurat</a>
          <a routerLink="/" fragment="brendet">Casio</a>
          <a routerLink="/" fragment="brendet">Q&amp;Q</a>
        </div>
        <div>
          <h4>Informacion</h4>
          <a routerLink="/" fragment="pse-ame">Garancia</a>
          <a routerLink="/" fragment="pse-ame">Dërgesa</a>
          <a routerLink="/" fragment="pse-ame">Mënyrat e pagesës</a>
        </div>
        <div>
          <h4>Kontakti</h4>
          <a href="#" rel="noopener">Instagram</a>
          <a href="#" rel="noopener">WhatsApp</a>
          <a href="#" rel="noopener">Email</a>
        </div>
      </div>
      <div class="container legal">
        <span>© {{ year }} amé watches. Të gjitha të drejtat e rezervuara.</span>
        <span>Dyqan online</span>
      </div>
    </footer>
  `,
  styles: `
    footer { background: var(--ink); color: #b9b3a6; padding-top: 4rem; margin-top: 0; }
    .grid { display: grid; grid-template-columns: 1.6fr 1fr 1fr 1fr; gap: 3rem; padding-bottom: 3rem; }
    .about p { font-size: 0.9rem; line-height: 1.8; max-width: 34ch; margin: 1.5rem 0 0; }
    h4 { font-family: var(--font-body); font-weight: 500; color: var(--gold-light); letter-spacing: 0.22em; text-transform: uppercase; font-size: 0.72rem; margin: 0 0 1.2rem; }
    a { display: block; color: #b9b3a6; text-decoration: none; font-size: 0.9rem; margin-bottom: 0.7rem; transition: color 0.2s; }
    a:hover { color: #fff; }
    .legal { display: flex; justify-content: space-between; flex-wrap: wrap; gap: 0.5rem; border-top: 1px solid rgba(255, 255, 255, 0.1); padding-top: 1.4rem; padding-bottom: 1.8rem; font-size: 0.78rem; color: #8b8578; }
    @media (max-width: 820px) { .grid { grid-template-columns: 1fr 1fr; } .about { grid-column: 1 / -1; } }
  `
})
export class FooterComponent {
  year = new Date().getFullYear();
}
