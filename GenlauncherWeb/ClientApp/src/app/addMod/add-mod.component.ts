import {Component, OnInit} from '@angular/core';
import {Router} from "@angular/router";
import {GeneralService} from "../../services/general.service";
import {ReposModsData} from "../../models/ReposModsData";

@Component({
  selector: 'add-mod',
  templateUrl: './add-mod.component.html',
  styleUrls: ['./add-mod.component.css']
})
export class AddModComponent implements OnInit {
  dtOptions: DataTables.Settings = {
    paging: false,
    scrollY: "400px",
  };

  constructor(public router: Router, public generalService: GeneralService) {

  }

  public repoModsData: ReposModsData;

  ngOnInit(): void {
    this.load();
  }


  public load() {
    this.generalService.getModList().subscribe(success => {
    this.repoModsData = success;
    })
  }
}