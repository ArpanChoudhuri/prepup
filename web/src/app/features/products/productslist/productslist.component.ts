import { Component,ChangeDetectionStrategy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Productmodel, ProductUpdatemodel } from '../productmodel';
import { ProductService } from '../productservice.service';
import { BehaviorSubject,catchError,debounceTime,distinctUntilChanged, of, switchMap, tap } from 'rxjs';
import { ProductaddupdateComponent } from "../productaddupdate/productaddupdate/productaddupdate.component";


@Component({
  selector: 'app-productslist',
  standalone: true,
  imports: [CommonModule, ProductaddupdateComponent],
  templateUrl: './productslist.component.html',
  styleUrls: ['./productslist.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush

})
export class ProductslistComponent implements OnInit {
  search$ = new BehaviorSubject<string>('');
  showAddUpdateForm:boolean = false;

  products$= this.search$.pipe(
    tap(searchTerm => console.log('Search emitted:', searchTerm)),
    debounceTime(300),
    // Removed distinctUntilChanged so refresh triggers work
    tap(() => console.log('Calling getProducts...')),
    switchMap(() => this.productService.getProducts()
      .pipe(
        tap(products => console.log('Products received:', products)),
        catchError(err => {
          console.error('Error fetching products:', err);
          return of([]);
        })
      )
    ));
    trackById = (_: number, item: Productmodel) => item.id;

 
  constructor(private productService: ProductService) {}
  selectedProduct: ProductUpdatemodel | null = null;
  
   onEditNew(product: Productmodel | null) {
    this.showAddUpdateForm = true;
    if (product?.id) {
      this.selectedProduct = {
        productId: product.id,
        name: product.name,
        newPrice: product.price,
     };
    } else {
      this.selectedProduct = {
        productId: '',
        name: '',
        newPrice: 0
      };
    }
   }

   onCLoseAndEdit()
   {
    this.selectedProduct = null;
    this.showAddUpdateForm = false;
   }

  ngOnInit(): void {

  this.search$.next('');
  }

}



