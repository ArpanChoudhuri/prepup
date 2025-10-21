import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { catchError, debounceTime, distinctUntilChanged, filter, map, of, startWith, switchMap, tap } from 'rxjs';
import { OrderService } from '../features/order/order.service';
import { ToastService } from '../features/toast/toast.service';


type Item = { id: number; name: string };

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
  <section>
    <h2>Products</h2>
    <input type="search" [formControl]="q" placeholder="Type at least 2 chars…" />
    <div *ngIf="vm$ | async as vm">
      <div *ngIf="vm.loading">Loading…</div>
      <div *ngIf="vm.error" class="error">{{ vm.error }}</div>
      <ul>
        <li *ngFor="let p of vm.items; trackBy: trackById">{{ p.name }}</li>
      </ul>
    </div>
  </section>
  <section style="margin-top:1rem">
    <h2>Orders (protected)</h2>
    <button (click)="loadOrders()" [disabled]="ordersLoading">Load Orders</button>
    <div *ngIf="ordersLoading">Loading orders…</div>
    <div *ngIf="ordersError" class="error">{{ ordersError }}</div>
    <ul *ngIf="orders && !ordersLoading">
      <li *ngFor="let o of orders; trackBy: trackByOrderId">
        #{{ o.id }} 
      </li>
    </ul>
  </section>
  <section style="margin-top:1rem">
  <h2>Create Order</h2>

  <button
    (click)="createOrder(1)"
    [disabled]="state === 'submitting' || state === 'queued'"
    [attr.aria-busy]="state === 'submitting' || state === 'queued' ? 'true' : null"
  >
    {{ state === 'submitting' ? 'Submitting…' : state === 'queued' ? 'Queued…' : 'Create Order (Product #1)' }}
  </button>

  <div *ngIf="createdOrderId && state === 'queued'" style="margin-top:.5rem">
    <small>Order #{{ createdOrderId }} is queued. We’re dispatching it shortly…</small>
    <button (click)="checkNow(createdOrderId)">Check now</button>
  </div>

  <div *ngIf="state === 'dispatched'" style="margin-top:.5rem">
    <strong>Order #{{ createdOrderId }} dispatched.</strong>
  </div>

  <div *ngIf="state === 'failed'" class="error" style="margin-top:.5rem">
    {{ ordersError || 'Something went wrong.' }}
    <button (click)="createOrder(1)">Retry</button>
  </div>
</section>
`
  
  
})

export class SearchComponent {

  constructor(
  public ordersApi: OrderService,
  public toasts: ToastService
  ) {}
  private http = inject(HttpClient);
  q = new FormControl('', { nonNullable: true });

  // ... inside component class
  state: 'idle' | 'submitting' | 'queued' | 'dispatched' | 'failed' = 'idle';
  createdOrderId: number | null = null;
  ordersError: string | null = null;

  vm$ = this.q.valueChanges.pipe(
    map(v => v.trim()),
    filter(v => v.length >= 2),
    debounceTime(300),
    distinctUntilChanged(),
    switchMap(term =>
      this.http.get<Item[]>('http://localhost:5134/products', { params: { q: term } }).pipe(
        map(items => ({ items, loading: false as const, error: null as string | null })),
        startWith({ items: [] as Item[], loading: true as const, error: null }),
        catchError(() => of({ items: [] as Item[], loading: false as const, error: 'Request failed' }))
      )
    ),
    startWith({ items: [] as Item[], loading: false as const, error: null })
    
  );
    trackById = (_: number, it: Item) => it.id;

 // --- Orders (protected via interceptor) ---
  orders: Item[] | null = null;
  ordersLoading = false;


  loadOrders() {
    this.ordersLoading = true;
    this.ordersError = null;

    this.http
      .get<Item[]>('http://localhost:5134/orders') // protected endpoint
      .pipe(
        tap({
          next: (res) => {
            this.orders = res ?? [];
            this.ordersLoading = false;
          },
          error: (err) => {
            this.ordersLoading = false;
            this.ordersError =
              err?.status === 401
                ? 'Unauthorized (token missing/expired)'
                : 'Failed to load orders';
          },
        })
      )
      .subscribe();
  }
  trackByOrderId = (_: number, it: Item) => it.id;

  createOrder(productId: number) {
  if (this.state === 'submitting' || this.state === 'queued') return;
  this.state = 'submitting';
  this.ordersError = null;

  this.ordersApi.create$({ tenant: 'SOS1WEB', productId }).subscribe({
    next: (res) => {
      this.createdOrderId = res.id;
      this.state = 'queued';
      this.toasts.info(`Order #${res.id} queued. We’ll notify when dispatched.`);

      this.ordersApi.waitUntilDispatched$(res.id, 2000, 45000).subscribe({
        next: (s) => {
          if (s.dispatched) {
            this.state = 'dispatched';
            this.toasts.success(`Order #${s.id} dispatched ✅`);
          }
        },
        error: () => {
          this.state = 'failed';
          this.toasts.error(`Failed while tracking order #${res.id}`);
        },
        complete: () => {
          if (this.state !== 'dispatched') {
            this.state = 'failed';
            this.toasts.warn(`Order #${res.id} still pending. We’ll keep it queued.`);
          }
        }
      });
    },
    error: (err) => {
      this.state = 'failed';
      this.ordersError = err?.status === 401 ? 'Unauthorized' : 'Create order failed';
      this.toasts.error(this.ordersError);
    }
  });
}
checkNow(id: number | null): void {
  if (id == null) return;
  this.ordersApi.status$(id).subscribe(s => {
    this.toasts.info('Dispatched: ' + s.dispatched);
  });
}

}
