import {BrowserModule} from '@angular/platform-browser';
import {BrowserAnimationsModule} from '@angular/platform-browser/animations';
import {NgModule} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {HttpClientModule} from '@angular/common/http';
import {RouterModule} from '@angular/router';

import {AppComponent} from './app.component';
import {HomeComponent} from './home/home.component';
import {ToastrModule} from "ngx-toastr";
import {AddModComponent} from "./addMod/add-mod.component";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {ByteToMbPipe} from "../pipes/ByteToMb.pipe";
import {ByteToGbPipe} from "../pipes/ByteToGb.pipe";
import {OptionsComponent} from "./options/options.component";
import {CreditsComponent} from "./credits/credits.component";
import {PatchesComponent} from "./patches/patches.component";

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    AddModComponent,
    OptionsComponent,
    CreditsComponent,
    PatchesComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    FormsModule,
    ToastrModule.forRoot({
      positionClass: 'toast-bottom-right',
      timeOut: 4000,
    }),
    RouterModule.forRoot([
        {path: '', component: HomeComponent, pathMatch: 'full'},
        {path: 'add-mod', component: AddModComponent, pathMatch: 'full'},
        {path: 'patches', component: PatchesComponent, pathMatch: 'full'},
        {path: 'options', component: OptionsComponent, pathMatch: 'full'},
        {path: 'credits', component: CreditsComponent, pathMatch: 'full'},
      ],
    ),
    NgbTooltip,
    ByteToMbPipe,
    ByteToGbPipe
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule {
}
