import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { CatalogComponent } from './catalog/catalog.component';
import { ProductDetailComponent } from './product/product-detail.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'ora', component: CatalogComponent },
  { path: 'ora/:slug', component: ProductDetailComponent },
  { path: '**', redirectTo: '' }
];
