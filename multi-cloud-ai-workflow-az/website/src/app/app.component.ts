import { Component } from "@angular/core";
import { Observable, of } from "rxjs";
import { map, share } from "rxjs/operators";

import { AzureAdAuthService } from "./services/azure-ad-auth.service";

@Component({
    selector: "app-root",
    templateUrl: "./app.component.html",
    styleUrls: ["./app.component.scss"]
})


export class AppComponent {
    isLoggedIn$: Observable<boolean>;

    constructor(private azureAdAuthService: AzureAdAuthService) {
        this.isLoggedIn$ = this.azureAdAuthService.isLoggedIn$;
    }
}
