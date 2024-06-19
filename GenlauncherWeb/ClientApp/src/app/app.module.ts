import {BrowserModule} from '@angular/platform-browser';
import {NgModule} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {HttpClientModule} from '@angular/common/http';
import {RouterModule} from '@angular/router';

import {AppComponent} from './app.component';
import {HomeComponent} from './home/home.component';
import {ToastrModule} from "ngx-toastr";
import {AddModComponent} from "./addMod/add-mod.component";
import {DataTablesModule} from "angular-datatables";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    AddModComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    FormsModule,
    ToastrModule.forRoot(),
    DataTablesModule,
    RouterModule.forRoot([
        {path: '', component: HomeComponent, pathMatch: 'full'},
        {path: 'add-mod', component: AddModComponent, pathMatch: 'full'},
      ],
    ),
    DataTablesModule,
    NgbTooltip
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule {
}
