import { Component, DestroyRef, afterNextRender, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, of, switchMap, tap } from 'rxjs';
import { AdminApiService } from '../admin-api.service';
import { AdminProductListItem, Paged } from '../admin.models';
import { ImgPipe } from '../../pipes/img.pipe';

@Component({
  selector: 'app-admin-products',
  imports: [DecimalPipe, RouterLink, ImgPipe],
  template: `
    <div class="adm-page">
      <div class="adm-head">
        <div>
          <h1>Produktet</h1>
          <p class="adm-sub">Të gjitha orët, edhe ato të fshehura nga dyqani.</p>
        </div>
        <div class="tools">
          <input class="adm-input find" type="search" placeholder="Kërko: emër ose kod" aria-label="Kërko produkt"
                 [value]="search()" (change)="setSearch($event)">
          <a routerLink="/admin/produktet/new" class="adm-btn">+ Produkt i ri</a>
        </div>
      </div>

      <div class="adm-chips" role="group" aria-label="Filtro">
        @for (f of filters; track f.value) {
          <button type="button" class="adm-chip" [class.on]="active() === f.value" (click)="setActive(f.value)">{{ f.label }}</button>
        }
      </div>

      @if (deleteError()) {
        <p class="adm-errors" role="alert">{{ deleteError() }}</p>
      }

      @if (failed()) {
        <p class="adm-errors" role="alert">Nuk u ngarkuan produktet. A është ndezur API-ja?</p>
      } @else if (loading()) {
        <div class="sk"></div>
      } @else if (!result() || result()!.items.length === 0) {
        <p class="adm-empty">Nuk u gjet asnjë produkt.</p>
      } @else {
        <div class="adm-scroll">
          <table class="adm-table">
            <thead><tr><th></th><th>Ora</th><th>Brendi</th><th class="num">Çmimi</th><th class="num">Stoku</th><th>Në dyqan</th><th></th></tr></thead>
            <tbody>
              @for (p of result()!.items; track p.id) {
                <tr>
                  <td class="thumb">
                    @if (p.imageUrl) { <img [src]="p.imageUrl | appImg" alt="" width="48" height="48" loading="lazy"> } @else { <span class="none">—</span> }
                  </td>
                  <td><a [routerLink]="['/admin/produktet', p.id]"><strong>{{ p.name }}</strong></a><br><small class="code">{{ p.modelCode }}</small></td>
                  <td>{{ p.brand }}</td>
                  <td class="num">{{ p.price | number: '1.0-2' }} €</td>
                  <td class="num"><span class="adm-badge" [class.adm-badge--Cancelled]="p.stock === 0" [class.adm-badge--Pending]="p.stock > 0 && p.stock <= 2">{{ p.stock }}</span></td>
                  <td>
                    <button type="button" class="switch" [class.on]="p.isActive" role="switch" [attr.aria-checked]="p.isActive"
                            [attr.aria-label]="(p.isActive ? 'Fshih ' : 'Shfaq ') + p.name" [disabled]="busy() === p.id" (click)="toggle(p)">
                      <span></span>
                    </button>
                    <small class="state">{{ p.isActive ? 'Aktive' : 'E fshehur' }}</small>
                  </td>
                  <td class="num actions">
                    <a [routerLink]="['/admin/produktet', p.id]" class="adm-btn adm-btn--ghost adm-btn--small">Ndrysho</a>
                    @if (confirming() === p.id) {
                      <span class="ask">Fshi?</span>
                      <button type="button" class="adm-btn adm-btn--small" [disabled]="busy() === p.id" (click)="remove(p)">Po</button>
                      <button type="button" class="adm-btn adm-btn--ghost adm-btn--small" (click)="confirming.set(null)">Jo</button>
                    } @else {
                      <button type="button" class="adm-btn adm-btn--ghost adm-btn--small" (click)="confirming.set(p.id)">Fshi</button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        @if (result()!.totalPages > 1) {
          <nav class="adm-pager" aria-label="Faqet">
            <button type="button" [disabled]="result()!.page <= 1" (click)="goTo(result()!.page - 1)">‹</button>
            @for (p of pages(); track p) { <button type="button" [class.on]="p === result()!.page" (click)="goTo(p)">{{ p }}</button> }
            <button type="button" [disabled]="result()!.page >= result()!.totalPages" (click)="goTo(result()!.page + 1)">›</button>
          </nav>
        }
      }
    </div>
  `,
  styles: `
    .tools { display: flex; gap: 0.7rem; flex-wrap: wrap; align-items: center; }
    .find { min-width: 230px; }
    .thumb { width: 64px; }
    .thumb img { width: 48px; height: 48px; object-fit: contain; background: #fff; border: 1px solid var(--line); }
    .none { color: var(--muted); }
    .code { color: var(--muted); }
    .actions { white-space: nowrap; }
    .actions .adm-btn { margin-left: 0.35rem; }
    .ask { margin-left: 0.5rem; color: var(--muted); font-size: 0.85rem; }
    .state { margin-left: 0.5rem; color: var(--muted); }
    .switch { width: 42px; height: 24px; border-radius: 999px; border: none; background: #cfc8b8; position: relative; cursor: pointer; vertical-align: middle; transition: background 0.3s var(--ease); }
    .switch span { position: absolute; top: 3px; left: 3px; width: 18px; height: 18px; border-radius: 50%; background: #fff; transition: transform 0.3s var(--ease); }
    .switch.on { background: #2f6b45; }
    .switch.on span { transform: translateX(18px); }
    .switch:disabled { opacity: 0.5; cursor: wait; }
    .sk { height: 280px; background: linear-gradient(100deg, #eee9de 30%, #f7f4ee 50%, #eee9de 70%); background-size: 200% 100%; animation: shimmer 1.4s infinite; }
    @keyframes shimmer { to { background-position: -200% 0; } }
  `
})
export class AdminProductsComponent {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly filters = [
    { value: '', label: 'Të gjitha' },
    { value: 'true', label: 'Aktive' },
    { value: 'false', label: 'Të fshehura' }
  ];

