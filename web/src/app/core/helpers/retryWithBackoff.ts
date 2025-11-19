import { Observable, timer, throwError, retry } from 'rxjs';

// Modern RxJS: prefer `retry` with a delay function over `retryWhen`.
// The delay function can return an Observable to pause before retrying,
// or return a `throwError` Observable to stop retrying for non-retriable errors.
export function retryWithBackoff(maxRetries = 3, delayMs = 300) {
  return <T>(source: Observable<T>) =>
    source.pipe(
      retry({
        count: maxRetries,
        delay: (error: any, retryCount: number) => {
          const status = error?.status;
          const retriable = !status || (status >= 500 && status < 600);

          if (!retriable) {
            // stop retrying for client errors (4xx) or other non-retriable errors
            return throwError(() => error);
          }

          // exponential-ish backoff (linear multiplier)
          return timer(delayMs * retryCount);
        }
      })
    );
}
