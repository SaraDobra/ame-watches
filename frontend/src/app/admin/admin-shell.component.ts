import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { LogoComponent } from '../shared/logo.component';
import { AdminAuthService } from './admin-auth.service';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LogoComponent],
  template: `
    <div class="adm">
      <aside class="side">
        <a routerLink="/admin" class="logo" aria-label="Paneli">
          <app-logo variant="light" [size]="17" />
        </a>
        <nav aria-label="Menyja e adminit">
          <a routerLink="/admin" routerLinkActive="on" [routerLinkActiveOptions]="{ exact: true }">Paneli</a>
          <a routerLink="/admin/porosite" routerLinkActive="on">Porositë</a>
          <a routerLink="/admin/produktet" routerLinkActive="on">Produktet</a>
          <a routerLink="/admin/kategorite" routerLinkActive="on">Kategoritë</a>
        </nav>
        <div class="who">
          <span class="name">{{ auth.session()?.fullName }}</span>
          <a href="/" target="_blank" rel="noopener">Shiko dyqanin ↗</a>
          <button type="button" (click)="logout()">Dil</button>
        </div>
      </aside>
      <main class="content">
        <router-outlet />
      </main>
    </div>
  `,
  styles: `
    .adm { display: grid; grid-template-columns: 236px 1fr; min-height: 100vh; background: var(--ivory); }
    .side { background: var(--ink); color: #cfc8b8; padding: 1.6rem 1.2rem; display: flex; flex-direction: column; gap: 2rem; position: sticky; top: 0; height: 100vh; }
    .logo { display: block; padding: 0 0.4rem; text-decoration: none; }
    nav { display: flex; flex-direction: column; gap: 0.2rem; }
    nav a { color: #cfc8b8; text-decoration: none; padding: 0.7rem 0.9rem; font-size: 0.92rem; border-left: 2px solid transparent; transition: background 0.25s var(--ease), color 0.25s var(--ease); }
    nav a:hover { background: rgba(255, 255, 255, 0.06); color: #fff; }
    nav a.on { color: #fff; border-left-color: var(--gold); background: rgba(184, 151, 90, 0.14); }
    .who { margin-top: auto; display: flex; flex-direction: column; gap: 0.55rem; font-size: 0.84rem; padding: 0 0.4rem; }
    .name { color: #fff; }
    .who a { color: var(--gold-light); text-decoration: none; }
    .who a:hover { text-decoration: underline; }
    .who button { align-self: flex-start; background: none; border: 1px solid rgba(255, 255, 255, 0.25); color: #cfc8b8; padding: 0.4rem 0.9rem; cursor: pointer; font: inherit; font-size: 0.82rem; transition: all 0.25s var(--ease); }
    .who button:hover { color: #fff; border-color: #fff; }
    .content { min-width: 0; }
    @media (max-width: 900px) {
      .adm { grid-template-columns: 1fr; }
      .side { position: static; height: auto; flex-direction: row; flex-wrap: wrap; align-items: center; gap: 0.8rem 1.4rem; padding: 0.9rem 1rem; }
      nav { flex-direction: row; flex-wrap: wrap; }
      nav a { border-left: none; border-bottom: 2px solid transparent; padding: 0.4rem 0.6rem; }
      nav a.on { border-bottom-color: var(--gold); }
      .who { margin-top: 0; flex-direction: row; align-items: center; flex-wrap: wrap; gap: 0.8rem; margin-left: auto; }
    }
  `
})
export class AdminShellComponent {
  readonly auth = inject(AdminAuthService);
  private readonly router = inject(Router);

  constructor() {
    inject(Title).setTitle('Paneli | amé watches');
    inject(Meta).updateTag({ name: 'robots', content: 'noindex,nofollow' });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/admin/login']);
  }
}
