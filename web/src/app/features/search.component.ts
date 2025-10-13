import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { catchError, debounceTime, distinctUntilChanged, filter, map, of, startWith, switchMap } from 'rxjs';

type Item = { id: number; name: string };

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h2>Products</h2>
    <input type="search" [formControl]="q" placeholder="Type at least 2 chars…" />
    <div *ngIf="vm$ | async as vm">
      <div *ngIf="vm.loading">Loading…</div>
      <div *ngIf="vm.error" class="error">{{ vm.error }}</div>
      <ul>
        <li *ngFor="let p of vm.items; trackBy: trackById">{{ p.name }}</li>
      </ul>
    </div>
  `
})
export class SearchComponent {
  private http = inject(HttpClient);
  q = new FormControl('', { nonNullable: true });

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
}
