import { Component, input } from '@angular/core';

@Component({
  selector: 'app-logo',
  template: `
    <span class="logo" [class.logo--light]="variant() === 'light'" [style.--size]="size()">
      <svg class="emblem" viewBox="0 0 100 100" aria-hidden="true">
        <circle cx="50" cy="50" r="46" fill="none" stroke="currentColor" stroke-width="1.4"/>
        <circle cx="50" cy="50" r="40" fill="none" stroke="var(--gold)" stroke-width="0.8"/>
        @for (tick of ticks; track tick) {
          <line x1="50" y1="12" x2="50" [attr.y2]="tick % 3 === 0 ? 20 : 16" stroke="currentColor"
                [attr.stroke-width]="tick % 3 === 0 ? 1.8 : 0.9" [attr.transform]="'rotate(' + tick * 30 + ' 50 50)'"/>
        }
        <text x="50" y="63" text-anchor="middle" class="mono">a</text>
      </svg>
      <span class="words">
        <span class="name">AMÉ</span>
        <span class="rule"></span>
        <span class="sub">WATCHES</span>
      </span>
    </span>
  `,
  styles: `
    .logo { display: inline-flex; align-items: center; gap: calc(var(--size) * 0.55px); color: var(--ink); line-height: 1; }
    .logo--light { color: var(--ivory); }
    .emblem { width: calc(var(--size) * 2.15px); height: calc(var(--size) * 2.15px); flex: none; transition: width 0.35s var(--ease), height 0.35s var(--ease); }
    .mono { font-family: var(--font-display); font-size: 40px; font-style: italic; font-weight: 500; fill: currentColor; }
    .words { display: flex; flex-direction: column; align-items: center; }
    .name { transition: font-size 0.35s var(--ease); font-family: var(--font-display); font-weight: 500; font-size: calc(var(--size) * 1.45px); letter-spacing: 0.3em; margin-right: -0.3em; line-height: 1; }
    .rule { display: block; width: 100%; height: 1px; margin: calc(var(--size) * 0.28px) 0 calc(var(--size) * 0.3px); background: var(--gold); }
    .sub { transition: font-size 0.35s var(--ease); font-family: var(--font-body); font-weight: 400; font-size: calc(var(--size) * 0.5px); letter-spacing: 0.5em; margin-right: -0.5em; line-height: 1; }
  `
})
export class LogoComponent {
  variant = input<'dark' | 'light'>('dark');
  size = input(26);
  readonly ticks = Array.from({ length: 12 }, (_, i) => i);
}
