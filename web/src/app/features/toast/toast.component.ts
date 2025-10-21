import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, Toast } from './toast.service';
import { Subscription, timer } from 'rxjs';

@Component({
  selector: 'app-toasts',
  standalone: true,
  imports: [CommonModule],
  styles: [`
    .toasts { position: fixed; top: 1rem; right: 1rem; display: grid; gap: .5rem; z-index: 1000; }
    .toast { padding: .5rem .75rem; border-radius: .5rem; box-shadow: 0 4px 10px rgba(0,0,0,.1); color: #111; background: #fff; }
    .toast.success { border-left: 4px solid #16a34a; }
    .toast.error   { border-left: 4px solid #dc2626; }
    .toast.info    { border-left: 4px solid #2563eb; }
    .toast.warning { border-left: 4px solid #d97706; }
  `],
  template: `
    <div class="toasts" aria-live="polite" aria-atomic="true">
      <div *ngFor="let t of list" class="toast {{t.type}}" role="status">
        {{ t.text }}
      </div>
    </div>
  `,
})
export class ToastComponent implements OnDestroy {
  list: Toast[] = [];
  private sub: Subscription;

  constructor(private toasts: ToastService) {
    this.sub = this.toasts.events$.subscribe(t => {
      this.list = [...this.list, t];
      const ms = t.timeoutMs ?? 3000;
      const s = timer(ms).subscribe(() => {
        this.list = this.list.filter(x => x.id !== t.id);
        s.unsubscribe();
      });
    });
  }
  ngOnDestroy() { this.sub.unsubscribe(); }
}
