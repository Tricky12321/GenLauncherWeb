import {Injectable} from "@angular/core";
import {HttpClient} from "@angular/common/http";
import {LauncherOptions} from "../models/LauncherOptions";

@Injectable({
  providedIn: 'root',
})
export class OptionsService {
  constructor(private http: HttpClient) {

  }

  getOptions() {
    return this.http.get<LauncherOptions>('/api/options');
  }

  setOptions(launcherOptions: LauncherOptions) {
    return this.http.post<LauncherOptions>('/api/options', launcherOptions);
  }

  resetOptions() {
    return this.http.get<LauncherOptions>('/api/options/reset');
  }

  getIsSymLinksSupported() {
    return this.http.get<{ symlinkSupported: boolean }>('/api/options/isSymlinksSupported');
  }


}
