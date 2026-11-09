import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Brand, Category, PagedResult, PriceRange, ProductDetail, ProductFilter, ProductListItem, ProductSuggestion } from '../models/catalog.models';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly url = environment.apiUrl;

  getBrands(): Observable<Brand[]> {
    return this.http.get<Brand[]>(`${this.url}/brands`);
  }

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.url}/categories`);
  }

  getProducts(filter: ProductFilter = {}): Observable<PagedResult<ProductListItem>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filter)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<PagedResult<ProductListItem>>(`${this.url}/products`, { params });
  }

  getProduct(slug: string): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`${this.url}/products/${encodeURIComponent(slug)}`);
  }

  getPriceRange(): Observable<PriceRange> {
    return this.http.get<PriceRange>(`${this.url}/products/price-range`);
  }

  suggest(query: string): Observable<ProductSuggestion[]> {
    return this.http.get<ProductSuggestion[]>(`${this.url}/products/suggest`, { params: { q: query } });
  }
}
