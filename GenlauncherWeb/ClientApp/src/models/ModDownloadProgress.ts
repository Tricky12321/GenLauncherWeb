export interface ModDownloadProgress {
  percentage: number;
  totalDownloadSize: number;
  downloadedSize: number;
  fileList: string[];
  downloadedFiles: string[];
  downloaded: boolean;
}
