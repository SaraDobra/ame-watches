import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, map, of } from 'rxjs';
import { CartService } from './cart.service';
import { CatalogService } from './catalog.service';
import { ToastService } from './toast.service';

/** Shton një orë në shportë nga një kartelë (që ka vetëm të dhëna të shkurtra), ose e blen menjëherë. */
@Injectable({ providedIn: 'root' })
export class QuickBuyService {
  private readonly catalog = inject(CatalogService);
  private readonly cart = inject(CartService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  /** Merr orën e plotë nga serveri (stoku dhe çmimi aktual), e shton, dhe me `checkout` të çon te porosia. */
  add(slug: string, checkout = false): Observable<boolean> {
    return this.catalog.getProduct(slug).pipe(
      map(product => {
        const alreadyInCart = this.cart.quantityOf(product.id) > 0;
        const result = checkout && alreadyInCart ? 'added' : this.cart.add(product);

        if (result === 'unavailable') {
          this.toast.show('Na vjen keq, kjo orë është shitur.');
          return false;
        }
        if (checkout) {
          this.router.navigate(['/porosia']);
          return true;
        }
        if (result === 'max') {
          this.toast.show('Keni shtuar sasinë maksimale të disponueshme për këtë orë.', { label: 'Shiko shportën', url: '/shporta' });
          return false;
        }
        this.toast.show(`${product.brand} ${product.name} u shtua në shportë`, { label: 'Shiko shportën', url: '/shporta' });
        return true;
      }),
      catchError(() => {
        this.toast.show('Nuk u shtua në shportë. Provoni përsëri.');
        return of(false);
      })
    );
  }
}
