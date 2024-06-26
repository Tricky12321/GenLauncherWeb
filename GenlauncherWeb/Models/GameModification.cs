﻿using System;
using System.Collections.Generic;

namespace GenLauncherWeb.Models;

public class GameModification : ModData
{
    public List<ModData> ModificationVersions { get; set; } = new List<ModData>();

    public int NumberInList { get; set; }

    public GameModification()
    {

    }

    public GameModification(ModData version)
    {
        this.Name = version.Name;
        this.DependenceName = version.DependenceName;
        UpdateModificationData(version);
    }

    public void UpdateModificationData(ModData version)
    {
        if (ModificationVersions.Contains(version))
        {
            var modificationVersion = ModificationVersions[ModificationVersions.IndexOf(version)];
            modificationVersion.UnionModifications(version);

            if (this.ModificationType == ModificationType.Advertising)
                UpdateAdvertising(version);
        }
        else
            ModificationVersions.Add(version);

        if (!this.Installed && version.Installed)
            this.Installed = true;

        this.UnionModifications(version);
    }

    private void UpdateAdvertising(ModData version)
    {
        this.ModDBLink = version.ModDBLink;
        this.NetworkInfo = version.NetworkInfo;
        this.DiscordLink = version.DiscordLink;
        this.SimpleDownloadLink = version.SimpleDownloadLink;
        this.SupportLink = version.SupportLink;
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() != this.GetType()) return false;

        GameModification modification = (GameModification)obj;
        return (String.Equals(this.Name.ToLowerInvariant(), modification.Name.ToLowerInvariant(), StringComparison.CurrentCultureIgnoreCase));
    }

    public override int GetHashCode()
    {
        return Name.ToLowerInvariant().GetHashCode();
    }
}