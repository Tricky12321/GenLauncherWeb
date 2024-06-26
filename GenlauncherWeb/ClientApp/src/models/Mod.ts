import {ModAddonsAndPatches} from "./ModAddonsAndPatches";
import {ModData} from "./ModData";

export interface Mod {
  selected: boolean;
  downloaded: boolean;
  installed: boolean;
  downloadedVersion: string;
  downloadedFiles: string[];
  downloadedSize: number;
  totalSize: number;
  cleanedModName: string;
  modInfo: ModAddonsAndPatches;
  modData: ModData;
  downloading: boolean;
  installing: boolean;
  deleting: boolean;
  uninstalling: boolean;
}
