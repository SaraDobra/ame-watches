import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { HttpErrorResponse } from '@angular/common/http';
import { LogoComponent } from '../../shared/logo.component';
import { AdminAuthService } from '../admin-auth.service';

@Component({
  selector: 'app-admin-login',
  imports: [ReactiveFormsModule, LogoComponent],
  template: `
    <section class="wrap">
      <form class="box" [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <app-logo [size]="20" />
        <h1>Hyrja në panel</h1>

        @if (expired) {
          <p class="adm-warn" role="status">Sesioni skadoi. Hyni përsëri.</p>
        }
        @if (error()) {
          <p class="adm-errors err" role="alert">{{ error() }}</p>
        }

        <div class="adm-field" [class.adm-field--bad]="bad('email')">
          <label for="email">Email</label>
          <input id="email" type="email" formControlName="email" autocomplete="username">
          @if (bad('email')) { <small class="err">Shkruani një email të vlefshëm.</small> }
        </div>

        <div class="adm-field" [class.adm-field--bad]="bad('password')">
          <label for="password">Fjalëkalimi</label>
          <div class="pw">
            <input id="password" [type]="show() ? 'text' : 'password'" formControlName="password" autocomplete="current-password">
            <button type="button" (click)="show.set(!show())" [attr.aria-label]="show() ? 'Fshih fjalëkalimin' : 'Shfaq fjalëkalimin'">{{ show() ? 'Fshih' : 'Shfaq' }}</button>
          </div>
          @if (bad('password')) { <small class="err">Shkruani fjalëkalimin.</small> }
        </div>

        <button type="submit" class="adm-btn go" [disabled]="working()">{{ working() ? 'Duke hyrë...' : 'Hyr' }}</button>
      </form>
    </section>
  `,
  styles: `
    .wrap { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 1.5rem; background: radial-gradient(ellipse at 70% 20%, #2e2820, #0c0c0d 65%); }
    .box { width: 100%; max-width: 400px; background: var(--paper); padding: 2.2rem 2rem; display: flex; flex-direction: column; gap: 0.2rem; animation: rise 0.7s var(--ease) both; box-shadow: 0 40px 80px -30px rgba(0, 0, 0, 0.7); }
    h1 { font-size: 1.7rem; margin: 1.4rem 0 1rem; font-weight: 500; }
    .pw { position: relative; display: flex; }
    .pw input { flex: 1; padding-right: 4.6rem; }
    .pw button { position: absolute; right: 0.4rem; top: 50%; transform: translateY(-50%); background: none; border: none; color: var(--gold-dark); cursor: pointer; font: inherit; font-size: 0.82rem; padding: 0.3rem 0.5rem; }
    .go { justify-content: center; margin-top: 0.6rem; padding: 0.85rem; }
    .err { margin-bottom: 0.8rem; }
  `
})
export class AdminLoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AdminAuthService);
  private readonly router = inject(Router);

  readonly expired = inject(ActivatedRoute).snapshot.queryParamMap.has('expired');
  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });
  working = signal(false);
  error = signal('');
  show = signal(false);

  constructor() {
    inject(Title).setTitle('Hyrja | amé watches');
    inject(Meta).updateTag({ name: 'robots', content: 'noindex,nofollow' });
  }

  bad(name: 'email' | 'password'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  submit(): void {
    this.error.set('');
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.working.set(true);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email.trim(), password).subscribe({
      next: () => this.router.navigate(['/admin']),
      error: (err: unknown) => {
        this.working.set(false);
        const status = err instanceof HttpErrorResponse ? err.status : 0;
        this.error.set(
          status === 401 ? 'Email-i ose fjalëkalimi nuk është i saktë.'
          : status === 423 ? 'Llogaria është e mbyllur përkohësisht pas shumë përpjekjeve. Provoni pas 15 minutash.'
          : status === 429 ? 'Shumë përpjekje në pak kohë. Prisni një minutë.'
          : status === 0 ? 'Serveri nuk përgjigjet. A është ndezur API-ja?'
          : 'Ndodhi një gabim. Provoni përsëri.'
        );
      }
    });
  }
}
