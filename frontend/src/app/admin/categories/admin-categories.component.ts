import { Component, afterNextRender, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';
import { AdminApiService } from '../admin-api.service';
import { AdminCategory } from '../admin.models';
import { CatalogService } from '../../services/catalog.service';
import { Brand } from '../../models/catalog.models';

@Component({
  selector: 'app-admin-categories',
  imports: [FormsModule],
  template: `
    <div class="adm-page">
      <div class="adm-head">
        <div>
          <h1>Kategoritë</h1>
          <p class="adm-sub">Çdo kategori i përket një brendi (p.sh. G-SHOCK te Casio). Klientët i filtrojnë orët sipas tyre.</p>
        </div>
      </div>

      <form class="adm-card add" (ngSubmit)="create()" aria-label="Shto kategori">
        <div class="adm-field">
          <label for="newName">Kategori e re</label>
          <input id="newName" name="newName" [(ngModel)]="newName" maxlength="100" placeholder="p.sh. PRO TREK" autocomplete="off">
        </div>
        <div class="adm-field">
          <label for="newBrand">Brendi</label>
          <select id="newBrand" name="newBrand" [(ngModel)]="newBrandId">
            @for (b of brands(); track b.id) { <option [ngValue]="b.id">{{ b.name }}</option> }
          </select>
        </div>
        <button type="submit" class="adm-btn" [disabled]="busy() || newName.trim().length < 2">+ Shto</button>
      </form>

      @if (error()) {
        <p class="adm-errors" role="alert">{{ error() }}</p>
      }

      @if (failed()) {
        <p class="adm-errors" role="alert">Nuk u ngarkuan kategoritë. A është ndezur API-ja?</p>
      } @else if (loading()) {
        <div class="sk"></div>
      } @else if (items().length === 0) {
        <p class="adm-empty">Nuk ka asnjë kategori. Shtoni të parën më sipër.</p>
      } @else {
        <div class="adm-scroll">
          <table class="adm-table">
            <thead><tr><th>Kategoria</th><th>Brendi</th><th class="num">Ora</th><th></th></tr></thead>
            <tbody>
              @for (c of items(); track c.id) {
                <tr>
                  <td>
                    @if (editing() === c.id) {
                      <input class="adm-input" [(ngModel)]="editName" name="editName" maxlength="100" aria-label="Emri i kategorisë"
                             (keydown.enter)="saveRename(c)" (keydown.escape)="editing.set(null)">
                    } @else {
                      <strong>{{ c.name }}</strong>
                    }
                  </td>
                  <td>{{ c.brand }}</td>
                  <td class="num">{{ c.productCount }}</td>
                  <td class="num actions">
                    @if (editing() === c.id) {
                      <button type="button" class="adm-btn adm-btn--small" [disabled]="busy()" (click)="saveRename(c)">Ruaj</button>
                      <button type="button" class="adm-btn adm-btn--ghost adm-btn--small" (click)="editing.set(null)">Anulo</button>
                    } @else if (confirming() === c.id) {
                      <span class="ask">Fshi?</span>
                      <button type="button" class="adm-btn adm-btn--small" [disabled]="busy()" (click)="remove(c)">Po</button>
                      <button type="button" class="adm-btn adm-btn--ghost adm-btn--small" (click)="confirming.set(null)">Jo</button>
                    } @else {
                      <button type="button" class="adm-btn adm-btn--ghost adm-btn--small" (click)="startRename(c)">Riemërto</button>
                      <button type="button" class="adm-btn adm-btn--ghost adm-btn--small" (click)="confirming.set(c.id)">Fshi</button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
  styles: `
    .add { display: grid; grid-template-columns: 1fr 220px auto; gap: 0.8rem 1rem; align-items: end; margin-bottom: 1.4rem; }
    .add .adm-field { margin-bottom: 0; }
    .add .adm-btn { height: 44px; }
    .actions { white-space: nowrap; }
    .actions .adm-btn { margin-left: 0.35rem; }
    .ask { margin-left: 0.5rem; color: var(--muted); font-size: 0.85rem; }
    .sk { height: 160px; background: #ece8dd; border-radius: var(--radius-md); }
    @media (max-width: 700px) { .add { grid-template-columns: 1fr; } }
  `
})
export class AdminCategoriesComponent {
  private readonly api = inject(AdminApiService);
  private readonly catalog = inject(CatalogService);

  items = signal<AdminCategory[]>([]);
  brands = signal<Brand[]>([]);
  loading = signal(true);
  failed = signal(false);
  busy = signal(false);
  error = signal('');
  editing = signal<number | null>(null);
  confirming = signal<number | null>(null);

  newName = '';
  newBrandId = 0;
  editName = '';

  constructor() {
    afterNextRender(() => {
      this.catalog.getBrands().pipe(catchError(() => of([] as Brand[]))).subscribe(brands => {
        this.brands.set(brands);
        if (brands.length > 0) this.newBrandId = brands[0].id;
      });
      this.reload();
    });
  }

  create(): void {
    const name = this.newName.trim();
    if (name.length < 2 || this.busy()) return;
    this.run(this.api.createCategory(name, Number(this.newBrandId)), () => { this.newName = ''; this.reload(); });
  }

  startRename(category: AdminCategory): void {
    this.error.set('');
    this.confirming.set(null);
    this.editName = category.name;
    this.editing.set(category.id);
  }

  saveRename(category: AdminCategory): void {
    const name = this.editName.trim();
    if (name.length < 2 || this.busy()) return;
    this.run(this.api.renameCategory(category.id, name), () => { this.editing.set(null); this.reload(); });
  }

  remove(category: AdminCategory): void {
    this.run(this.api.deleteCategory(category.id), () => { this.confirming.set(null); this.reload(); });
  }

  private reload(): void {
    this.api.categories().subscribe({
      next: list => { this.items.set(list); this.loading.set(false); this.failed.set(false); },
      error: () => { this.failed.set(true); this.loading.set(false); }
    });
  }

  private run(request: Observable<unknown>, done: () => void): void {
    this.busy.set(true);
    this.error.set('');
    request.subscribe({
      next: () => { this.busy.set(false); done(); },
      error: (err: unknown) => {
        this.busy.set(false);
        this.confirming.set(null);
        this.error.set(this.message(err));
      }
    });
  }

  private message(err: unknown): string {
    if (!(err instanceof HttpErrorResponse)) return 'Ndodhi një gabim i papritur.';
    if (err.status === 0) return 'Serveri nuk përgjigjet. A është ndezur API-ja?';
    if (typeof err.error?.message === 'string') return err.error.message;
    if (err.error?.errors && typeof err.error.errors === 'object') {
      return (Object.values(err.error.errors).flat() as string[]).join(' ');
    }
    return `Gabim ${err.status}. Provoni përsëri.`;
  }
}
