import {Component, OnDestroy, OnInit} from '@angular/core';
import {GameService} from "../services/game.service";
import {ToastrService} from "ngx-toastr";
import {GeneralService} from "../services/general.service";
import {OptionsService} from "../services/options.service";
import {AppStateService} from "../services/app-state.service";
import {InstallationStatus} from "../models/InstallationStatus";
import {GameType, gameDisplayName} from "../models/GameType";
import {errorMessage} from "./util";
import {Subscription} from "rxjs";

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css'],
    standalone: false
})
export class AppComponent implements OnInit, OnDestroy {
  public installationStatus: InstallationStatus | null = null;
  public installingGenTool: boolean = false;
  public steamPath: string | null = null;
  public configPath: string | null = null;
  public detectedGames: GameType[] | null = null;
  public selectedGame: GameType | null = null;
  public switchingGame: boolean = false;
  public modStats: { installed: number, downloaded: number, totalSize: number } | null = null;

  protected readonly GameType = GameType;
  protected readonly gameDisplayName = gameDisplayName;

  private statusSub: Subscription | null = null;

  constructor(private gameService: GameService,
              private generalService: GeneralService,
              private optionsService: OptionsService,
              private appState: AppStateService,
              private toastrService: ToastrService) {
  }

  ngOnInit(): void {
    this.load();
    this.statusSub = this.appState.statusRefresh$.subscribe(() => {
      this.loadInstallationStatus();
      this.loadModStats();
    });
  }

  ngOnDestroy(): void {
    this.statusSub?.unsubscribe();
  }

  load() {
    this.generalService.getPaths().subscribe({
      next: result => {
        this.steamPath = result.steamInstallPath;
        this.configPath = result.configPath;
      },
      error: err => this.toastrService.error(errorMessage(err), 'Could not load paths')
    });

    this.generalService.getDetectedGames().subscribe({
      next: result => {
        this.detectedGames = result.detectedGames;
        this.selectedGame = result.selectedGame;
      },
      error: err => this.toastrService.error(errorMessage(err), 'Game detection failed')
    });

    this.loadInstallationStatus();
    this.loadModStats();
  }

  loadInstallationStatus() {
    this.generalService.getInstallationStatus().subscribe({
      next: status => this.installationStatus = status,
      error: () => this.installationStatus = {moddedLauncher: false, genTool: false}
    });
  }

  loadModStats() {
    this.generalService.getAddedMods().subscribe({
      next: mods => this.modStats = {
        installed: mods.filter(m => m.installed).length,
        downloaded: mods.filter(m => m.downloaded).length,
        totalSize: mods.filter(m => m.downloaded).reduce((sum, m) => sum + (m.totalSize || 0), 0)
      },
      error: () => this.modStats = null
    });
  }

  isDetected(game: GameType): boolean {
    return this.detectedGames != null && this.detectedGames.includes(game);
  }

  switchGame(game: GameType) {
    if (this.switchingGame || game === this.selectedGame || !this.isDetected(game)) {
      return;
    }
    this.switchingGame = true;
    this.optionsService.getOptions().subscribe({
      next: options => {
        options.selectedGame = game;
        this.optionsService.setOptions(options).subscribe({
          next: () => location.reload(),
          error: err => {
            this.switchingGame = false;
            this.toastrService.error(errorMessage(err), 'Could not switch game');
          }
        });
      },
      error: err => {
        this.switchingGame = false;
        this.toastrService.error(errorMessage(err), 'Could not switch game');
      }
    });
  }

  selectedGameName(): string {
    return this.selectedGame != null ? gameDisplayName(this.selectedGame) : '';
  }

  startGame() {
    this.gameService.startGame().subscribe({
      next: () => this.toastrService.success('Game is starting through Steam', 'Launch'),
      error: err => this.toastrService.error(errorMessage(err), 'Launch failed')
    });
  }

  installGenTool() {
    this.installingGenTool = true;
    this.generalService.installGenTool().subscribe({
      next: () => {
        this.installingGenTool = false;
        this.toastrService.success('GenTool installed');
        this.loadInstallationStatus();
      },
      error: err => {
        this.installingGenTool = false;
        this.toastrService.error(errorMessage(err), 'GenTool install failed');
      }
    });
  }
}
