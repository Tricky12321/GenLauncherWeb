export interface GamePatch {
  patchUrl: string;
  name: string;
  version: string | null;
  uiImageSourceLink: string | null;
  downloaded: boolean;
  installed: boolean;
  downloading: boolean;
  totalSize: number;
  // UI-only state
  _loadingDownload?: boolean;
  _loadingInstall?: boolean;
  _loadingDelete?: boolean;
  _downloadPollActive?: boolean;
  imgFailed?: boolean;
}
