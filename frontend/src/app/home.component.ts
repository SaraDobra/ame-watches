import { Component } from '@angular/core';

@Component({
  selector: 'app-home',
  template: `
    <section class="container hero">
      <h1>Koha, e veshur mirë.</h1>
      <p>Ora origjinale Casio dhe Q&amp;Q me garanci 1 vit dhe dërgesë falas.</p>
    </section>
  `,
  styles: `
    .hero { text-align: center; padding-top: 5rem; padding-bottom: 5rem; }
    h1 { font-size: 2.4rem; }
    p { color: var(--muted); }
  `
})
export class HomeComponent {}
