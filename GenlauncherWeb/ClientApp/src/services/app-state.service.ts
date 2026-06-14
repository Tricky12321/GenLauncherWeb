import {Injectable} from "@angular/core";
import {Subject} from "rxjs";

/**
 * Cross-component signals, e.g. the sidebar must refresh the installation status
 * after a mod install changes the modded launcher state.
 */
@Injectable({
  providedIn: 'root',
})
export class AppStateService {
  private statusRefresh = new Subject<void>();
  public statusRefresh$ = this.statusRefresh.asObservable();

  refreshStatus() {
    this.statusRefresh.next();
  }
}
