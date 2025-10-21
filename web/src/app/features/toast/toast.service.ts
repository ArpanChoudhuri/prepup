import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';

export type ToastType = 'info' | 'success' | 'error' | 'warning';
export interface Toast {
  id: number;
  type: ToastType;
  text: string;
  timeoutMs?: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private seq = 0;
  private _events = new Subject<Toast>();
  events$: Observable<Toast> = this._events.asObservable();

  show(text: string, type: ToastType = 'info', timeoutMs = 3000) {
    this._events.next({ id: ++this.seq, type, text, timeoutMs });
  }
  success(t: string, ms = 3000) { this.show(t, 'success', ms); }
  error(t: string, ms = 5000) { this.show(t, 'error', ms); }
  warn(t: string, ms = 4000) { this.show(t, 'warning', ms); }
  info(t: string, ms = 3000) { this.show(t, 'info', ms); }
}
