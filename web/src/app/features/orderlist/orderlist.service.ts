import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, retry, delay, scan, throwError, timer } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class OrdersService {
  constructor(private http: HttpClient) {}

  searchOrders(term: string): Observable<any[]> {
    const url = term
      ? `/api/orders/search?query=${encodeURIComponent(term)}`
      : `/api/orders`;

    return this.http.get<any[]>(url).pipe(
      retry({ count: 3, delay: (e, retryCount) => timer(200 * retryCount) })
    );
  }
}
