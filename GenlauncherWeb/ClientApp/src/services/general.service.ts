import {Injectable} from "@angular/core";
import {HttpClient} from "@angular/common/http";
import {ReposModsData} from "../models/ReposModsData";

@Injectable({
  providedIn: 'root',
})
export class GeneralService {
  constructor(private http: HttpClient) {

  }

  getSteamPath() {
    return this.http.get<{steamInstallPath: string}>('/api/general/steamInstallPath');
  }

  getModList() {
    return this.http.get<ReposModsData>('/api/general/modlist');
  }

  installMod(modId: number) {
    return this.http.get('/api/general/installMod/' + modId);
  }
}
