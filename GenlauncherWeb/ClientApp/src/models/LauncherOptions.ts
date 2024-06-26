export class LauncherOptions {

  public InstallMethod: InstallMethod = InstallMethod.SymLink;
}

export enum InstallMethod {
  CopyFiles,
  SymLink
}
