using System;
using System.Linq;

namespace GenLauncherWeb.Models;

public class ModData : ModificationReposVersion, IComparable
{
    public bool IsSelected = false;
    public bool Installed = false;

    public override bool Equals(object obj)
    {
        if (obj.GetType() != this.GetType()) return false;

        ModData modData = (ModData)obj;
        return (String.Equals(this.Name + this.Version, modData.Name + modData.Version, StringComparison.CurrentCultureIgnoreCase));
    }

    public override int GetHashCode()
    {
        return (Name.ToUpper() + Version.ToUpper()).GetHashCode();
    }

    public ModData()
    {
    }

    public void UnionModifications(ModData otherModData)
    {
        if (otherModData.IsSelected || this.IsSelected)
            this.IsSelected = true;

        if (otherModData.Installed || this.Installed)
            this.Installed = true;

        if (!String.IsNullOrEmpty(otherModData.SimpleDownloadLink) && String.IsNullOrEmpty(this.SimpleDownloadLink))
            this.SimpleDownloadLink = otherModData.SimpleDownloadLink;

        if (otherModData.ModificationType != ModificationType.Mod && this.ModificationType == ModificationType.Mod)
            this.ModificationType = otherModData.ModificationType;

        if (!String.IsNullOrEmpty(otherModData.UIImageSourceLink) && String.IsNullOrEmpty(this.UIImageSourceLink))
            this.UIImageSourceLink = otherModData.UIImageSourceLink;

        if (!String.IsNullOrEmpty(otherModData.DependenceName) && String.IsNullOrEmpty(this.DependenceName))
            this.DependenceName = otherModData.DependenceName;

        if (!String.IsNullOrEmpty(otherModData.NewsLink) && String.IsNullOrEmpty(this.NewsLink))
            this.NewsLink = otherModData.NewsLink;

        if (!String.IsNullOrEmpty(otherModData.ModDBLink) && String.IsNullOrEmpty(this.ModDBLink))
            this.ModDBLink = otherModData.ModDBLink;

        if (!String.IsNullOrEmpty(otherModData.DiscordLink) && String.IsNullOrEmpty(this.DiscordLink))
            this.DiscordLink = otherModData.DiscordLink;

        if (!String.IsNullOrEmpty(otherModData.NetworkInfo) && String.IsNullOrEmpty(this.NetworkInfo))
            this.NetworkInfo = otherModData.NetworkInfo;

        if (!String.IsNullOrEmpty(otherModData.SupportLink) && String.IsNullOrEmpty(this.SupportLink))
            this.SupportLink = otherModData.SupportLink;

        if (!String.IsNullOrEmpty(otherModData.SupportLink) && String.IsNullOrEmpty(this.SupportLink))
            this.NetworkInfo = otherModData.SupportLink;

        if (!String.IsNullOrEmpty(otherModData.S3BucketName) && String.IsNullOrEmpty(this.S3BucketName))
            this.S3BucketName = otherModData.S3BucketName;

        if (!String.IsNullOrEmpty(otherModData.S3FolderName) && String.IsNullOrEmpty(this.S3FolderName))
            this.S3FolderName = otherModData.S3FolderName;

        if (!String.IsNullOrEmpty(otherModData.S3HostLink) && String.IsNullOrEmpty(this.S3HostLink))
            this.S3HostLink = otherModData.S3HostLink;

        if (!String.IsNullOrEmpty(otherModData.S3HostPublicKey) && String.IsNullOrEmpty(this.S3HostPublicKey))
            this.S3HostPublicKey = otherModData.S3HostPublicKey;

        if (!String.IsNullOrEmpty(otherModData.S3HostSecretKey) && String.IsNullOrEmpty(this.S3HostSecretKey))
            this.S3HostSecretKey = otherModData.S3HostSecretKey;

        this.Deprecated = otherModData.Deprecated;
        /*
        if (otherModificationVersion.ColorsInformation != null)
            this.ColorsInformation = otherModificationVersion.ColorsInformation;
            */
    }

    public int CompareTo(object o)
    {
        ModData mv = o as ModData;
        if (mv != null)
        {
            var thisVersionString = new string(this.Version.ToCharArray().Where(n => n >= '0' && n <= '9').ToArray());
            var otherVersionString = new string(mv.Version.ToCharArray().Where(n => n >= '0' && n <= '9').ToArray());

            while (thisVersionString.Length > otherVersionString.Length)
                otherVersionString += '0';

            while (thisVersionString.Length < otherVersionString.Length)
                thisVersionString += '0';

            if (String.IsNullOrEmpty(thisVersionString)) thisVersionString = "-1";
            if (String.IsNullOrEmpty(otherVersionString)) otherVersionString = "-1";


            var thisVersion = int.Parse(thisVersionString);
            var otherVersion = int.Parse(otherVersionString);

            return thisVersion.CompareTo(otherVersion);
        }
        else
            throw new Exception("Cannot compare 2 objects");
    }

    public ModData(ModificationReposVersion modification)
    {
        this.Name = modification.Name;
        this.Version = modification.Version;
        this.ModificationType = modification.ModificationType;
        this.DependenceName = modification.DependenceName;
        this.ModDBLink = modification.ModDBLink;
        this.DiscordLink = modification.DiscordLink;
        this.SimpleDownloadLink = modification.SimpleDownloadLink;
        this.UIImageSourceLink = modification.UIImageSourceLink;
        this.NewsLink = modification.NewsLink;

        this.NetworkInfo = modification.NetworkInfo;

        this.S3HostLink = modification.S3HostLink;
        this.S3BucketName = modification.S3BucketName;
        this.S3FolderName = modification.S3FolderName;
        this.S3HostPublicKey = modification.S3HostPublicKey;
        this.S3HostSecretKey = modification.S3HostSecretKey;
        this.Deprecated = modification.Deprecated;

        //this.ColorsInformation = modification.ColorsInformation;
        this.SupportLink = modification.SupportLink;
    }
    
    
}