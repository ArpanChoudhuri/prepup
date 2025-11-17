import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OrdersService } from './orderlist.service';
import { BehaviorSubject, combineLatest, debounceTime, distinctUntilChanged, switchMap, catchError, of } from 'rxjs';

@Component({
  selector: 'app-orders-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './orderlist.component.html',
  styleUrls: ['./orderlist.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrdersListComponent {
  private search$ = new BehaviorSubject<string>('');
  orders$ = this.search$.pipe(
    debounceTime(300),
    distinctUntilChanged(),
    switchMap(term => this.svc.searchOrders(term)
      .pipe(catchError(err => { console.error(err); return of([]); })))
  );
 trackById = (_: number, item: any) => item.orderId;

  constructor(private svc: OrdersService) {}

  onSearch(term: string) {
    this.search$.next(term);
  }
}
