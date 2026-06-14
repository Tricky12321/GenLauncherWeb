import {Injectable} from "@angular/core";
import {HttpClient} from "@angular/common/http";
import {ReposModsData} from "../models/ReposModsData";
import {Mod} from "../models/Mod";
import {ModDownloadProgress} from "../models/ModDownloadProgress";
import {InstallationStatus} from "../models/InstallationStatus";
import {DetectedGames} from "../models/DetectedGames";

@Injectable({
  providedIn: 'root',
})
export class GeneralService {
  constructor(private http: HttpClient) {}

  getPaths() {
    return this.http.get<{steamInstallPath: string, configPath: string}>('/api/general/paths');
  }

  getDetectedGames() {
    return this.http.get<DetectedGames>('/api/general/detectedGames');
  }

  getModList() {
    return this.http.get<ReposModsData>('/api/general/modlist');
  }

  addMod(modName: string) {
    return this.http.post('/api/general/addMod', {modName});
  }

  getAddedMods() {
    return this.http.get<Mod[]>('/api/general/addedMods');
  }

  removeMod(modName: string) {
    return this.http.post('/api/general/removeMod', {modName});
  }

  downloadMod(modName: string) {
    return this.http.post('/api/general/downloadMod', {modName});
  }

  uninstallMod(modName: string) {
    return this.http.post('/api/general/uninstallMod', {modName});
  }

  getModDownloadProgress(modName: string) {
    return this.http.get<ModDownloadProgress>('/api/general/getModDownloadProgress/' + modName);
  }

  installMod(modName: string) {
    return this.http.post('/api/general/installMod', {modName});
  }

  deleteMod(modName: string) {
    return this.http.post('/api/general/deleteMod', {modName});
  }

  getInstallationStatus() {
    return this.http.get<InstallationStatus>('/api/general/GetInstallationStatus');
  }

  installGenTool() {
    return this.http.get('/api/general/installGenTool');
  }

  checkSteamPath() {
    return this.http.get<{steamPath: string}>('/api/general/checkSteamPath');
  }

  browseSteamFolder() {
    return this.http.get<{available: boolean, path: string | null}>('/api/general/browseSteamFolder');
  }
}
