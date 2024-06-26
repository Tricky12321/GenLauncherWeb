import {Component, OnInit} from '@angular/core';
import {Router} from "@angular/router";
import {GeneralService} from "../../services/general.service";
import {Mod} from "../../models/Mod";
import {ModDownloadProgress} from "../../models/ModDownloadProgress";

@Component({
  selector: 'options',
  templateUrl: './options.component.html',
  styleUrls: ['./options.component.css']
})
export class OptionsComponent implements OnInit {

  constructor(public router: Router, public generalService: GeneralService) {

  }


  ngOnInit(): void {
    this.load();
  }

  load() {

  }
}
