import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, retry, delay, scan, throwError, timer } from 'rxjs';
import { retryWithBackoff } from '../../core/helpers/retryWithBackoff';

@Injectable({ providedIn: 'root' })
export class OrdersService {
  constructor(private http: HttpClient) {}

  searchOrders(term: string): Observable<any[]> {
    const url = term
      ? `https://localhost:44321/api/Orders/search?query=${encodeURIComponent(term)}`
      : `https://localhost:44321/api/Orders/customer/1`;

    return this.http.get<any[]>(url).pipe(retryWithBackoff(3, 300));
  }


}
