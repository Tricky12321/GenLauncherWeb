import {Component, OnDestroy, OnInit, TemplateRef, ViewChild} from '@angular/core';
import {GeneralService} from "../../services/general.service";
import {AppStateService} from "../../services/app-state.service";
import {Mod} from "../../models/Mod";
import {ModDownloadProgress} from "../../models/ModDownloadProgress";
import {ToastrService} from 'ngx-toastr';
import {NgbModal} from "@ng-bootstrap/ng-bootstrap";
import {errorMessage} from "../util";
import {GameType, gameDisplayName} from "../../models/GameType";

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.css'],
    standalone: false
})
export class HomeComponent implements OnInit, OnDestroy {
  public addedMods: Mod[] | null = null;
  public steamPathError: string | null = null;
  public lockButtons: boolean = false;

  public downloadingMod: Mod | null = null;
  public downloadProgress: ModDownloadProgress | null = null;
  public downloadSpeed: string | null = null;

  public confirmTitle: string = '';
  public confirmMessage: string = '';
  public confirmButton: string = 'Confirm';
  private confirmAction: (() => void) | null = null;

  @ViewChild('confirmDialog') confirmDialog: TemplateRef<any>;

  protected readonly GameType = GameType;
  protected readonly gameDisplayName = gameDisplayName;

  private pollTimer: any = null;
  private lastSample: { bytes: number, time: number } | null = null;

