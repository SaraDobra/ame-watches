import { Component, DestroyRef, afterNextRender, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, of, switchMap, tap } from 'rxjs';
import { AdminApiService } from '../admin-api.service';
import { AdminOrderListItem, PAYMENT_LABELS, Paged, STATUS_LABELS } from '../admin.models';

const FILTERS = ['', 'New', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled'];

@Component({
  selector: 'app-admin-orders',
  imports: [DatePipe, DecimalPipe, RouterLink],
  template: `
    <div class="adm-page">
      <div class="adm-head">
        <div>
          <h1>Porositë</h1>
          <p class="adm-sub">Të gjitha porositë, më të rejat së pari.</p>
        </div>
        <input class="adm-input find" type="search" placeholder="Kërko: numër, emër, telefon"
               aria-label="Kërko porosi" [value]="search()" (change)="setSearch($event)">
      </div>

      <div class="adm-chips" role="group" aria-label="Filtro sipas statusit">
        @for (f of filters; track f) {
          <button type="button" class="adm-chip" [class.on]="status() === f" (click)="setStatus(f)">{{ f ? label(f) : 'Të gjitha' }}</button>
        }
      </div>

      @if (failed()) {
        <p class="adm-errors" role="alert">Nuk u ngarkuan porositë. A është ndezur API-ja?</p>
      } @else if (loading()) {
        <div class="sk"></div>
      } @else if (!result() || result()!.items.length === 0) {
        <p class="adm-empty">Nuk u gjet asnjë porosi.</p>
      } @else {
        <div class="adm-scroll">
          <table class="adm-table">
            <thead><tr><th>Nr.</th><th>Klienti</th><th>Telefoni</th><th>Pagesa</th><th class="num">Totali</th><th>Statusi</th><th>Data</th></tr></thead>
            <tbody>
              @for (o of result()!.items; track o.orderNo) {
                <tr class="click" [routerLink]="['/admin/porosite', o.orderNo]">
                  <td><a [routerLink]="['/admin/porosite', o.orderNo]">{{ o.orderNo }}</a></td>
                  <td>{{ o.customerName }}</td>
                  <td>{{ o.phone }}</td>
                  <td>{{ pay(o.paymentMethod) }} · <span class="adm-badge" [class]="'adm-badge adm-badge--' + o.paymentStatus">{{ pay(o.paymentStatus) }}</span></td>
                  <td class="num">{{ o.total | number: '1.0-2' }} €</td>
                  <td><span class="adm-badge" [class]="'adm-badge adm-badge--' + o.status">{{ label(o.status) }}</span></td>
                  <td>{{ o.createdAt | date: 'dd.MM.yyyy HH:mm' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        @if (result()!.totalPages > 1) {
          <nav class="adm-pager" aria-label="Faqet">
            <button type="button" [disabled]="result()!.page <= 1" (click)="goTo(result()!.page - 1)">‹</button>
            @for (p of pages(); track p) {
              <button type="button" [class.on]="p === result()!.page" (click)="goTo(p)">{{ p }}</button>
            }
            <button type="button" [disabled]="result()!.page >= result()!.totalPages" (click)="goTo(result()!.page + 1)">›</button>
            <span class="adm-sub total">{{ result()!.totalCount }} porosi</span>
          </nav>
        }
      }
    </div>
  `,
  styles: `
    .find { min-width: 260px; }
    .total { margin: 0 0 0 0.8rem; }
    .sk { height: 260px; background: linear-gradient(100deg, #eee9de 30%, #f7f4ee 50%, #eee9de 70%); background-size: 200% 100%; animation: shimmer 1.4s infinite; }
    @keyframes shimmer { to { background-position: -200% 0; } }
  `
})
export class AdminOrdersComponent {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly filters = FILTERS;
  result = signal<Paged<AdminOrderListItem> | null>(null);
  status = signal('');
  search = signal('');
  loading = signal(true);
  failed = signal(false);

  constructor() {
    afterNextRender(() => {
      this.route.queryParamMap.pipe(
        tap(params => {
          this.status.set(params.get('status') ?? '');
          this.search.set(params.get('search') ?? '');
          this.loading.set(true);
          this.failed.set(false);
        }),
        switchMap(params => this.api.orders({
          status: params.get('status') ?? undefined,
          search: params.get('search') ?? undefined,
          page: Number(params.get('page')) || 1
        }).pipe(catchError(() => { this.failed.set(true); return of(null); }))),
        takeUntilDestroyed(this.destroyRef)
      ).subscribe(result => {
        this.result.set(result);
        this.loading.set(false);
      });
    });
  }

  setStatus(status: string): void {
    this.router.navigate([], { queryParams: { status: status || null, page: null }, queryParamsHandling: 'merge' });
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

  label(status: string): string {
    return STATUS_LABELS[status] ?? status;
  }

  pay(value: string): string {
    return PAYMENT_LABELS[value] ?? value;
  }
}
