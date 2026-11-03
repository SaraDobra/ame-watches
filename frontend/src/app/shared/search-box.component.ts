import { Component, ElementRef, HostListener, effect, inject, input, output, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { CatalogService } from '../services/catalog.service';
import { ProductSuggestion } from '../models/catalog.models';
import { ImgPipe } from '../pipes/img.pipe';

interface Part {
  text: string;
  match: boolean;
}

@Component({
  selector: 'app-search-box',
  imports: [DecimalPipe, ImgPipe],
  template: `
    <div class="box" [class.focused]="focused()">
      <svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" aria-hidden="true">
        <circle cx="11" cy="11" r="6.5"/><path d="m20 20-4.2-4.2"/>
      </svg>
      <input type="text" role="combobox" autocomplete="off" spellcheck="false"
             aria-autocomplete="list" aria-controls="search-suggestions" [attr.aria-expanded]="panelOpen()"
             [attr.aria-activedescendant]="active() >= 0 ? 'suggestion-' + active() : null"
             placeholder="Kërko orë ose kod" aria-label="Kërko orë"
             [value]="query()" (input)="onInput($event)" (keydown)="onKey($event)"
             (focus)="focused.set(true); open.set(true)" (blur)="focused.set(false)">
      @if (query()) {
        <button type="button" class="clear" aria-label="Pastro kërkimin" (mousedown)="$event.preventDefault()" (click)="clear()">×</button>
      }
    </div>

    @if (panelOpen()) {
      <ul id="search-suggestions" class="panel" role="listbox" (mousedown)="$event.preventDefault()">
        @for (item of items(); track item.slug; let i = $index) {
          <li role="option" [id]="'suggestion-' + i" [class.on]="i === active()" [attr.aria-selected]="i === active()"
              (mouseenter)="active.set(i)" (click)="pick(item)">
            <span class="thumb">
              @if (item.imageUrl) { <img [src]="item.imageUrl | appImg" alt="" width="44" height="44" loading="lazy"> }
            </span>
            <span class="text">
              <span class="name">@for (part of highlight(item.brand + ' ' + item.name); track $index) {@if (part.match) {<b>{{ part.text }}</b>} @else {{{ part.text }}}}</span>
              <span class="code">@for (part of highlight(item.modelCode); track $index) {@if (part.match) {<b>{{ part.text }}</b>} @else {{{ part.text }}}}</span>
            </span>
            <span class="price">{{ item.price | number: '1.0-2' }} €</span>
          </li>
        }
        @if (items().length === 0 && !loading()) {
          <li class="empty" role="presentation">Nuk ka sugjerime për “{{ query().trim() }}”.</li>
        }
        <li class="all" role="option" [attr.aria-selected]="false" (click)="submit()">
          Shiko të gjitha rezultatet për “{{ query().trim() }}” <span aria-hidden="true">→</span>
        </li>
      </ul>
    }
  `,
  styles: `
    :host { display: block; position: relative; }
    .box { display: flex; align-items: center; gap: 0.6rem; padding: 0 0.5rem 0 1.1rem; background: var(--paper); border: 1px solid var(--line); border-radius: var(--radius-pill); box-shadow: var(--shadow-soft); transition: border-color 0.25s var(--ease), box-shadow 0.25s var(--ease); }
    .box:hover { border-color: #d9cfb8; }
    .box.focused { border-color: var(--gold); box-shadow: var(--ring); }
    .icon { width: 19px; height: 19px; color: var(--gold-dark); flex: none; }
    input { flex: 1; min-width: 0; border: none; outline: none; background: transparent; padding: 0.95rem 0; font: inherit; font-size: 0.95rem; color: var(--ink); }
    input::placeholder { color: #9a948a; }
    .clear { border: none; background: #f1ece0; color: #6f6a60; width: 28px; height: 28px; border-radius: 50%; cursor: pointer; font-size: 1.1rem; line-height: 1; transition: background 0.2s; }
    .clear:hover { background: #e6dfcd; }
    .panel { position: absolute; z-index: 40; left: 0; width: max(100%, min(400px, calc(100vw - 2rem))); top: calc(100% + 0.5rem); margin: 0; padding: 0.4rem; list-style: none; background: var(--paper); border: 1px solid var(--line); border-radius: var(--radius-md); box-shadow: 0 24px 50px -22px rgba(12, 12, 13, 0.55); animation: pop 0.22s var(--ease); max-height: 420px; overflow-y: auto; }
    @keyframes pop { from { opacity: 0; transform: translateY(-6px) scale(0.98); } to { opacity: 1; transform: none; } }
    li { display: flex; align-items: center; gap: 0.8rem; padding: 0.55rem 0.7rem; border-radius: var(--radius-sm); cursor: pointer; transition: background 0.15s; }
    li.on { background: #f6efe0; }
    .thumb { width: 44px; height: 44px; flex: none; background: #fff; border: 1px solid var(--line); border-radius: 10px; overflow: hidden; display: flex; align-items: center; justify-content: center; }
    .thumb img { width: 100%; height: 100%; object-fit: contain; }
    .text { display: flex; flex-direction: column; min-width: 0; flex: 1; }
    .name { font-size: 0.9rem; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .code { font-size: 0.76rem; color: var(--muted); }
    b { font-weight: 700; color: var(--gold-dark); }
    .price { font-family: var(--font-display); font-weight: 500; font-size: 1rem; white-space: nowrap; }
    .empty { color: var(--muted); font-size: 0.88rem; cursor: default; }
    .all { justify-content: space-between; color: var(--gold-dark); font-size: 0.84rem; font-weight: 600; border-top: 1px solid var(--line); border-radius: 0 0 var(--radius-sm) var(--radius-sm); margin-top: 0.3rem; }
    .all:hover { background: #f6efe0; }
    @media (max-width: 820px) { .panel { width: 100%; } }
  `
})
export class SearchBoxComponent {
  private readonly catalog = inject(CatalogService);
  private readonly router = inject(Router);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  value = input('');
  search = output<string>();

  query = signal('');
  items = signal<ProductSuggestion[]>([]);
  open = signal(false);
  focused = signal(false);
  loading = signal(false);
  active = signal(-1);

  private readonly typed = new Subject<string>();

  constructor() {
    effect(() => this.query.set(this.value()));

    this.typed.pipe(
      debounceTime(160),
      distinctUntilChanged(),
      switchMap(text => {
        if (!text.trim()) {
          this.loading.set(false);
          return of([] as ProductSuggestion[]);
        }
        this.loading.set(true);
        return this.catalog.suggest(text.trim()).pipe(catchError(() => of([] as ProductSuggestion[])));
      }),
      takeUntilDestroyed()
    ).subscribe(items => {
      this.items.set(items);
      this.active.set(-1);
      this.loading.set(false);
    });
  }

  panelOpen(): boolean {
    return this.open() && this.query().trim().length > 0;
  }

  onInput(event: Event): void {
    const text = (event.target as HTMLInputElement).value;
    this.query.set(text);
    this.open.set(true);
    this.typed.next(text);
  }

  onKey(event: KeyboardEvent): void {
    const count = this.items().length;

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.open.set(true);
      this.active.update(index => Math.min(index + 1, count - 1));
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.active.update(index => Math.max(index - 1, -1));
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const picked = this.items()[this.active()];
      if (picked) this.pick(picked);
      else this.submit();
    } else if (event.key === 'Escape') {
      this.open.set(false);
    }
  }

  pick(item: ProductSuggestion): void {
    this.open.set(false);
    this.router.navigate(['/ora', item.slug]);
  }

  submit(): void {
    this.open.set(false);
    this.search.emit(this.query().trim());
  }

  clear(): void {
    this.query.set('');
    this.items.set([]);
    this.typed.next('');
    this.search.emit('');
  }

  highlight(text: string): Part[] {
    const term = this.query().trim().toLowerCase();
    const at = term ? text.toLowerCase().indexOf(term) : -1;
    if (at < 0) return [{ text, match: false }];

    return [
      { text: text.slice(0, at), match: false },
      { text: text.slice(at, at + term.length), match: true },
      { text: text.slice(at + term.length), match: false }
    ].filter(part => part.text.length > 0);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    if (!this.host.nativeElement.contains(event.target as Node)) this.open.set(false);
  }
}