  constructor(public generalService: GeneralService,
              private appState: AppStateService,
              private modalService: NgbModal,
              public toastrService: ToastrService) {
  }

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    if (this.pollTimer != null) {
      clearTimeout(this.pollTimer);
    }
  }

  load() {
    this.addedMods = null;
    this.steamPathError = null;
    this.generalService.checkSteamPath().subscribe({
      next: () => this.fetchMods(),
      error: (err) => {
        this.steamPathError = errorMessage(err);
        this.addedMods = [];
      }
    });
  }

  private fetchMods() {
    this.generalService.getAddedMods().subscribe({
      next: mods => {
        mods.forEach(x => {
          x.downloading = false;
          x.deleting = false;
          x.uninstalling = false;
          x.installing = false;
        });
        this.addedMods = mods;
        this.lockButtons = false;
      },
      error: err => {
        this.addedMods = [];
        this.toastrService.error(errorMessage(err), 'Could not load mods');
      }
    });
  }

  anyModInstalled(): boolean {
    return this.addedMods != null && this.addedMods.some(x => x.installed);
  }

  // ------------------------------------------------------------- download

  downloadMod(mod: Mod) {
    this.lockButtons = true;
    mod.downloading = true;
    this.downloadingMod = mod;
    this.downloadProgress = null;
    this.downloadSpeed = null;
    this.lastSample = null;

    this.generalService.downloadMod(mod.modInfo.modName).subscribe({
      next: () => {
        this.toastrService.success(mod.modInfo.modName + ' downloaded');
        this.finishDownload(mod);
      },
      error: err => {
        this.toastrService.error(errorMessage(err), 'Download failed');
        this.finishDownload(mod);
      }
    });

    this.pollProgress(mod);
  }

  private finishDownload(mod: Mod) {
    mod.downloading = false;
    this.downloadingMod = null;
    this.downloadProgress = null;
    this.downloadSpeed = null;
    this.lastSample = null;
    if (this.pollTimer != null) {
      clearTimeout(this.pollTimer);
      this.pollTimer = null;
    }
    this.appState.refreshStatus();
    this.fetchMods();
  }

  private pollProgress(mod: Mod) {
    this.generalService.getModDownloadProgress(mod.cleanedModName).subscribe({
      next: progress => {
        if (!mod.downloading) {
          return;
        }
        this.downloadProgress = progress;
        this.updateSpeed(progress);
        if (!progress.downloaded) {
          this.pollTimer = setTimeout(() => this.pollProgress(mod), 500);
        }
      },
      error: () => {
        if (mod.downloading) {
          this.pollTimer = setTimeout(() => this.pollProgress(mod), 1000);
        }
      }
    });
  }

  private updateSpeed(progress: ModDownloadProgress) {
    const now = Date.now();
    if (this.lastSample != null && progress.downloadedSize > this.lastSample.bytes) {
      const seconds = (now - this.lastSample.time) / 1000;
      if (seconds > 0) {
        const bytesPerSecond = (progress.downloadedSize - this.lastSample.bytes) / seconds;
        this.downloadSpeed = this.formatSpeed(bytesPerSecond);
      }
    }
    this.lastSample = {bytes: progress.downloadedSize, time: now};
  }

  private formatSpeed(bytesPerSecond: number): string {
    if (bytesPerSecond >= 1024 ** 2) {
      return (bytesPerSecond / 1024 ** 2).toFixed(1) + ' MB/s';
    }
    return (bytesPerSecond / 1024).toFixed(0) + ' KB/s';
  }

  progressPercentage(): number {
    if (this.downloadProgress == null || this.downloadProgress.totalDownloadSize === 0) {
      return 0;
    }
    return Math.min(100, Math.floor(this.downloadProgress.downloadedSize / this.downloadProgress.totalDownloadSize * 100));
  }

  // ------------------------------------------------------- install lifecycle

  installMod(mod: Mod) {
    this.lockButtons = true;
    mod.installing = true;
    this.generalService.installMod(mod.modInfo.modName).subscribe({
      next: () => {
        this.toastrService.success(mod.modInfo.modName + ' installed');
        this.appState.refreshStatus();
        this.fetchMods();
      },
      error: err => {
        this.toastrService.error(errorMessage(err), 'Install failed');
        this.fetchMods();
      }
    });
  }

  uninstallMod(mod: Mod) {
    this.openConfirm(
      'Uninstall ' + mod.modInfo.modName,
      'The mod files will be removed from the game folder and the original game files will be restored. The downloaded files are kept.',
      'Uninstall',
      () => {
        this.lockButtons = true;
        mod.uninstalling = true;
        this.generalService.uninstallMod(mod.modInfo.modName).subscribe({
          next: () => {
            this.toastrService.success(mod.modInfo.modName + ' uninstalled');
            this.appState.refreshStatus();
            this.fetchMods();
          },
          error: err => {
            this.toastrService.error(errorMessage(err), 'Uninstall failed');
            this.fetchMods();
          }
        });
      });
  }

  deleteMod(mod: Mod) {
    this.openConfirm(
      'Delete files of ' + mod.modInfo.modName,
      'The downloaded files are deleted from disk. The mod stays in your list and can be downloaded again.',
      'Delete files',
      () => {
        this.lockButtons = true;
        mod.deleting = true;
        this.generalService.deleteMod(mod.modInfo.modName).subscribe({
          next: () => {
            this.appState.refreshStatus();
            this.fetchMods();
          },
          error: err => {
            this.toastrService.error(errorMessage(err), 'Delete failed');
            this.fetchMods();
          }
        });
      });
  }

  removeMod(mod: Mod) {
    this.openConfirm(
      'Remove ' + mod.modInfo.modName,
      'The mod is removed from your list and any downloaded files are deleted.',
      'Remove',
      () => {
        this.lockButtons = true;
        mod.deleting = true;
        this.generalService.removeMod(mod.modInfo.modName).subscribe({
          next: () => {
            this.appState.refreshStatus();
            this.fetchMods();
          },
          error: err => {
            this.toastrService.error(errorMessage(err), 'Remove failed');
            this.fetchMods();
          }
        });
      });
  }

  // ---------------------------------------------------------------- confirm

  private openConfirm(title: string, message: string, button: string, action: () => void) {
    this.confirmTitle = title;
    this.confirmMessage = message;
    this.confirmButton = button;
    this.confirmAction = action;
    this.modalService.open(this.confirmDialog, {centered: true});
  }

  confirm(modal: any) {
    modal.close();
    if (this.confirmAction != null) {
      this.confirmAction();
      this.confirmAction = null;
    }
  }
}
