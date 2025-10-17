// interceptors/auth.interceptor.ts
import {
  HttpInterceptorFn,
  HttpRequest,
  HttpEvent,
  HttpHandlerFn,
  HttpErrorResponse,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, finalize, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from './auth.service';

// module-scoped refresh state (shared across all calls)
const refreshingState = {
  refreshing: false,
  gate: new BehaviorSubject<boolean>(false),
};

export const authInterceptor: HttpInterceptorFn =
  (req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> => {
    const auth = inject(AuthService);
    const withAuth = addAuth(req, auth.token);

    return next(withAuth).pipe(
      catchError(err => {
        if (err instanceof HttpErrorResponse && err.status === 401) {
          return handle401(req, next, auth);
        }
        return throwError(() => err);
      })
    );
  };

// --- helpers ---

function handle401(req: HttpRequest<any>, next: HttpHandlerFn, auth: AuthService): Observable<HttpEvent<any>> {
  if (!refreshingState.refreshing) {
    refreshingState.refreshing = true;
    refreshingState.gate.next(true);

    return auth.refresh().pipe(
      finalize(() => {
        refreshingState.refreshing = false;
        refreshingState.gate.next(false);
      }),
      switchMap(() => next(addAuth(req, auth.token)))
    );
  } else {
    // Wait for the in-flight refresh to finish, then retry with the new token
    return refreshingState.gate.pipe(
      filter(v => v === false),
      take(1),
      switchMap(() => next(addAuth(req, auth.token)))
    );
  }
}

function addAuth(req: HttpRequest<any>, token: string | null): HttpRequest<any> {
  return token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
}
