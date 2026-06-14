import {Component, OnInit} from '@angular/core';
import {GeneralService} from "../../services/general.service";
import {OptionsService} from "../../services/options.service";
import {InstallMethod, LauncherOptions} from "../../models/LauncherOptions";
import {ToastrService} from "ngx-toastr";
import {errorMessage} from "../util";

@Component({
    selector: 'options',
    templateUrl: './options.component.html',
    styleUrls: ['./options.component.css'],
    standalone: false
})
export class OptionsComponent implements OnInit {
  public launcherOptions: LauncherOptions;
  public isSymlinksSupported: boolean = false;
  public loading: boolean = true;

  protected readonly InstallMethod = InstallMethod;

  constructor(public generalService: GeneralService, public optionsService: OptionsService, public toastrService: ToastrService) {
  }

  ngOnInit(): void {
    this.load();
  }

  load() {
    this.loading = true;
    this.optionsService.getOptions().subscribe({
      next: options => {
        this.launcherOptions = options;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        this.toastrService.error(errorMessage(err), 'Could not load options');
      }
    });
    this.optionsService.getIsSymLinksSupported().subscribe({
      next: result => this.isSymlinksSupported = result.symlinkSupported,
      error: () => this.isSymlinksSupported = false
    });
  }

  resetOptions() {
    this.loading = true;
    this.optionsService.resetOptions().subscribe({
      next: options => {
        this.loading = false;
        this.launcherOptions = options;
        this.toastrService.success('Options reset to defaults');
      },
      error: err => {
        this.loading = false;
        this.toastrService.error(errorMessage(err), 'Reset failed');
      }
    });
  }

  saveOptions() {
    this.loading = true;
    this.optionsService.setOptions(this.launcherOptions).subscribe({
      next: options => {
        this.loading = false;
        this.launcherOptions = options;
        this.toastrService.success('Options saved');
      },
      error: err => {
        this.loading = false;
        this.toastrService.error(errorMessage(err), 'Save failed');
      }
    });
  }

  updateInstallMethod(event: Event) {
    const target = event.target as HTMLInputElement;
    this.launcherOptions.installMethod = target.value == "1" ? InstallMethod.SymLink : InstallMethod.CopyFiles;
  }

  browseSteamFolder() {
    this.generalService.browseSteamFolder().subscribe({
      next: result => {
        if (result.available && result.path) {
          this.launcherOptions.steamPath = result.path;
        }
      },
      error: err => this.toastrService.error(errorMessage(err), 'Browse failed')
    });
  }
}
