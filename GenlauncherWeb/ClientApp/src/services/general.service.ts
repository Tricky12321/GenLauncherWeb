import {Injectable} from "@angular/core";
import {HttpClient} from "@angular/common/http";
import {ReposModsData} from "../models/ReposModsData";
import {Mod} from "../models/Mod";
import {ModDownloadProgress} from "../models/ModDownloadProgress";
import {InstallationStatus} from "../models/InstallationStatus";

@Injectable({
  providedIn: 'root',
})
export class GeneralService {
  constructor(private http: HttpClient) {

  }

  getPaths() {
    return this.http.get<{steamInstallPath: string, configPath: string}>('/api/general/paths');
  }

  getModList() {
    return this.http.get<ReposModsData>('/api/general/modlist');
  }


  addMod(modName: string) {
    return this.http.post('/api/general/addMod', {modName: modName} );
  }
  selectMod(modName: string) {
    return this.http.post('/api/general/selectMod', {modName: modName} );
  }

  getAddedMods() {
    return this.http.get<Mod[]>('/api/general/addedMods');
  }

  removeMod(modName: string) {
    return this.http.post('/api/general/removeMod',  {modName: modName});
  }

  downloadMod(modName: string) {
    return this.http.post('/api/general/downloadMod',  {modName: modName});
  }

  uninstallMod(modName: string) {
    return this.http.post('/api/general/uninstallMod',  {modName: modName});
  }

  getModDownloadProgress(modName: string) {
    return this.http.get<ModDownloadProgress>('/api/general/getModDownloadProgress/' + modName);
  }

  installMod(modName: string) {
    return this.http.post('/api/general/installMod',  {modName: modName});
  }

  deleteMod(modName: string) {
    return this.http.post('/api/general/deleteMod',  {modName: modName});
  }

  getInstallationStatus() {
    return this.http.get<InstallationStatus>('/api/general/GetInstallationStatus');
  }

  installGenTool() {
    return this.http.get('/api/general/installGenTool');
  }

  checkSteamPath() {
    return this.http.get('/api/general/checkSteamPath');
  }
}
