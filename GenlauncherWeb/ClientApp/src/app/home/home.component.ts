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
    this.generalService.getAddedMods().subscribe(data => {
      this.addedMods = data;
    });
  }

  InstallMod(modName: string) {
    this.generalService.installMod(modName).subscribe(success => {
      this.load();
    })
  }

  UninstallMod(modName: string) {
    // TODO: Implement confirm dialog
    // TODO: Implement error handling

    this.generalService.uninstallMod(modName).subscribe(success => {
      this.load();
    })

  }

  RemoveMod(modName: string) {
    // TODO: Implement confirm dialog
    // TODO: Implement error handling
    this.generalService.removeMod(modName).subscribe(success => {
      this.load();
    })
  }

  SelectMod(modName: string) {
    // TODO: Implement error handling
    this.generalService.selectMod(modName).subscribe(success => {
      this.load();
    })
  }
}
