import {Injectable} from "@angular/core";
import {HttpClient} from "@angular/common/http";

@Injectable({
  providedIn: 'root',
})
export class GeneralService {
  constructor(private http: HttpClient) {

  }

  getSteamPath() {
    return this.http.get<{steamInstallPath: string}>('/api/general/steamInstallPath');
  }
}
