export class LauncherOptions {

  public installMethod: InstallMethod;
}

export enum InstallMethod {
  CopyFiles = 0,
  SymLink = 1
}
