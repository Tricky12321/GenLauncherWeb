import {Component, OnInit} from '@angular/core';
import {GameService} from "../services/game.service";
import {ToastrService} from "ngx-toastr";
import {GeneralService} from "../services/general.service";

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  constructor(private gameService: GameService, private generalService: GeneralService, private toastrService: ToastrService) {

  }

  public steamPath: string;

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
}
