export class LauncherOptions {

  public InstallMethod: InstallMethod = InstallMethod.SymLink;
}

export enum InstallMethod {
  MoveFiles,
  CopyFiles,
  SymLink
}
