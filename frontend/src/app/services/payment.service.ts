import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PaymentSessionInfo } from '../models/order.models';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/payments/simulator`;

  getMethods(): Observable<{ card: boolean }> {
    return this.http.get<{ card: boolean }>(`${environment.apiUrl}/payments/methods`);
  }

  getSimulatorSession(sessionId: string): Observable<PaymentSessionInfo> {
    return this.http.get<PaymentSessionInfo>(`${this.url}/${encodeURIComponent(sessionId)}`);
  }

  completeSimulator(sessionId: string, outcome: 'Paid' | 'Failed'): Observable<{ status: string }> {
    return this.http.post<{ status: string }>(`${this.url}/${encodeURIComponent(sessionId)}/complete`, { outcome });
  }
}
