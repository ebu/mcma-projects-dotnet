import { Injectable } from "@angular/core";
import { BehaviorSubject, zip } from "rxjs";
import { filter, map } from "rxjs/operators";
import { ResourceManager, AuthProvider } from "@mcma/client";

import { ConfigService } from "./config.service";
import { AzureAdAuthService } from './azure-ad-auth.service';

@Injectable()
export class McmaClientService {

    private resourceManagerSubject = new BehaviorSubject<ResourceManager>(null);
    resourceManager$ = this.resourceManagerSubject.asObservable().pipe(filter(x => !!x));

    constructor(private configService: ConfigService, private azureAdAuthService: AzureAdAuthService) {
        zip(
            this.azureAdAuthService.accessTokenProvider$,
            this.configService.get<string>("resourceManager.servicesUrl"),
            this.configService.get<string>("resourceManager.servicesAuthType"),
            this.configService.get<string>("resourceManager.servicesAuthContext")
        ).pipe(
            map(([accessTokenProvider, servicesUrl, servicesAuthType, servicesAuthContext]) => {
                return {
                    accessTokenProvider,
                    resourceManagerConfig: {
                        servicesUrl,
                        servicesAuthType,
                        servicesAuthContext
                    }
                };
            })
        ).subscribe(x => {
            this.resourceManagerSubject.next(
                new ResourceManager(x.resourceManagerConfig, new AuthProvider().addAccessTokenAuth(x.accessTokenProvider, x.resourceManagerConfig.servicesAuthType))
            );
        });
    }
}