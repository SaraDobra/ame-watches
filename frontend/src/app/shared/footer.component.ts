import { Component } from '@angular/core';

@Component({
  selector: 'app-footer',
  template: `
    <footer>
      <div class="container">
        <span>© {{ year }} amé watches</span>
        <span>Garanci 1 vit · Dërgesë falas</span>
      </div>
    </footer>
  `,
  styles: `
    footer { background: var(--deep); color: #cbd5e1; font-size: 0.85rem; padding: 1.5rem 0; margin-top: 3rem; }
    .container { display: flex; justify-content: space-between; flex-wrap: wrap; gap: 0.5rem; }
  `
})
export class FooterComponent {
  year = new Date().getFullYear();
}
