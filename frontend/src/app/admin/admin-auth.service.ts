import { HttpClient } from '@angular/common/http';
import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResponse } from './admin.models';

const KEY = 'ame-admin-session';

@Injectable({ providedIn: 'root' })
export class AdminAuthService {
  private readonly http = inject(HttpClient);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly session = signal<LoginResponse | null>(this.read());

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, { email, password }).pipe(
      tap(response => {
        this.session.set(response);
        this.write(response);
      })
    );
  }

  logout(): void {
    this.session.set(null);
    if (!this.isBrowser) return;
    try {
      sessionStorage.removeItem(KEY);
    } catch {
    }
  }

  token(): string | null {
    return this.isLoggedIn() ? this.session()!.token : null;
  }

  isLoggedIn(): boolean {
    const session = this.session();
    return !!session && new Date(session.expiresAt).getTime() > Date.now();
  }

  private read(): LoginResponse | null {
    if (!this.isBrowser) return null;
    try {
      const raw = sessionStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as LoginResponse) : null;
    } catch {
      return null;
    }
  }

  private write(response: LoginResponse): void {
    if (!this.isBrowser) return;
    try {
      sessionStorage.setItem(KEY, JSON.stringify(response));
    } catch {
    }
  }
}
