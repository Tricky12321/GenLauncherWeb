import {Injectable} from "@angular/core";
import {EMPTY, Observable, timer} from "rxjs";
import {catchError, switchMap, takeWhile} from "rxjs/operators";

/**
 * Centralizes the "poll an endpoint until it reports done" pattern used for download
 * progress. Transient errors are swallowed (the next tick retries); the observable
 * completes once `isDone` returns true, emitting that final value. Callers should pair
 * this with `takeUntilDestroyed` (or unsubscribe) so polling stops with the component.
 */
@Injectable({
  providedIn: 'root',
})
export class ProgressPollingService {
  poll<T>(fetch: () => Observable<T>, isDone: (value: T) => boolean, intervalMs: number = 500): Observable<T> {
    return timer(0, intervalMs).pipe(
      switchMap(() => fetch().pipe(catchError(() => EMPTY))),
      takeWhile(value => !isDone(value), true),
    );
  }
}
