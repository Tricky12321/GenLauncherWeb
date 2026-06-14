import {Component, OnDestroy, OnInit} from '@angular/core';
import {PatchService} from "../../services/patch.service";
import {GamePatch} from "../../models/GamePatch";
import {ModDownloadProgress} from "../../models/ModDownloadProgress";
import {ToastrService} from "ngx-toastr";
import {errorMessage} from "../util";

@Component({
  selector: 'app-patches',
  templateUrl: './patches.component.html',
  styleUrls: ['./patches.component.css'],
  standalone: false
})
export class PatchesComponent implements OnInit, OnDestroy {
  public patches: GamePatch[] | null = null;
  public loading: boolean = true;

  public downloadProgress: { [patchUrl: string]: ModDownloadProgress } = {};
  public downloadSpeed: { [patchUrl: string]: string } = {};
  private pollTimers: { [patchUrl: string]: any } = {};
  private lastSamples: { [patchUrl: string]: { bytes: number, time: number } } = {};

  constructor(private patchService: PatchService, private toastr: ToastrService) {}

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    Object.values(this.pollTimers).forEach(t => clearTimeout(t));
  }

  load() {
    this.loading = true;
    this.patches = null;
    this.patchService.getPatches().subscribe({
      next: patches => {
        this.patches = patches;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        this.toastr.error(errorMessage(err), 'Could not load patches');
      }
    });
  }

  // ---------------------------------------------------------------- download

  downloadPatch(patch: GamePatch) {
    patch._loadingDownload = true;
    patch._downloadPollActive = true;
    this.downloadProgress[patch.patchUrl] = {downloadedSize: 0, totalDownloadSize: 0, downloaded: false, downloadedFiles: [], fileList: [], percentage: 0};
    this.lastSamples[patch.patchUrl] = {bytes: 0, time: Date.now()};

    this.patchService.downloadPatch(patch.patchUrl).subscribe({
      next: () => {
        patch.downloaded = true;
        patch._loadingDownload = false;
        patch._downloadPollActive = false;
        delete this.downloadProgress[patch.patchUrl];
        delete this.downloadSpeed[patch.patchUrl];
      },
      error: err => {
        this.toastr.error(errorMessage(err), 'Download failed');
        patch._loadingDownload = false;
        patch._downloadPollActive = false;
        delete this.downloadProgress[patch.patchUrl];
        delete this.downloadSpeed[patch.patchUrl];
      }
    });

    this.pollProgress(patch);
  }

  private pollProgress(patch: GamePatch) {
    this.patchService.getProgress(patch.patchUrl).subscribe({
      next: progress => {
        if (!patch._downloadPollActive) return;
        this.downloadProgress[patch.patchUrl] = progress;
        this.updateSpeed(patch.patchUrl, progress);
        if (!progress.downloaded) {
          this.pollTimers[patch.patchUrl] = setTimeout(() => this.pollProgress(patch), 500);
        }
      },
      error: () => {
        if (patch._downloadPollActive) {
          this.pollTimers[patch.patchUrl] = setTimeout(() => this.pollProgress(patch), 1000);
        }
      }
    });
  }

  private updateSpeed(patchUrl: string, progress: ModDownloadProgress) {
    const now = Date.now();
    const last = this.lastSamples[patchUrl];
    if (last && progress.downloadedSize > last.bytes) {
      const seconds = (now - last.time) / 1000;
      if (seconds > 0) {
        const bps = (progress.downloadedSize - last.bytes) / seconds;
        this.downloadSpeed[patchUrl] = this.formatSpeed(bps);
      }
    }
    this.lastSamples[patchUrl] = {bytes: progress.downloadedSize, time: now};
  }

  progressPct(patchUrl: string): number {
    const p = this.downloadProgress[patchUrl];
    if (!p || p.totalDownloadSize === 0) return 0;
    return Math.min(100, Math.floor(p.downloadedSize / p.totalDownloadSize * 100));
  }

  private formatSpeed(bps: number): string {
    if (bps >= 1024 ** 2) return (bps / 1024 ** 2).toFixed(1) + ' MB/s';
    return (bps / 1024).toFixed(0) + ' KB/s';
  }

  // ---------------------------------------------------------------- actions

  installPatch(patch: GamePatch) {
    patch._loadingInstall = true;
    this.patchService.installPatch(patch.patchUrl).subscribe({
      next: () => {
        patch.installed = true;
        patch._loadingInstall = false;
      },
      error: err => {
        this.toastr.error(errorMessage(err), 'Install failed');
        patch._loadingInstall = false;
      }
    });
  }

  uninstallPatch(patch: GamePatch) {
    patch._loadingInstall = true;
    this.patchService.uninstallPatch(patch.patchUrl).subscribe({
      next: () => {
        patch.installed = false;
        patch._loadingInstall = false;
      },
      error: err => {
        this.toastr.error(errorMessage(err), 'Uninstall failed');
        patch._loadingInstall = false;
      }
    });
  }

  deletePatch(patch: GamePatch) {
    patch._loadingDelete = true;
    this.patchService.deletePatch(patch.patchUrl).subscribe({
      next: () => {
        patch.downloaded = false;
        patch.installed = false;
        patch._loadingDelete = false;
      },
      error: err => {
        this.toastr.error(errorMessage(err), 'Delete failed');
        patch._loadingDelete = false;
      }
    });
  }
}
