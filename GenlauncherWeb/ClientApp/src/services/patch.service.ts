import {Injectable} from "@angular/core";
import {HttpClient} from "@angular/common/http";
import {GamePatch} from "../models/GamePatch";
import {ModDownloadProgress} from "../models/ModDownloadProgress";

@Injectable({
  providedIn: 'root',
})
export class PatchService {
  constructor(private http: HttpClient) {}

  getPatches() {
    return this.http.get<GamePatch[]>('/api/patch');
  }

  downloadPatch(patchUrl: string) {
    return this.http.post('/api/patch/download', {patchUrl});
  }

  getProgress(patchUrl: string) {
    return this.http.get<ModDownloadProgress>('/api/patch/progress', {params: {patchUrl}});
  }

  installPatch(patchUrl: string) {
    return this.http.post('/api/patch/install', {patchUrl});
  }

  uninstallPatch(patchUrl: string) {
    return this.http.post('/api/patch/uninstall', {patchUrl});
  }

  deletePatch(patchUrl: string) {
    return this.http.post('/api/patch/delete', {patchUrl});
  }
}
