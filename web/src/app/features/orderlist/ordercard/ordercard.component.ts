import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';

interface OrderItem {
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface Order {
  id: number | string;
  status: string;
  customerName: string;
  totalAmount: number;
  items: OrderItem[];
}

@Component({
  standalone: true,
  selector: 'app-ordercard',
  imports: [CommonModule, CurrencyPipe],
  template: `
    <article class="order-card">
      <header>
        Order #{{ order.id }} – {{ order.status }}
      </header>
      <p>Customer: {{ order.customerName }}</p>
      <p>Total: {{ order.totalAmount | currency }}</p>
      <ul>
        <li *ngFor="let item of order.items">
          {{ item.productName }} x {{ item.quantity }} ({{ item.unitPrice | currency }})
        </li>
      </ul>
    </article>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrdercardComponent {
  @Input() order!: Order;
}
