import {ModAddonsAndPatches} from "./ModAddonsAndPatches";
import {ModData} from "./ModData";

export interface Mod {
  selected: boolean;
  installed: boolean;
  installedVersion: string;
  modInfo: ModAddonsAndPatches;
  modData: ModData;
  installing: boolean;
  removing: boolean;
  uninstalling: boolean;
}
