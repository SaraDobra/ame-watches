import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { CatalogComponent } from './catalog/catalog.component';
import { ProductDetailComponent } from './product/product-detail.component';
import { CartComponent } from './cart/cart.component';
import { CheckoutComponent } from './checkout/checkout.component';
import { OrderConfirmationComponent } from './confirmation/order-confirmation.component';
import { LegalPageComponent } from './legal/legal-page.component';
import { PaymentSimulatorComponent } from './payment/simulator.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'ora', component: CatalogComponent },
  { path: 'ora/:slug', component: ProductDetailComponent },
  { path: 'shporta', component: CartComponent },
  { path: 'porosia', component: CheckoutComponent },
  { path: 'porosia/konfirmim/:orderNo', component: OrderConfirmationComponent },
  { path: 'privatesia', component: LegalPageComponent, data: { slug: 'privatesia' } },
  { path: 'kushtet', component: LegalPageComponent, data: { slug: 'kushtet' } },
  { path: 'dergesa-pagesa', component: LegalPageComponent, data: { slug: 'dergesa-pagesa' } },
  { path: 'garancia-kthimi', component: LegalPageComponent, data: { slug: 'garancia-kthimi' } },
  { path: 'admin', loadChildren: () => import('./admin/admin.routes').then(m => m.ADMIN_ROUTES) },
  { path: 'pagesa/simulator/:sessionId', component: PaymentSimulatorComponent },
  { path: '**', redirectTo: '' }
];
