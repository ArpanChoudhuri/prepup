import { Injectable } from '@angular/core';
import { Productmodel, ProductUpdatemodel } from './productmodel';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  getProducts() {
    return this.http.get<{ items: Productmodel[], nextAfter: string | null }>('https://localhost:44321/Products')
      .pipe(map(response => response.items));
  }
  
  getProductbyId(id: string) {
    return this.http.get<Productmodel>(`https://localhost:44321/Products/${id}`);
  }
  postProducts(product: Productmodel) {
    return this.http.post<Productmodel>('https://localhost:44321/api/Products', product);
  }

  putProducts(product: ProductUpdatemodel) {
    return this.http.put<ProductUpdatemodel>(`https://localhost:44321/api/Products/${product.productId}/price`, product);
  }

  
  constructor(private http: HttpClient) {

   }
}
