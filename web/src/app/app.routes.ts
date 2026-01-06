import { Routes } from '@angular/router';
import { SearchComponent } from './features/search.component';
import { OrdersListComponent } from './features/orderlist/orderlist.component';

export const routes: Routes = [
{path: '', component: SearchComponent},
{path: 'orders', 
    loadChildren: () => import('./features/orderlist/orderlist.routes').then(m => m.ORDERS_ROUTES)},
{path: 'products', 
    loadChildren:() => import('./features/products/products.route').then(m => m.PRODUCTS_ROUTES)
}
];
