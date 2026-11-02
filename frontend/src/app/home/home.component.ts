import { Component, OnInit, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CatalogService } from '../services/catalog.service';
import { Brand, ProductListItem } from '../models/catalog.models';
import { ProductCardComponent } from '../shared/product-card.component';
import { RevealDirective } from '../directives/reveal.directive';

@Component({
  selector: 'app-home',
  imports: [RouterLink, ProductCardComponent, RevealDirective],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  private readonly catalog = inject(CatalogService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  featured = signal<ProductListItem[]>([]);
  brands = signal<Brand[]>([]);
  loaded = signal(false);
  failed = signal(false);

  readonly benefits = [
    { icon: 'shield', title: 'Garanci 1 vit', text: 'Çdo orë mbulohet nga garancia e amé watches për një vit të plotë.' },
    { icon: 'truck', title: 'Dërgesë falas', text: 'Porosia mbërrin te dera juaj pa kosto shtesë, në gjithë vendin.' },
    { icon: 'watch', title: 'Ora origjinale', text: 'Shesim vetëm modele autentike Casio dhe Q&Q.' },
    { icon: 'card', title: 'Pagesë e lehtë', text: 'Paguani në dorëzim ose me kartë, sipas zgjedhjes suaj.' }
  ];

  ngOnInit(): void {
    if (!this.isBrowser) return;

    this.catalog.getProducts({ pageSize: 4, sort: 'Newest' }).subscribe({
      next: result => { this.featured.set(result.items); this.loaded.set(true); },
      error: () => { this.failed.set(true); this.loaded.set(true); }
    });
    this.catalog.getBrands().subscribe({ next: brands => this.brands.set(brands) });
  }

  brandCount(slug: string): string {
    const count = this.brands().find(b => b.slug === slug)?.productCount ?? 0;
    return count === 1 ? '1 model' : `${count} modele`;
  }
}
