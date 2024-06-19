import {Component, OnInit} from '@angular/core';
import {Router} from "@angular/router";
import {GeneralService} from "../../services/general.service";
import {Mod} from "../../models/Mod";

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit{

  constructor(public router: Router, public generalService: GeneralService) {

  }

  public dtOptions: DataTables.Settings = {
    paging: false,
    scrollY: "600px",
  };
  public addedMods: Mod[] | null = null;

  ngOnInit(): void {
    this.load();
  }

  load() {
    this.addedMods = null;
    this.generalService.getAddedMods().subscribe(success => {
      success.map(x => {
        x.installing = false;
        x.removing = false;
        x.uninstalling = false;
      });
      this.addedMods = success;
    });
  }

  installMod(modName: string) {
    var mod = this.addedMods.find(x => x.modInfo.modName == modName);
    mod.installing = true;
    this.generalService.installMod(modName).subscribe(success => {
      this.load();
    })
  }

  uninstallMod(modName: string) {
    // TODO: Implement confirm dialog
    // TODO: Implement error handling
    var mod = this.addedMods.find(x => x.modInfo.modName == modName);
    mod.uninstalling = true;
    this.generalService.uninstallMod(modName).subscribe(success => {
      this.load();
    })

  }

  removeMod(modName: string) {
    // TODO: Implement confirm dialog
    // TODO: Implement error handling
    var mod = this.addedMods.find(x => x.modInfo.modName == modName);
    mod.removing = true;
    this.generalService.removeMod(modName).subscribe(success => {
      this.load();
    })
  }

  selectMod(modName: string) {
    // TODO: Implement error handling
    this.generalService.selectMod(modName).subscribe(success => {
      this.load();
    })
  }


}
