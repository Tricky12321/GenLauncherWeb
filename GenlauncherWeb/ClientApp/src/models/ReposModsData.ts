import {ModAddonsAndPatches} from "./ModAddonsAndPatches";

export interface ReposModsData {
  launcherVersion: string;
  downloadLink: string;
  modDatas: ModAddonsAndPatches[];
  globalAddonsData: string[];
  originalGameAddons: string[];
  originalGamePatches: string[];
}
