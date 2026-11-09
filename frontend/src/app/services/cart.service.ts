import { Injectable, PLATFORM_ID, afterNextRender, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { CartItem } from '../models/order.models';
import { ProductDetail } from '../models/catalog.models';
import { CatalogService } from './catalog.service';

const STORAGE_KEY = 'ame-cart-v1';
export const MAX_PER_PRODUCT = 10;

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly catalog = inject(CatalogService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly items = signal<CartItem[]>([]);
  readonly ready = signal(false);

  readonly count = computed(() => this.items().reduce((sum, item) => sum + item.quantity, 0));
  readonly subtotal = computed(() => this.items().reduce((sum, item) => sum + item.price * item.quantity, 0));

  constructor() {
    afterNextRender(() => {
      const stored = this.read();
      this.items.update(current => [...stored.filter(item => !current.some(c => c.productId === item.productId)), ...current]);
      this.write();
      this.ready.set(true);
    });
  }

  quantityOf(productId: number): number {
    return this.items().find(item => item.productId === productId)?.quantity ?? 0;
  }

  add(product: ProductDetail, quantity = 1): 'added' | 'max' | 'unavailable' {
    if (!product.inStock) return 'unavailable';

    const maxQuantity = Math.min(MAX_PER_PRODUCT, product.stock);
    const existing = this.items().find(item => item.productId === product.id);

    if (existing) {
      if (existing.quantity >= maxQuantity) return 'max';
      this.update(items => items.map(item => item.productId === product.id
        ? { ...item, quantity: Math.min(item.quantity + quantity, maxQuantity), maxQuantity, price: product.price }
        : item));
      return 'added';
    }

    this.update(items => [...items, {
      productId: product.id,
      slug: product.slug,
      name: product.name,
      brand: product.brand,
      modelCode: product.modelCode,
      price: product.price,
      imageUrl: product.images[0] ?? null,
      quantity: Math.min(quantity, maxQuantity),
      maxQuantity
    }]);
    return 'added';
  }

  setQuantity(productId: number, quantity: number): void {
    this.update(items => items.map(item => item.productId === productId
      ? { ...item, quantity: Math.max(1, Math.min(quantity, item.maxQuantity)) }
      : item));
  }

  remove(productId: number): void {
    this.update(items => items.filter(item => item.productId !== productId));
  }

  clear(): void {
    this.update(() => []);
  }

  refresh() {
    const current = this.items();
    if (current.length === 0) return of<string[]>([]);

    return forkJoin(current.map(item => this.catalog.getProduct(item.slug).pipe(catchError(() => of(null))))).pipe(
      map(products => {
        const removed: string[] = [];
        const next: CartItem[] = [];

        current.forEach((item, index) => {
          const product = products[index];
          if (!product || !product.inStock) {
            removed.push(`${item.brand} ${item.name}`);
            return;
          }
          const maxQuantity = Math.min(MAX_PER_PRODUCT, product.stock);
          next.push({ ...item, price: product.price, maxQuantity, quantity: Math.min(item.quantity, maxQuantity) });
        });

        this.update(() => next);
        return removed;
      })
    );
  }

  private update(change: (items: CartItem[]) => CartItem[]): void {
    this.items.update(change);
    this.write();
  }

  private read(): CartItem[] {
    if (!this.isBrowser) return [];
    try {
      const parsed = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]');
      if (!Array.isArray(parsed)) return [];
      return parsed.filter((item): item is CartItem =>
        item && Number.isInteger(item.productId) && typeof item.slug === 'string' && typeof item.name === 'string'
        && typeof item.price === 'number' && Number.isInteger(item.quantity) && item.quantity >= 1
        && Number.isInteger(item.maxQuantity) && item.maxQuantity >= 1);
    } catch {
      return [];
    }
  }

  private write(): void {
    if (!this.isBrowser) return;
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.items()));
    } catch {
    }
  }
}
