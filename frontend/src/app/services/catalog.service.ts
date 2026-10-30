import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Brand, PagedResult, ProductFilter, ProductListItem } from '../models/catalog.models';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly url = environment.apiUrl;

  getBrands(): Observable<Brand[]> {
    return this.http.get<Brand[]>(`${this.url}/brands`);
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
}
