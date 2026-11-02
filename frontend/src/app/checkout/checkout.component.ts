import { Component, DestroyRef, effect, inject, signal, untracked } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { CartService } from '../services/cart.service';
import { OrderService } from '../services/order.service';
import { apiErrors } from '../util/api-error';

const PHONE = /^\+?[0-9][0-9\s\-]{6,18}[0-9]$/;

@Component({
  selector: 'app-checkout',
  imports: [DecimalPipe, ReactiveFormsModule, RouterLink],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss'
})
export class CheckoutComponent {
  readonly cart = inject(CartService);
  private readonly orders = inject(OrderService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);

  submitting = signal(false);
  errors = signal<string[]>([]);
  checked = signal(false);

  form = this.fb.nonNullable.group({
    customerName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
    phone: ['', [Validators.required, Validators.pattern(PHONE)]],
    email: ['', [Validators.email, Validators.maxLength(150)]],
    city: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    address: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(250)]],
    paymentMethod: ['CashOnDelivery' as 'CashOnDelivery' | 'Card']
  });

  constructor() {
    inject(Title).setTitle('Porosia | amé watches');
    inject(Meta).updateTag({ name: 'robots', content: 'noindex' });

    effect(() => {
      if (!this.cart.ready() || untracked(() => this.checked())) return;
      this.checked.set(true);
      this.cart.refresh().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(removed => {
        if (removed.length > 0) {
          this.errors.set([`U hoqën nga shporta sepse nuk janë më të disponueshme: ${removed.join(', ')}.`]);
        }
        if (this.cart.items().length === 0) {
          this.router.navigate(['/shporta']);
        }
      });
    });
  }

  invalid(name: 'customerName' | 'phone' | 'email' | 'city' | 'address'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  submit(): void {
    this.errors.set([]);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errors.set(['Plotësoni saktë fushat e shënuara.']);
      return;
    }
    if (this.cart.items().length === 0 || this.submitting()) return;

    const value = this.form.getRawValue();
    this.submitting.set(true);

    this.orders.create({
      customerName: value.customerName.trim(),
      phone: value.phone.trim(),
      email: value.email.trim() || null,
      city: value.city.trim(),
      address: value.address.trim(),
      paymentMethod: value.paymentMethod,
      items: this.cart.items().map(item => ({ productId: item.productId, quantity: item.quantity }))
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: created => {
        this.cart.clear();
        if (created.paymentUrl) {
          window.location.assign(created.paymentUrl);
          return;
        }
        this.router.navigate(['/porosia/konfirmim', created.orderNo], { queryParams: { token: created.accessToken } });
      },
      error: err => {
        this.errors.set(apiErrors(err));
        this.submitting.set(false);
        if (err?.status === 409 || err?.status === 400) {
          this.cart.refresh().pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
        }
      }
    });
  }
}
