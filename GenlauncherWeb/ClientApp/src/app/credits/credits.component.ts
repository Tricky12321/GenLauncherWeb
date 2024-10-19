import {Component, OnInit} from '@angular/core';
import {Router} from "@angular/router";
import {GeneralService} from "../../services/general.service";
import {Mod} from "../../models/Mod";
import {ModDownloadProgress} from "../../models/ModDownloadProgress";
import {OptionsService} from "../../services/options.service";
import {InstallMethod, LauncherOptions} from "../../models/LauncherOptions";
import {ToastrService} from "ngx-toastr";

@Component({
  selector: 'credits',
  templateUrl: './credits.component.html',
  styleUrls: ['./credits.component.css']
})
export class CreditsComponent implements OnInit {
  constructor(public router: Router, public generalService: GeneralService, public optionsService: OptionsService, public toastrService: ToastrService) {

  }


  ngOnInit(): void {
    this.load();
  }

  load() {
  }

}
