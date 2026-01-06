import { Routes } from "@angular/router";
import { ProductslistComponent } from "./productslist/productslist.component";

export const PRODUCTS_ROUTES: Routes = [
    {
        path: '',
        component: ProductslistComponent
    }
]