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
    return this.http.get('/api/options');
  }

  setOptions(launcherOptions: LauncherOptions) {
    return this.http.post('/api/options', launcherOptions);
  }


}