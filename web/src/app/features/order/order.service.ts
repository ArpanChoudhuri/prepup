import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, switchMap, timer, takeWhile, catchError, of } from 'rxjs';

export interface OrderCreate { tenant: string; productId: number; }
export interface OrderAccepted { id: number; }   // from POST /orders Accepted payload
export interface OrderStatus { id: number; dispatched: boolean; }

@Injectable({ providedIn: 'root' })
export class OrderService {
  private http = inject(HttpClient);
  private api = 'http://localhost:5134';

  create$(input: OrderCreate): Observable<OrderAccepted> {
    return this.http.post<OrderAccepted>(`${this.api}/orders`, input, { observe: 'body' });
  }

  status$(id: number): Observable<OrderStatus> {
    return this.http.get<OrderStatus>(`${this.api}/orders/${id}/status`);
  }

  // Poll until dispatched or timeout
  waitUntilDispatched$(id: number, everyMs = 2000, maxMs = 60000): Observable<OrderStatus> {
    const maxTicks = Math.ceil(maxMs / everyMs);
    return timer(0, everyMs).pipe(
      switchMap(() => this.status$(id).pipe(catchError(() => of({ id, dispatched: false } as OrderStatus)))),
      takeWhile((s, idx) => !s.dispatched && idx < maxTicks, true) // include final emission
    );
  }
}
