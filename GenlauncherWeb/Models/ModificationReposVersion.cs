// Decompiled with JetBrains decompiler
// Type: GenLauncherNet.ModificationReposVersion
// Assembly: GenLauncher, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A29188F0-3347-4599-8FE4-671DB486C1A9
// Assembly location: C:\Users\Administrator\Desktop\GenLauncher.exe

using System;
using System.ComponentModel.DataAnnotations.Schema;


namespace GenLauncherWeb.Models
{
    public class ModificationReposVersion
    {
        public ModificationType ModificationType { get; set; }

        public string Name { get; set; }

        public string Version { get; set; }

        public string SimpleDownloadLink { get; set; }

        public string UIImageSourceLink { get; set; }

        public string DiscordLink { get; set; }

        public string ModDBLink { get; set; }

        public string NewsLink { get; set; }

        public string DependenceName { get; set; }

        public string S3HostLink { get; set; }

        public string S3BucketName { get; set; }

        public string S3FolderName { get; set; }

        public string S3HostPublicKey { get; set; }

        public string S3HostSecretKey { get; set; }

        public string NetworkInfo { get; set; }

        public bool Deprecated { get; set; }

        public string SupportLink { get; set; }

        public ModificationReposVersion()
        {
        }

        public ModificationReposVersion(
            string name,
            string version,
            string downloadLink = null,
            string imageSource = null)
        {
            this.Name = name;
            this.Version = version;
            this.UIImageSourceLink = imageSource;
            this.SimpleDownloadLink = downloadLink;
        }

        public ModificationReposVersion(string name) => this.Name = name;

        public override bool Equals(object obj)
        {
            return (!(obj.GetType() != this.GetType()) || obj is ModData) && string.Equals(this.Name, ((ModificationReposVersion)obj).Name, StringComparison.CurrentCultureIgnoreCase);
        }

        public override int GetHashCode() => this.Name.ToUpper().GetHashCode();

        public override string ToString() => this.Name;
    }
}