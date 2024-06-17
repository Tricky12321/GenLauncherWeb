import {ModAddonsAndPatches} from "./ModAddonsAndPatches";

export interface Mod {
  selected: boolean;
  installed: boolean;
  installedVersion: string;
  modInfo: ModAddonsAndPatches;
}
