import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../productservice.service';
import { Productmodel, ProductUpdatemodel } from '../../productmodel';

@Component({
  selector: 'app-productaddupdate',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './productaddupdate.component.html',
  styleUrls: ['./productaddupdate.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductaddupdateComponent  {
 @Input() product: ProductUpdatemodel | null = null;
 @Output() close = new EventEmitter<void>();
 @Output() save = new EventEmitter<void>();
  constructor(private productService: ProductService) {}


  onSave(product:ProductUpdatemodel) {
    if (this.product) {
      // Update existing product  
      this.productService.putProducts(product).subscribe({
        next: updatedProduct => {
          console.log('Product updated:', updatedProduct);
          // Handle success
          this.save.emit();
          this.close.emit();
        },
        error: error => {
          console.error('Error updating product:', error);
          // Handle error
        }
      });
    } 
   else {
      // Add new product
      this.productService.postProducts(product as Productmodel).subscribe({
        next: newProduct => {
          console.log('Product added:', newProduct);
          // Handle success
          this.save.emit();
          this.close.emit();
        },
        error: error => {
          console.error('Error adding product:', error);
          // Handle error
        }
      });
    }

  }
  }