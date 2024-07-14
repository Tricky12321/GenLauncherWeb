import {Component, OnInit} from '@angular/core';
import {Router} from "@angular/router";
import {GeneralService} from "../../services/general.service";
import {Mod} from "../../models/Mod";
import {ModDownloadProgress} from "../../models/ModDownloadProgress";

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  public downloadProgress: ModDownloadProgress | null = null;

  constructor(public router: Router, public generalService: GeneralService) {

  }

  public dtOptions: DataTables.Settings = {
    paging: false,
    scrollY: "600px",
  };

  public addedMods: Mod[] | null = null;
  public downloadingMod: Mod | null = null;
  public lockButtons: boolean = false;

  ngOnInit(): void {
    this.load();
  }

  load() {
    this.addedMods = null;
    this.generalService.getAddedMods().subscribe(success => {
      success.map(x => {
        x.downloading = false;
        x.deleting = false;
        x.uninstalling = false;
      });
      this.addedMods = success;
      this.lockButtons = false;
    });
  }

  downloadMod(modName: string) {
    this.lockButtons = true;
    var mod = this.addedMods.find(x => x.modInfo.modName == modName);
    this.downloadingMod = mod;
    mod.downloading = true;
    this.generalService.downloadMod(modName).subscribe(success => {
      this.load();
    })
    this.getDownloadProgress(mod.cleanedModName);
  }

  getDownloadProgress(modName: string) {
    this.generalService.getModDownloadProgress(modName).subscribe(success => {
      console.log(success);
      this.downloadProgress = success;
      if (this.downloadProgress.downloaded == false) {
        setTimeout(() => {
          this.getDownloadProgress(modName)
        }, 500);
      } else {
        this.downloadProgress = null;
      }
    });
  }

  uninstallMod(modName: string) {
    this.lockButtons = true;
    // TODO: Implement confirm dialog
    // TODO: Implement error handling
    var mod = this.addedMods.find(x => x.modInfo.modName == modName);
    mod.uninstalling = true;
    this.generalService.uninstallMod(modName).subscribe(success => {
      location.reload();
    })
  }

  removeMod(modName: string) {
    this.lockButtons = true;
    // TODO: Implement confirm dialog
    // TODO: Implement error handling
    var mod = this.addedMods.find(x => x.modInfo.modName == modName);
    mod.deleting = true;
    this.generalService.removeMod(modName).subscribe(success => {
      this.load();
    })
  }


  installMod(modName: string) {
    this.lockButtons = true;
    this.generalService.installMod(modName).subscribe(success => {
      location.reload();
    })
  }

  protected readonly Math = Math;

  deleteMod(modName: string) {
    this.lockButtons = true;
    this.generalService.deleteMod(modName).subscribe(success => {
      this.load();
    })

  }

  anyModInstalled() {
    return this.addedMods.filter(x => x.installed).length > 0;
  }
}
