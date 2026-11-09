import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { PaymentService } from '../services/payment.service';
import { PaymentSessionInfo } from '../models/order.models';
import { apiErrors } from '../util/api-error';

@Component({
  selector: 'app-payment-simulator',
  imports: [DecimalPipe],
  templateUrl: './simulator.component.html',
  styleUrl: './simulator.component.scss'
})
export class PaymentSimulatorComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly payments = inject(PaymentService);
  private readonly destroyRef = inject(DestroyRef);

  readonly sessionId = this.route.snapshot.paramMap.get('sessionId') ?? '';
  private readonly token = this.route.snapshot.queryParamMap.get('token') ?? '';

  session = signal<PaymentSessionInfo | null>(null);
  loading = signal(true);
  unavailable = signal(false);
  working = signal(false);
  errors = signal<string[]>([]);

  constructor() {
    inject(Title).setTitle('Pagesa (test) | amé watches');
    inject(Meta).updateTag({ name: 'robots', content: 'noindex' });

    this.payments.getSimulatorSession(this.sessionId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: session => { this.session.set(session); this.loading.set(false); },
      error: () => { this.unavailable.set(true); this.loading.set(false); }
    });
  }

  finish(outcome: 'Paid' | 'Failed'): void {
    const session = this.session();
    if (!session || this.working()) return;

    this.working.set(true);
    this.errors.set([]);
    this.payments.completeSimulator(this.sessionId, outcome).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.router.navigate(['/porosia/konfirmim', session.orderNo], { queryParams: { token: this.token } }),
      error: err => { this.errors.set(apiErrors(err)); this.working.set(false); }
    });
  }
}
