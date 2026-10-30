import { Component, computed, input } from '@angular/core';

let nextId = 0;

const DIALS: Record<string, { from: string; to: string; marks: string; hands: string }> = {
  black: { from: '#2a2a2e', to: '#0c0c0e', marks: '#e9e2d0', hands: '#e9e2d0' },
  navy: { from: '#2c4a73', to: '#0e1f3a', marks: '#f1ead7', hands: '#f1ead7' },
  ivory: { from: '#fbf8f0', to: '#e4dccb', marks: '#2a2723', hands: '#2a2723' },
  green: { from: '#2f5a4a', to: '#0f2a22', marks: '#efe6cf', hands: '#efe6cf' }
};

@Component({
  selector: 'app-watch-art',
  template: `
    <svg viewBox="0 0 400 520" role="img" [attr.aria-label]="label()">
      <defs>
        <linearGradient [attr.id]="id + 'case'" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stop-color="#f0dca8"/>
          <stop offset="0.45" stop-color="#b08d4f"/>
          <stop offset="1" stop-color="#7d6232"/>
        </linearGradient>
        <radialGradient [attr.id]="id + 'dial'" cx="0.4" cy="0.35" r="0.85">
          <stop offset="0" [attr.stop-color]="dial().from"/>
          <stop offset="1" [attr.stop-color]="dial().to"/>
        </radialGradient>
        <linearGradient [attr.id]="id + 'strap'" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0" stop-color="#17171a"/>
          <stop offset="0.5" stop-color="#2b2b30"/>
          <stop offset="1" stop-color="#121214"/>
        </linearGradient>
        <linearGradient [attr.id]="id + 'glass'" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stop-color="#fff" stop-opacity="0.28"/>
          <stop offset="0.5" stop-color="#fff" stop-opacity="0"/>
        </linearGradient>
      </defs>
      <path d="M150 0h100l14 128H136z" [attr.fill]="'url(#' + id + 'strap)'"/>
      <path d="M136 392h128l-14 128H150z" [attr.fill]="'url(#' + id + 'strap)'"/>
      <path d="M200 8v112M200 400v112" stroke="#8d7442" stroke-width="1" stroke-dasharray="4 5" opacity="0.5" transform="translate(-30 0)"/>
      <path d="M200 8v112M200 400v112" stroke="#8d7442" stroke-width="1" stroke-dasharray="4 5" opacity="0.5" transform="translate(30 0)"/>
      <rect x="326" y="236" width="22" height="30" rx="4" [attr.fill]="'url(#' + id + 'case)'"/>
      <circle cx="200" cy="260" r="138" [attr.fill]="'url(#' + id + 'case)'"/>
      <circle cx="200" cy="260" r="124" fill="#14120d"/>
      <circle cx="200" cy="260" r="118" [attr.fill]="'url(#' + id + 'case)'"/>
      <circle cx="200" cy="260" r="110" [attr.fill]="'url(#' + id + 'dial)'"/>
      @for (m of marks; track m) {
        <line x1="200" y1="160" x2="200" [attr.y2]="m % 3 === 0 ? 182 : 172" [attr.stroke]="dial().marks"
              [attr.stroke-width]="m % 3 === 0 ? 4 : 2" stroke-linecap="round" [attr.transform]="'rotate(' + m * 30 + ' 200 260)'"/>
      }
      <text x="200" y="212" text-anchor="middle" class="brand" [attr.fill]="dial().marks">amé</text>
      <rect x="236" y="250" width="30" height="20" rx="2" fill="#f4efe2" stroke="#8d7442" stroke-width="1"/>
      <text x="251" y="265" text-anchor="middle" class="date">{{ day }}</text>
      <line x1="200" y1="260" x2="200" y2="190" [attr.stroke]="dial().hands" stroke-width="5" stroke-linecap="round" transform="rotate(-30 200 260)"/>
      <line x1="200" y1="260" x2="200" y2="168" [attr.stroke]="dial().hands" stroke-width="3.4" stroke-linecap="round" transform="rotate(60 200 260)"/>
      <line x1="200" y1="278" x2="200" y2="166" stroke="#c8a45c" stroke-width="1.6" transform="rotate(150 200 260)"/>
      <circle cx="200" cy="260" r="7" fill="#c8a45c"/>
      <circle cx="200" cy="260" r="2.6" fill="#14120d"/>
      <path d="M118 200a110 110 0 0 1 164 0 110 110 0 0 0-164 0z" [attr.fill]="'url(#' + id + 'glass)'"/>
      <circle cx="200" cy="260" r="110" [attr.fill]="'url(#' + id + 'glass)'"/>
    </svg>
  `,
  styles: `
    :host { display: block; }
    svg { width: 100%; height: auto; display: block; }
    .brand { font-family: var(--font-display); font-style: italic; font-size: 26px; font-weight: 500; }
    .date { font-family: var(--font-body); font-size: 12px; fill: #2a2723; font-weight: 500; }
  `
})
export class WatchArtComponent {
  tone = input<'black' | 'navy' | 'ivory' | 'green'>('black');
  label = input('Orë');
  readonly id = `w${nextId++}`;
  readonly marks = Array.from({ length: 12 }, (_, i) => i);
  readonly day = new Date().getDate();
  readonly dial = computed(() => DIALS[this.tone()]);
}
