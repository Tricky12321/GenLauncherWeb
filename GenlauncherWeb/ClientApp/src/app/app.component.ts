import {Component, OnInit} from '@angular/core';
import {GameService} from "../services/game.service";
import {ToastrService} from "ngx-toastr";
import {GeneralService} from "../services/general.service";
import {Router} from "@angular/router";

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  constructor(private gameService: GameService, private generalService: GeneralService, private toastrService: ToastrService, public router: Router) {

  }

  public steamPath: string;
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
    this.generalService.getSteamPath().subscribe(result => {
      console.log("Steam path: " + result.steamInstallPath);
      this.steamPath = result.steamInstallPath;
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
    return window.location.pathname == '/add-mod' ? 'Installed mods' : 'Install mods';
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
}
