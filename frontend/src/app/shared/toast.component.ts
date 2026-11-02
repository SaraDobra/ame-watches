import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-toasts',
  imports: [RouterLink],
  template: `
    <div class="stack" role="status" aria-live="polite">
      @for (toast of toasts.toasts(); track toast.id) {
        <div class="toast">
          <span class="mark" aria-hidden="true">✓</span>
          <span class="text">{{ toast.message }}</span>
          @if (toast.linkUrl) {
            <a [routerLink]="toast.linkUrl" (click)="toasts.dismiss(toast.id)">{{ toast.linkLabel }}</a>
          }
          <button type="button" aria-label="Mbyll" (click)="toasts.dismiss(toast.id)">×</button>
        </div>
      }
    </div>
  `,
  styles: `
    .stack { position: fixed; right: 1.2rem; bottom: 1.2rem; z-index: 60; display: grid; gap: 0.6rem; max-width: min(420px, calc(100vw - 2.4rem)); }
    .toast { display: flex; align-items: center; gap: 0.8rem; background: var(--ink); color: #fff; padding: 0.9rem 1rem; border-left: 3px solid var(--gold); box-shadow: 0 18px 40px -16px rgba(0, 0, 0, 0.6); animation: slide 0.45s var(--ease) both; font-size: 0.9rem; }
    .mark { color: var(--gold-light); }
    .text { flex: 1; }
    a { color: var(--gold-light); text-decoration: underline; white-space: nowrap; font-size: 0.84rem; }
    button { background: none; border: none; color: #9a948a; font-size: 1.2rem; cursor: pointer; line-height: 1; padding: 0 0.2rem; }
    button:hover { color: #fff; }
    @keyframes slide { from { opacity: 0; transform: translateX(24px); } to { opacity: 1; transform: none; } }
    @media (max-width: 640px) { .stack { left: 1rem; right: 1rem; bottom: 4.6rem; max-width: none; } }
  `
})
export class ToastsComponent {
  readonly toasts = inject(ToastService);
}
