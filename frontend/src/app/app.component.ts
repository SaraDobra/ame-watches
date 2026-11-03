import { Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
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

  constructor() {
    this.router.events.pipe(filter(event => event instanceof NavigationEnd), takeUntilDestroyed())
      .subscribe(event => this.isAdmin.set((event as NavigationEnd).urlAfterRedirects.startsWith('/admin')));
  }
}
