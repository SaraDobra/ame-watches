import { Component, inject, signal } from '@angular/core';
import { NavigationEnd, NavigationStart, Router, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HeaderComponent } from './shared/header.component';
import { FooterComponent } from './shared/footer.component';
import { ToastsComponent } from './shared/toast.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HeaderComponent, FooterComponent, ToastsComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  private readonly router = inject(Router);

  isAdmin = signal(this.router.url.startsWith('/admin'));

  private readonly positions = new Map<number, number>();
  private lastPath = this.router.url.split(/[?#]/)[0];
  private currentId = 0;

  constructor() {
    this.router.events.pipe(takeUntilDestroyed()).subscribe(event => {
      if (event instanceof NavigationStart) {
        if (typeof window !== 'undefined') this.positions.set(this.currentId, window.scrollY);
        return;
      }

      if (!(event instanceof NavigationEnd)) return;

      this.isAdmin.set(event.urlAfterRedirects.startsWith('/admin'));

      const path = event.urlAfterRedirects.split(/[?#]/)[0];
      const samePage = path === this.lastPath;
      const hasAnchor = event.urlAfterRedirects.includes('#');
      this.lastPath = path;
      this.currentId = event.id;

      if (typeof window !== 'undefined' && !samePage && !hasAnchor) {
        window.scrollTo({ top: 0, behavior: 'instant' });
      }
    });
  }
}
