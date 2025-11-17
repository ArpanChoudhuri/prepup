import { Routes } from '@angular/router';
import { SearchComponent } from './features/search.component';
import { OrdersListComponent } from './features/orderlist/orderlist.component';

export const routes: Routes = [
{path: '', component: SearchComponent},
{path: 'orders', component: OrdersListComponent}

];
