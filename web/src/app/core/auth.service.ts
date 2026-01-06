import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, switchMap, tap } from 'rxjs';


@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private token$= new BehaviorSubject<string|null>(null);

  constructor(private http: HttpClient) { }

  get token(): string | null { return this.token$.value; }
  tokenChanges(): Observable<string | null> { return this.token$.asObservable();}

  login(username = 'demo', password = 'demo'): Observable<string> {
    // Use HTTPS to match the API dev server (Kestrel) which serves on https://localhost:44321
    return this.http.post<any>('https://localhost:44321/auth/token', { user: username, password })
      .pipe(
        tap(r => this.token$.next(r.access_token)),
        switchMap(r => of(r.access_token))
      );
  }

  // Refresh = just call token endpoint again (dev-only)
  refresh(): Observable<string> {
    return this.login('demo','demo');
  }

  clear() { this.token$.next(null); }
  
}
