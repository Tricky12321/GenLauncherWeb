import {Component, OnInit} from '@angular/core';
import {GameService} from "../services/game.service";
import {ToastrService} from "ngx-toastr";
import {GeneralService} from "../services/general.service";
import {Router} from "@angular/router";
import {InstallationStatus} from "../models/InstallationStatus";

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  public installationStatus: InstallationStatus | null = null;
  public installingGenTool: boolean = false;

  constructor(private gameService: GameService, private generalService: GeneralService, private toastrService: ToastrService, public router: Router) {

  }

  public steamPath: string;
  public configPath: string;
  public homePage: boolean = true;

  startGame() {
    this.gameService.startGame().subscribe(success => {
      this.toastrService.success('Game started', 'Success');
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load() {
    this.generalService.getPaths().subscribe(result => {
      this.steamPath = result.steamInstallPath;
      this.configPath = result.configPath;
    })

    this.generalService.getInstallationStatus().subscribe(success => {
      this.installationStatus = success;
    })
  }

  toggleModsView() {
    if (this.homePage) {
      this.router.navigate(['add-mod']);
    } else {
      this.router.navigate(['']);
    }
    this.homePage = !this.homePage;
  }

  getModsButtonText() {
    return window.location.pathname == '/add-mod' ? 'Added mods' : 'Add mods';
  }

  showOptions() {
    this.router.navigate(['options']);
  }

  isOnOptionsPage() {
    return window.location.pathname == '/options';
  }

  goHome() {
    this.router.navigate(['']);
  }


  installGenTool() {
    this.installingGenTool = true;
    this.generalService.installGenTool().subscribe(success => {
      location.reload();
    });
  }

  showCredits() {
    this.router.navigate(['credits']);
  }
}
