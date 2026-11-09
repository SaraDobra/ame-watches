import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AdminOrderDetail, AdminOrderListItem, AdminProductDetail, AdminProductListItem,
  DashboardSummary, Paged, ProductInput
} from './admin.models';

function toParams(values: Record<string, string | number | boolean | null | undefined>): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(values)) {
    if (value !== null && value !== undefined && value !== '') params = params.set(key, String(value));
  }
  return params;
}

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/admin`;

  dashboard(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.url}/dashboard`);
  }

  orders(query: { status?: string; search?: string; page?: number; pageSize?: number }): Observable<Paged<AdminOrderListItem>> {
    return this.http.get<Paged<AdminOrderListItem>>(`${this.url}/orders`, { params: toParams(query) });
  }

  order(orderNo: string): Observable<AdminOrderDetail> {
    return this.http.get<AdminOrderDetail>(`${this.url}/orders/${encodeURIComponent(orderNo)}`);
  }

  changeStatus(orderNo: string, status: string): Observable<AdminOrderDetail> {
    return this.http.put<AdminOrderDetail>(`${this.url}/orders/${encodeURIComponent(orderNo)}/status`, { status });
  }

  products(query: { search?: string; isActive?: boolean | null; page?: number; pageSize?: number }): Observable<Paged<AdminProductListItem>> {
    return this.http.get<Paged<AdminProductListItem>>(`${this.url}/products`, { params: toParams(query) });
  }

  product(id: number): Observable<AdminProductDetail> {
    return this.http.get<AdminProductDetail>(`${this.url}/products/${id}`);
  }

  createProduct(input: ProductInput): Observable<AdminProductDetail> {
    return this.http.post<AdminProductDetail>(`${this.url}/products`, input);
  }

  updateProduct(id: number, input: ProductInput): Observable<AdminProductDetail> {
    return this.http.put<AdminProductDetail>(`${this.url}/products/${id}`, input);
  }

  setActive(id: number, isActive: boolean): Observable<AdminProductDetail> {
    return this.http.patch<AdminProductDetail>(`${this.url}/products/${id}/active`, { isActive });
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/products/${id}`);
  }

  uploadImages(id: number, files: File[]): Observable<AdminProductDetail> {
    const form = new FormData();
    files.forEach(file => form.append('files', file, file.name));
    return this.http.post<AdminProductDetail>(`${this.url}/products/${id}/images`, form);
  }

  deleteImage(id: number, imageId: number): Observable<AdminProductDetail> {
    return this.http.delete<AdminProductDetail>(`${this.url}/products/${id}/images/${imageId}`);
  }

  reorderImages(id: number, imageIds: number[]): Observable<AdminProductDetail> {
    return this.http.put<AdminProductDetail>(`${this.url}/products/${id}/images/order`, { imageIds });
  }
}
