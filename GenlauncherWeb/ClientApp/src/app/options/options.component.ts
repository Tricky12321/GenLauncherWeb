import {Component, OnInit} from '@angular/core';
import {Router} from "@angular/router";
import {GeneralService} from "../../services/general.service";
import {Mod} from "../../models/Mod";
import {ModDownloadProgress} from "../../models/ModDownloadProgress";
import {OptionsService} from "../../services/options.service";
import {InstallMethod, LauncherOptions} from "../../models/LauncherOptions";
import {ToastrService} from "ngx-toastr";

@Component({
  selector: 'options',
  templateUrl: './options.component.html',
  styleUrls: ['./options.component.css']
})
export class OptionsComponent implements OnInit {
  public launcherOptions: LauncherOptions;
  public isSymlinksSupported: boolean = false;
  public loading: boolean = true;

  constructor(public router: Router, public generalService: GeneralService, public optionsService: OptionsService, public toastrService: ToastrService) {

  }


  ngOnInit(): void {
    this.load();
  }

  load() {
    this.loading = true;
    this.optionsService.getOptions().subscribe(success => {
      this.launcherOptions = success;
      this.loading = false;
    })
    this.optionsService.getIsSymLinksSupported().subscribe(success => {
      this.isSymlinksSupported = success.isSymlinksSupported;
    });
  }

  protected readonly InstallMethod = InstallMethod;

  resetOptions() {
    this.loading = true;
    this.optionsService.resetOptions().subscribe(success => {
      this.loading = false;
      this.launcherOptions = success;
    })
  }

  saveOptions() {
    this.optionsService.setOptions(this.launcherOptions).subscribe(success => {
      this.toastrService.success("Options saved");
    })
  }
}
