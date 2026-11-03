import { Component, computed, effect, input, output, signal } from '@angular/core';

@Component({
  selector: 'app-price-range',
  template: `
    <div class="values" aria-live="polite">
      <span class="pill">{{ lo() }} €</span>
      <span class="dash" aria-hidden="true"></span>
      <span class="pill">{{ hi() }} €</span>
    </div>

    <div class="slider" [style.--l]="left() + '%'" [style.--r]="right() + '%'">
      <div class="track"><div class="fill"></div></div>
      <input type="range" [min]="min()" [max]="max()" [step]="step()" [value]="lo()"
             [class.top]="lo() > min() + (max() - min()) / 2"
             (input)="onLo($event)" (change)="commit()" aria-label="Çmimi minimal">
      <input type="range" [min]="min()" [max]="max()" [step]="step()" [value]="hi()"
             (input)="onHi($event)" (change)="commit()" aria-label="Çmimi maksimal">
    </div>

    <div class="limits"><span>{{ min() }} €</span><span>{{ max() }} €</span></div>
  `,
  styles: `
    :host { display: block; }
    .values { display: flex; align-items: center; gap: 0.6rem; margin-bottom: 1.1rem; }
    .pill { background: var(--paper); border: 1px solid var(--line); border-radius: var(--radius-pill); padding: 0.32rem 0.9rem; font-weight: 600; font-size: 0.88rem; box-shadow: var(--shadow-soft); min-width: 4.2rem; text-align: center; }
    .dash { flex: 1; height: 1px; background: var(--line); }
    .slider { position: relative; height: 28px; }
    .track { position: absolute; left: 0; right: 0; top: 50%; height: 5px; margin-top: -2.5px; background: #e6dfcd; border-radius: 999px; }
    .fill { position: absolute; top: 0; bottom: 0; left: var(--l); right: calc(100% - var(--r)); background: linear-gradient(90deg, var(--gold), var(--gold-dark)); border-radius: 999px; }
    input[type='range'] { position: absolute; inset: 0; width: 100%; height: 28px; margin: 0; background: transparent; -webkit-appearance: none; appearance: none; pointer-events: none; outline: none; }
    input.top { z-index: 3; }
    input[type='range']::-webkit-slider-runnable-track { background: transparent; height: 28px; }
    input[type='range']::-moz-range-track { background: transparent; height: 28px; }
    input[type='range']::-webkit-slider-thumb { -webkit-appearance: none; pointer-events: auto; width: 24px; height: 24px; margin-top: 2px; border-radius: 50%; background: var(--paper); border: 2px solid var(--gold-dark); box-shadow: 0 6px 14px -4px rgba(12, 12, 13, 0.45); cursor: grab; transition: transform 0.2s var(--ease), box-shadow 0.2s var(--ease); }
    input[type='range']::-moz-range-thumb { pointer-events: auto; width: 20px; height: 20px; border-radius: 50%; background: var(--paper); border: 2px solid var(--gold-dark); box-shadow: 0 6px 14px -4px rgba(12, 12, 13, 0.45); cursor: grab; }
    input[type='range']:hover::-webkit-slider-thumb { transform: scale(1.1); }
    input[type='range']:active::-webkit-slider-thumb { cursor: grabbing; transform: scale(1.18); }
    input[type='range']:focus-visible::-webkit-slider-thumb { box-shadow: var(--ring); }
    .limits { display: flex; justify-content: space-between; margin-top: 0.35rem; font-size: 0.76rem; color: var(--muted); }
  `
})
export class PriceRangeComponent {
  min = input.required<number>();
  max = input.required<number>();
  from = input<number | null>(null);
  to = input<number | null>(null);
  changed = output<{ from: number | null; to: number | null }>();

  lo = signal(0);
  hi = signal(0);

  readonly step = computed(() => (this.max() - this.min() > 40 ? 5 : 1));
  readonly left = computed(() => this.percent(this.lo()));
  readonly right = computed(() => this.percent(this.hi()));

  constructor() {
    effect(() => {
      this.lo.set(this.clamp(this.from() ?? this.min()));
      this.hi.set(this.clamp(this.to() ?? this.max()));
    });
  }

  onLo(event: Event): void {
    this.lo.set(Math.min(Number((event.target as HTMLInputElement).value), this.hi()));
  }

  onHi(event: Event): void {
    this.hi.set(Math.max(Number((event.target as HTMLInputElement).value), this.lo()));
  }

  commit(): void {
    this.changed.emit({
      from: this.lo() <= this.min() ? null : this.lo(),
      to: this.hi() >= this.max() ? null : this.hi()
    });
  }

  private percent(value: number): number {
    const span = this.max() - this.min();
    return span <= 0 ? 0 : ((value - this.min()) / span) * 100;
  }

  private clamp(value: number): number {
    return Math.min(Math.max(value, this.min()), this.max());
  }
}
