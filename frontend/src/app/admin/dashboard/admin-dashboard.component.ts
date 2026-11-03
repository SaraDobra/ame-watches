import { Component, afterNextRender, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AdminApiService } from '../admin-api.service';
import { AdminOrderListItem, DashboardSummary, PAYMENT_LABELS, STATUS_LABELS } from '../admin.models';

@Component({
  selector: 'app-admin-dashboard',
  imports: [DatePipe, DecimalPipe, RouterLink],
  template: `
    <div class="adm-page">
      <div class="adm-head">
        <div>
          <h1>Paneli</h1>
          <p class="adm-sub">Çfarë kërkon vëmendjen tënde sot.</p>
        </div>
        <a routerLink="/admin/produktet/new" class="adm-btn">+ Produkt i ri</a>
      </div>

      @if (error()) {
        <p class="adm-errors" role="alert">{{ error() }}</p>
      } @else if (!summary()) {
        <div class="adm-grid adm-stats">
          @for (n of [1, 2, 3, 4]; track n) { <div class="sk"></div> }
        </div>
      } @else {
        <div class="adm-grid adm-stats">
          <a class="adm-stat" [class.adm-stat--alert]="summary()!.newOrders > 0" routerLink="/admin/porosite" [queryParams]="{ status: 'New' }">
            <strong>{{ summary()!.newOrders }}</strong><span>Porosi të reja (në dorëzim)</span>
          </a>
          <a class="adm-stat" routerLink="/admin/porosite" [queryParams]="{ status: 'New' }">
            <strong>{{ summary()!.awaitingPayment }}</strong><span>Presin pagesën me kartë</span>
          </a>
          <a class="adm-stat" [class.adm-stat--alert]="summary()!.toShip > 0" routerLink="/admin/porosite" [queryParams]="{ status: 'Confirmed' }">
            <strong>{{ summary()!.toShip }}</strong><span>Gati për dërgim</span>
          </a>
          <div class="adm-stat">
            <strong>{{ summary()!.ordersToday }}</strong><span>Porosi sot</span>
          </div>
        </div>

        <div class="adm-grid two">
          <section class="adm-card">
            <h2>Xhiro e muajit</h2>
            <p class="big">{{ summary()!.revenueThisMonth | number: '1.0-2' }} €</p>
            <p class="adm-sub">Vetëm porosi të paguara dhe jo të anuluara.</p>
          </section>

          <section class="adm-card">
            <h2>Stok i ulët</h2>
            @if (summary()!.lowStock.length === 0) {
              <p class="adm-sub">Të gjitha orët kanë stok të mjaftueshëm.</p>
            } @else {
              <ul class="low">
                @for (item of summary()!.lowStock; track item.id) {
                  <li>
                    <a [routerLink]="['/admin/produktet', item.id]">{{ item.name }} <small>{{ item.modelCode }}</small></a>
                    <span class="adm-badge" [class.adm-badge--Cancelled]="item.stock === 0">{{ item.stock === 0 ? 'Shitur' : item.stock + ' copë' }}</span>
                  </li>
                }
              </ul>
            }
          </section>
        </div>

        <h2 class="recent">Porositë e fundit</h2>
        @if (recent().length === 0) {
          <p class="adm-empty">Ende nuk ka porosi.</p>
        } @else {
          <div class="adm-scroll">
            <table class="adm-table">
              <thead><tr><th>Nr.</th><th>Klienti</th><th>Pagesa</th><th class="num">Totali</th><th>Statusi</th><th>Data</th></tr></thead>
              <tbody>
                @for (o of recent(); track o.orderNo) {
                  <tr class="click" [routerLink]="['/admin/porosite', o.orderNo]">
                    <td><a [routerLink]="['/admin/porosite', o.orderNo]">{{ o.orderNo }}</a></td>
                    <td>{{ o.customerName }}</td>
                    <td>{{ pay(o.paymentMethod) }}</td>
                    <td class="num">{{ o.total | number: '1.0-2' }} €</td>
                    <td><span class="adm-badge" [class]="'adm-badge adm-badge--' + o.status">{{ label(o.status) }}</span></td>
                    <td>{{ o.createdAt | date: 'dd.MM HH:mm' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      }
    </div>
  `,
  styles: `
    .two { grid-template-columns: 1fr 1fr; margin-bottom: 1.6rem; }
    .big { font-family: var(--font-display); font-size: 2.8rem; margin: 0; line-height: 1.1; }
    .low { list-style: none; margin: 0; padding: 0; }
    .low li { display: flex; justify-content: space-between; align-items: center; padding: 0.5rem 0; border-bottom: 1px solid var(--line); gap: 1rem; }
    .low li:last-child { border-bottom: none; }
    .low a { color: var(--ink); text-decoration: none; }
    .low a:hover { text-decoration: underline; }
    .low small { color: var(--muted); margin-left: 0.4rem; }
    .recent { font-size: 1.4rem; margin: 0 0 0.8rem; }
    .sk { height: 92px; background: linear-gradient(100deg, #eee9de 30%, #f7f4ee 50%, #eee9de 70%); background-size: 200% 100%; animation: shimmer 1.4s infinite; }
    @keyframes shimmer { to { background-position: -200% 0; } }
    @media (max-width: 900px) { .two { grid-template-columns: 1fr; } }
  `
})
export class AdminDashboardComponent {
  private readonly api = inject(AdminApiService);

  summary = signal<DashboardSummary | null>(null);
  recent = signal<AdminOrderListItem[]>([]);
  error = signal('');

  constructor() {
    afterNextRender(() => {
      forkJoin({ summary: this.api.dashboard(), recent: this.api.orders({ pageSize: 5 }) }).subscribe({
        next: ({ summary, recent }) => {
          this.summary.set(summary);
          this.recent.set(recent.items);
        },
        error: () => this.error.set('Nuk u ngarkuan të dhënat. A është ndezur API-ja?')
      });
    });
  }

  label(status: string): string {
    return STATUS_LABELS[status] ?? status;
  }

  pay(method: string): string {
    return PAYMENT_LABELS[method] ?? method;
  }
}
