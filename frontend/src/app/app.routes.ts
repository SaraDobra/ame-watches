import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { CatalogComponent } from './catalog/catalog.component';
import { ProductDetailComponent } from './product/product-detail.component';
import { CartComponent } from './cart/cart.component';
import { CheckoutComponent } from './checkout/checkout.component';
import { OrderConfirmationComponent } from './confirmation/order-confirmation.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'ora', component: CatalogComponent },
  { path: 'ora/:slug', component: ProductDetailComponent },
  { path: 'shporta', component: CartComponent },
  { path: 'porosia', component: CheckoutComponent },
  { path: 'porosia/konfirmim/:orderNo', component: OrderConfirmationComponent },
  { path: '**', redirectTo: '' }
];
