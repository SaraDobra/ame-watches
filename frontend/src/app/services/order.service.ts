import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateOrderRequest, Order, OrderCreated } from '../models/order.models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/orders`;

  create(request: CreateOrderRequest): Observable<OrderCreated> {
    return this.http.post<OrderCreated>(this.url, request);
  }

  get(orderNo: string, token: string, fresh = false): Observable<Order> {
    return this.http.get<Order>(`${this.url}/${encodeURIComponent(orderNo)}`, { params: { token }, transferCache: !fresh });
  }
}
