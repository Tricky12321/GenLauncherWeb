import {ModAddonsAndPatches} from "./ModAddonsAndPatches";
import {GameType} from "./GameType";

export interface ReposModsData {
  game: GameType;
  launcherVersion: string;
  downloadLink: string;
  modDatas: ModAddonsAndPatches[];
  globalAddonsData: string[];
  originalGameAddons: string[];
  originalGamePatches: string[];
}
