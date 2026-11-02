import { Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { catchError, of, switchMap, tap } from 'rxjs';
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
    });
  }

  paymentText(order: Order): string {
    return order.paymentMethod === 'CashOnDelivery' ? 'Pagesë në dorëzim' : 'Me kartë';
  }
}
