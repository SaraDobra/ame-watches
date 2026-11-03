import { Component, DestroyRef, effect, inject, signal, untracked } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { CartService } from '../services/cart.service';
import { WatchArtComponent } from '../shared/watch-art.component';
import { ImgPipe } from '../pipes/img.pipe';

@Component({
  selector: 'app-cart',
  imports: [DecimalPipe, RouterLink, WatchArtComponent, ImgPipe],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss'
})
export class CartComponent {
  readonly cart = inject(CartService);
  private readonly destroyRef = inject(DestroyRef);

  notice = signal('');
  refreshed = signal(false);

  constructor() {
    inject(Title).setTitle('Shporta | amé watches');
    inject(Meta).updateTag({ name: 'robots', content: 'noindex' });

    effect(() => {
      if (!this.cart.ready() || untracked(() => this.refreshed())) return;
      this.refreshed.set(true);
      this.cart.refresh().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(removed => {
        if (removed.length > 0) {
          this.notice.set(`U hoqën nga shporta sepse nuk janë më të disponueshme: ${removed.join(', ')}.`);
        }
      });
    });
  }

  change(productId: number, delta: number, current: number): void {
    this.cart.setQuantity(productId, current + delta);
  }
}
