import {Component, OnInit} from '@angular/core';
import {GeneralService} from "../../services/general.service";
import {ModAddonsAndPatches} from "../../models/ModAddonsAndPatches";
import {ToastrService} from "ngx-toastr";
import {errorMessage} from "../util";
import {GameType, gameDisplayName} from "../../models/GameType";

@Component({
    selector: 'add-mod',
    templateUrl: './add-mod.component.html',
    styleUrls: ['./add-mod.component.css'],
    standalone: false
})
export class AddModComponent implements OnInit {
  public mods: ModAddonsAndPatches[] | null = null;
  public filter: string = '';
  public game: GameType | null = null;

  protected readonly GameType = GameType;
  protected readonly gameDisplayName = gameDisplayName;

  constructor(public generalService: GeneralService, public toastrService: ToastrService) {
  }

  ngOnInit(): void {
    this.load();
  }

  public load() {
    this.mods = null;
    this.generalService.getModList().subscribe({
      next: repoData => {
        // added/installed come from the backend; adding is client-side state
        repoData.modDatas.forEach(x => x.adding = false);
        this.game = repoData.game;
        this.mods = repoData.modDatas;
      },
      error: err => {
        this.mods = [];
        this.toastrService.error(errorMessage(err), 'Could not load mod repository');
      }
    });
  }

  filteredMods(): ModAddonsAndPatches[] {
    if (this.mods == null) {
      return [];
    }
    const needle = this.filter.trim().toLowerCase();
    if (needle === '') {
      return this.mods;
    }
    return this.mods.filter(x => x.modName.toLowerCase().includes(needle));
  }

  addMod(mod: ModAddonsAndPatches) {
    mod.adding = true;
    this.generalService.addMod(mod.modName).subscribe({
      next: () => {
        this.toastrService.success(mod.modName + ' added to your list');
        mod.added = true;
      },
      error: err => {
        mod.adding = false;
        this.toastrService.error(errorMessage(err), 'Could not add mod');
      }
    });
  }
}
