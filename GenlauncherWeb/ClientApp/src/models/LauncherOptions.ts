import {GameType} from "./GameType";

export class LauncherOptions {

  public installMethod: InstallMethod;
  public steamPath: string;
  public selectedGame: GameType;
}

export enum InstallMethod {
  CopyFiles = 0,
  SymLink = 1
}
