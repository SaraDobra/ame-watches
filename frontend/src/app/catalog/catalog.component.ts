import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { SeoService } from '../services/seo.service';
import { catchError, of, switchMap, tap } from 'rxjs';
import { CatalogService } from '../services/catalog.service';
import { Brand, Category, PagedResult, PriceRange, ProductFilter, ProductListItem } from '../models/catalog.models';
import { ProductCardComponent } from '../shared/product-card.component';
import { SearchBoxComponent } from '../shared/search-box.component';
import { PriceRangeComponent } from '../shared/price-range.component';

const SORTS = [
  { value: 'Newest', label: 'Më të rejat' },
  { value: 'PriceAsc', label: 'Çmimi: nga më i ulëti' },
  { value: 'PriceDesc', label: 'Çmimi: nga më i larti' },
  { value: 'Name', label: 'Emri A–Z' }
] as const;

const GENDERS = [
  { value: '', label: 'Të gjitha' },
  { value: 'Women', label: 'Gra' },
  { value: 'Men', label: 'Burra' }
] as const;

@Component({
  selector: 'app-catalog',
  imports: [RouterLink, ProductCardComponent, SearchBoxComponent, PriceRangeComponent],
  templateUrl: './catalog.component.html',
  styleUrl: './catalog.component.scss'
})
export class CatalogComponent implements OnInit {
  private readonly catalog = inject(CatalogService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly seo = inject(SeoService);

  readonly sorts = SORTS;
  readonly genders = GENDERS;

  brands = signal<Brand[]>([]);
  categories = signal<Category[]>([]);
  bounds = signal<PriceRange | null>(null);
  result = signal<PagedResult<ProductListItem> | null>(null);
  filter = signal<ProductFilter>({});
  loading = signal(true);
  failed = signal(false);
  filtersOpen = signal(false);

  ngOnInit(): void {
    this.title.setTitle('Koleksioni | amé watches');
    this.seo.setCanonical('/ora');
    this.meta.updateTag({ name: 'description', content: 'Ora origjinale Casio dhe Q&Q për gra dhe për burra, me garanci 1 vit dhe dërgesë falas.' });

    this.catalog.getBrands().pipe(catchError(() => of([] as Brand[])), takeUntilDestroyed(this.destroyRef))
      .subscribe(brands => this.brands.set(brands));

    this.catalog.getCategories().pipe(catchError(() => of([] as Category[])), takeUntilDestroyed(this.destroyRef))
      .subscribe(list => this.categories.set(list));

    this.catalog.getPriceRange().pipe(catchError(() => of(null)), takeUntilDestroyed(this.destroyRef))
      .subscribe(range => this.bounds.set(range));

    this.route.queryParamMap.pipe(
      tap(params => {
        this.filter.set(this.toFilter(params));
        this.loading.set(true);
        this.failed.set(false);
      }),
      switchMap(() => this.catalog.getProducts(this.filter()).pipe(
        catchError(() => { this.failed.set(true); return of(null); })
      )),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(result => {
      this.result.set(result);
      this.loading.set(false);
    });
  }

  setFilter(changes: Partial<Record<'brand' | 'category' | 'gender' | 'sort' | 'minPrice' | 'maxPrice' | 'search', string | number | null>>): void {
    const queryParams: Record<string, string | number | null> = { page: null };
    for (const [key, value] of Object.entries(changes)) {
      queryParams[key] = value === '' || value === undefined ? null : value;
    }
    this.router.navigate([], { queryParams, queryParamsHandling: 'merge' });
  }

  visibleCategories(): Category[] {
    const brand = this.filter().brand;
    return this.categories().filter(c => !brand || c.brandSlug === brand);
  }

  setPrice(range: { from: number | null; to: number | null }): void {
    this.setFilter({ minPrice: range.from, maxPrice: range.to });
  }

  goToPage(page: number): void {
    this.router.navigate([], { queryParams: { page: page > 1 ? page : null }, queryParamsHandling: 'merge' });
    if (typeof document !== 'undefined') {
      document.querySelector('.results')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  }

  clearFilters(): void {
    this.router.navigate([], { queryParams: {} });
  }

  hasFilters(): boolean {
    const f = this.filter();
    return !!(f.brand || f.category || f.gender || f.minPrice !== undefined || f.maxPrice !== undefined || f.search);
  }

  pages(): number[] {
    const total = this.result()?.totalPages ?? 0;
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  countLabel(): string {
    const total = this.result()?.totalCount ?? 0;
    return total === 1 ? '1 orë' : `${total} ora`;
  }

  private toFilter(params: ParamMap): ProductFilter {
    const number = (key: string): number | undefined => {
      const raw = params.get(key);
      if (raw === null || raw === '') return undefined;
      const value = Number(raw);
      return Number.isNaN(value) ? undefined : value;
    };
    const sort = params.get('sort');
    const gender = params.get('gender');

    return {
      brand: params.get('brand') ?? undefined,
      category: params.get('category') ?? undefined,
      gender: gender === 'Men' || gender === 'Women' || gender === 'Unisex' ? gender : undefined,
      minPrice: number('minPrice'),
      maxPrice: number('maxPrice'),
      search: params.get('search') ?? undefined,
      sort: SORTS.some(s => s.value === sort) ? (sort as ProductFilter['sort']) : 'Newest',
      page: number('page') ?? 1,
      pageSize: 12
    };
  }
}