  result = signal<Paged<AdminProductListItem> | null>(null);
  search = signal('');
  active = signal('');
  loading = signal(true);
  failed = signal(false);
  busy = signal<number | null>(null);
  confirming = signal<number | null>(null);
  deleteError = signal('');

  constructor() {
    afterNextRender(() => {
      this.route.queryParamMap.pipe(
        tap(params => {
          this.search.set(params.get('search') ?? '');
          this.active.set(params.get('active') ?? '');
          this.loading.set(true);
          this.failed.set(false);
        }),
        switchMap(params => this.api.products({
          search: params.get('search') ?? undefined,
          isActive: params.get('active') === null ? undefined : params.get('active') === 'true',
          page: Number(params.get('page')) || 1
        }).pipe(catchError(() => { this.failed.set(true); return of(null); }))),
        takeUntilDestroyed(this.destroyRef)
      ).subscribe(result => {
        this.result.set(result);
        this.loading.set(false);
      });
    });
  }

  setActive(value: string): void {
    this.router.navigate([], { queryParams: { active: value || null, page: null }, queryParamsHandling: 'merge' });
  }

  setSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value.trim();
    this.router.navigate([], { queryParams: { search: value || null, page: null }, queryParamsHandling: 'merge' });
  }

  goTo(page: number): void {
    this.router.navigate([], { queryParams: { page: page > 1 ? page : null }, queryParamsHandling: 'merge' });
  }

  pages(): number[] {
    return Array.from({ length: this.result()?.totalPages ?? 0 }, (_, i) => i + 1);
  }

  remove(product: AdminProductListItem): void {
    this.busy.set(product.id);
    this.deleteError.set('');
    this.api.deleteProduct(product.id).subscribe({
      next: () => {
        this.result.update(r => r && { ...r, items: r.items.filter(x => x.id !== product.id), totalCount: r.totalCount - 1 });
        this.confirming.set(null);
        this.busy.set(null);
      },
      error: (err: { error?: { message?: string } }) => {
        this.deleteError.set(err?.error?.message ?? 'Ora nuk u fshi. Provo përsëri.');
        this.confirming.set(null);
        this.busy.set(null);
      }
    });
  }

  toggle(product: AdminProductListItem): void {
    this.busy.set(product.id);
    this.api.setActive(product.id, !product.isActive).subscribe({
      next: updated => {
        this.result.update(r => r && { ...r, items: r.items.map(x => x.id === product.id ? { ...x, isActive: updated.isActive } : x) });
        this.busy.set(null);
      },
      error: () => this.busy.set(null)
    });
  }
}
