export class LauncherOptions {

  public installMethod: InstallMethod;
  public steamPath: string;
}

export enum InstallMethod {
  CopyFiles = 0,
  SymLink = 1
}
