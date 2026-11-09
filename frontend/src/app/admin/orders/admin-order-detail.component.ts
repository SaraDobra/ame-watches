import { Component, DestroyRef, afterNextRender, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, of, switchMap, tap } from 'rxjs';
import { AdminApiService } from '../admin-api.service';
import { AdminOrderDetail, PAYMENT_LABELS, STATUS_LABELS } from '../admin.models';

const ACTIONS: Record<string, string> = {
  Confirmed: 'Konfirmo porosinë',
  Shipped: 'Shëno si të dërguar',
  Delivered: 'Shëno si të dorëzuar',
  Cancelled: 'Anulo porosinë'
};

@Component({
  selector: 'app-admin-order-detail',
  imports: [DatePipe, DecimalPipe, RouterLink],
  template: `
    <div class="adm-page">
      <a routerLink="/admin/porosite" class="back">← Të gjitha porositë</a>

      @if (loading()) {
        <div class="sk"></div>
      } @else if (!order()) {
        <p class="adm-empty">Porosia nuk u gjet.</p>
      } @else {
        <div class="adm-head">
          <div>
            <h1>{{ order()!.orderNo }}</h1>
            <p class="adm-sub">Regjistruar më {{ order()!.createdAt | date: 'dd.MM.yyyy HH:mm' }}</p>
          </div>
          <span class="adm-badge big" [class]="'adm-badge big adm-badge--' + order()!.status">{{ label(order()!.status) }}</span>
        </div>

        @if (order()!.needsRefund) {
          <p class="adm-warn" role="alert"><strong>Duhet rimbursim.</strong> Porosia u anulua pasi klienti kishte paguar me kartë. Ktheji paratë nga ofruesi i pagesës.</p>
        }
        @if (message()) { <p class="adm-errors" role="alert">{{ message() }}</p> }

        @if (order()!.allowedStatuses.length) {
          <section class="adm-card actions">
            <h2>Veprimet</h2>
            @if (confirming()) {
              <p class="adm-warn">Anulimi e kthen stokun dhe nuk kthehet mbrapsht. Jeni të sigurt?</p>
              <button type="button" class="adm-btn adm-btn--danger" [disabled]="working()" (click)="change('Cancelled')">Po, anuloje</button>
              <button type="button" class="adm-btn adm-btn--ghost" (click)="confirming.set(false)">Jo, mbaje</button>
            } @else {
              @for (status of order()!.allowedStatuses; track status) {
                @if (status === 'Cancelled') {
                  <button type="button" class="adm-btn adm-btn--ghost" [disabled]="working()" (click)="confirming.set(true)">{{ actions[status] }}</button>
                } @else {
                  <button type="button" class="adm-btn" [disabled]="working()" (click)="change(status)">{{ actions[status] }}</button>
                }
              }
            }
          </section>
        }

        <div class="adm-grid cols">
          <section class="adm-card">
            <h2>Klienti</h2>
            <p class="line"><strong>{{ order()!.customerName }}</strong></p>
            <p class="line"><a [href]="'tel:' + order()!.phone">{{ order()!.phone }}</a></p>
            @if (order()!.email) { <p class="line"><a [href]="'mailto:' + order()!.email">{{ order()!.email }}</a></p> }
            <p class="line">{{ order()!.address }}, {{ order()!.city }}</p>
          </section>

          <section class="adm-card">
            <h2>Pagesa</h2>
            <p class="line">{{ pay(order()!.paymentMethod) }} · <span class="adm-badge" [class]="'adm-badge adm-badge--' + order()!.paymentStatus">{{ pay(order()!.paymentStatus) }}</span></p>
            @if (order()!.paidAt) { <p class="line">Paguar më {{ order()!.paidAt | date: 'dd.MM.yyyy HH:mm' }}</p> }
            @if (order()!.warrantyUntil) { <p class="line">Garancia vlen deri më <strong>{{ order()!.warrantyUntil | date: 'dd.MM.yyyy' }}</strong></p> }
          </section>
        </div>

        <section class="adm-card items">
          <h2>Çfarë u porosit</h2>
          <div class="adm-scroll">
            <table class="adm-table">
              <thead><tr><th>Ora</th><th class="num">Çmimi</th><th class="num">Sasia</th><th class="num">Shuma</th></tr></thead>
              <tbody>
                @for (line of order()!.items; track line.productId) {
                  <tr>
                    <td>{{ line.productName }} <small class="code">{{ line.modelCode }}</small></td>
                    <td class="num">{{ line.unitPrice | number: '1.0-2' }} €</td>
                    <td class="num">{{ line.quantity }}</td>
                    <td class="num">{{ line.lineTotal | number: '1.0-2' }} €</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <dl class="totals">
            <div><dt>Nëntotali</dt><dd>{{ order()!.subtotal | number: '1.0-2' }} €</dd></div>
            <div><dt>Dërgesa</dt><dd>{{ order()!.shipping === 0 ? 'Falas' : (order()!.shipping | number: '1.0-2') + ' €' }}</dd></div>
            <div class="grand"><dt>Totali</dt><dd>{{ order()!.total | number: '1.0-2' }} €</dd></div>
          </dl>
        </section>
      }
    </div>
  `,
  styles: `
    .back { display: inline-block; margin-bottom: 1rem; color: var(--gold-dark); text-decoration: none; font-size: 0.88rem; }
    .back:hover { text-decoration: underline; }
    .big { font-size: 0.86rem; padding: 0.4rem 1rem; }
    .actions { margin-bottom: 1.2rem; display: flex; flex-wrap: wrap; gap: 0.7rem; align-items: center; }
    .actions h2 { flex-basis: 100%; margin: 0; }
    .actions .adm-warn { flex-basis: 100%; margin: 0; }
    .cols { grid-template-columns: 1fr 1fr; margin-bottom: 1.2rem; }
    .line { margin: 0 0 0.4rem; }
    .code { color: var(--muted); margin-left: 0.4rem; }
    .items { margin-bottom: 1rem; }
    .totals { margin: 1rem 0 0 auto; max-width: 280px; }
    .totals div { display: flex; justify-content: space-between; padding: 0.3rem 0; }
    .totals dd { margin: 0; }
    .grand { border-top: 1px solid var(--line); margin-top: 0.3rem; padding-top: 0.6rem; font-weight: 500; font-size: 1.1rem; }
    .sk { height: 320px; background: linear-gradient(100deg, #eee9de 30%, #f7f4ee 50%, #eee9de 70%); background-size: 200% 100%; animation: shimmer 1.4s infinite; }
    @keyframes shimmer { to { background-position: -200% 0; } }
    @media (max-width: 900px) { .cols { grid-template-columns: 1fr; } }
  `
})
export class AdminOrderDetailComponent {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly actions = ACTIONS;
  order = signal<AdminOrderDetail | null>(null);
  loading = signal(true);
  working = signal(false);
  confirming = signal(false);
  message = signal('');

  constructor() {
    afterNextRender(() => {
      this.route.paramMap.pipe(
        tap(() => this.loading.set(true)),
        switchMap(params => this.api.order(params.get('orderNo') ?? '').pipe(catchError(() => of(null)))),
        takeUntilDestroyed(this.destroyRef)
      ).subscribe(order => {
        this.order.set(order);
        this.loading.set(false);
      });
    });
  }

  change(status: string): void {
    const current = this.order();
    if (!current || this.working()) return;

    this.working.set(true);
    this.message.set('');
    this.api.changeStatus(current.orderNo, status).subscribe({
      next: updated => {
        this.order.set(updated);
        this.confirming.set(false);
        this.working.set(false);
      },
      error: (err: unknown) => {
        this.working.set(false);
        this.confirming.set(false);
        const text = err instanceof HttpErrorResponse && typeof err.error?.message === 'string' ? err.error.message : 'Ndryshimi nuk u krye.';
        this.message.set(text);
        this.api.order(current.orderNo).subscribe(fresh => this.order.set(fresh));
      }
    });
  }

  label(status: string): string {
    return STATUS_LABELS[status] ?? status;
  }

  pay(value: string): string {
    return PAYMENT_LABELS[value] ?? value;
  }
}
