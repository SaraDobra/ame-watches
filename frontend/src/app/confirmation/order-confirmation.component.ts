import { Component, DestroyRef, PLATFORM_ID, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe, isPlatformBrowser } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { EMPTY, catchError, of, switchMap, takeWhile, take, tap, timer } from 'rxjs';
import { OrderService } from '../services/order.service';
import { Order } from '../models/order.models';

@Component({
  selector: 'app-order-confirmation',
  imports: [DatePipe, DecimalPipe, RouterLink],
  templateUrl: './order-confirmation.component.html',
  styleUrl: './order-confirmation.component.scss'
})
export class OrderConfirmationComponent {
  private readonly orders = inject(OrderService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  order = signal<Order | null>(null);
  loading = signal(true);

  constructor() {
    inject(Title).setTitle('Porosia u regjistrua | amé watches');
    inject(Meta).updateTag({ name: 'robots', content: 'noindex' });

    this.route.paramMap.pipe(
      tap(() => this.loading.set(true)),
      switchMap(params => this.orders.get(params.get('orderNo') ?? '', this.route.snapshot.queryParamMap.get('token') ?? '').pipe(
        catchError(() => of(null))
      )),
      takeUntilDestroyed()
    ).subscribe(order => {
      this.order.set(order);
      this.loading.set(false);
      this.watchPayment(order);
    });
  }

  waiting = signal(false);
  gaveUp = signal(false);

  state(order: Order): 'cash' | 'paid' | 'pending' | 'failed' | 'refund' {
    if (order.paymentMethod !== 'Card') return 'cash';
    if (order.paymentStatus === 'Paid') return order.status === 'Cancelled' ? 'refund' : 'paid';
    if (order.status === 'Cancelled' || order.paymentStatus === 'Failed') return 'failed';
    return 'pending';
  }

  private watchPayment(order: Order | null): void {
    if (!order || !this.isBrowser || this.state(order) !== 'pending') {
      this.waiting.set(false);
      return;
    }

    this.waiting.set(true);
    const token = this.route.snapshot.queryParamMap.get('token') ?? '';
    timer(3000, 3000).pipe(
      take(20),
      switchMap(() => this.orders.get(order.orderNo, token, true).pipe(catchError(() => EMPTY))),
      tap(latest => this.order.set(latest)),
      takeWhile(latest => this.state(latest) === 'pending', true),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      complete: () => {
        this.waiting.set(false);
        const latest = this.order();
        this.gaveUp.set(!!latest && this.state(latest) === 'pending');
      }
    });
  }

  paymentText(order: Order): string {
    if (order.paymentMethod === 'CashOnDelivery') return 'Pagesë në dorëzim';
    return order.paymentStatus === 'Paid' ? 'Me kartë — e paguar' : 'Me kartë';
  }
}
