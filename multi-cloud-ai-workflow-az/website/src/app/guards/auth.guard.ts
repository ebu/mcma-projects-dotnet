
import { Injectable } from "@angular/core";
import { CanActivate } from "@angular/router";
import { Observable } from "rxjs";
import { map } from "rxjs/operators";

import { AzureAdAuthService } from "../services/azure-ad-auth.service";

@Injectable()
export class AuthGuard implements CanActivate {

    constructor(private azureAdAuthService: AzureAdAuthService) {}
    
    canActivate(): Observable<boolean> {
        return this.azureAdAuthService.isLoggedIn$;
    }
}