export interface ModData {
  isSelected: boolean;
  installed: boolean;
  modificationType: number;
  name: string;
  version: string;
  simpleDownloadLink: string;
  uiImageSourceLink: string;
  discordLink: string;
  modDBLink: string;
  newsLink: string;
  dependenceName: string;
  s3HostLink: string;
  s3BucketName: string;
  s3FolderName: string;
  s3HostPublicKey: string;
  s3HostSecretKey: string;
  deprecated: boolean;
}
