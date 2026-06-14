import {ModAddonsAndPatches} from "./ModAddonsAndPatches";
import {ModData} from "./ModData";
import {GameType} from "./GameType";

export interface Mod {
  game: GameType;
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
  imgFailed?: boolean;
}
