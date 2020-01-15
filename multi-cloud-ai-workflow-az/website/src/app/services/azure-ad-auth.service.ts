import { Injectable } from "@angular/core";
import { Observable, from, BehaviorSubject, zip } from 'rxjs';
import { switchMap, map, tap, distinctUntilChanged } from "rxjs/operators";
import { UserAgentApplication, Account, Logger, LogLevel } from "@azure/msal";
import { AccessToken } from '@mcma/client';
import { AzureAdPublicClientAccessTokenProvider, ConfigurationWithTenant } from "@mcma/azure-client";

import { ConfigService } from './config.service';
import { emitOnceAndCache } from '../utility-functions';

@Injectable()
export class AzureAdAuthService {

    private scopesSubject = new BehaviorSubject<{ [key: string]: string }>(null);
    private scopes$: Observable<{ [key: string]: string }>;
    
    private userAgentOptionsSubject = new BehaviorSubject<ConfigurationWithTenant>(null);
    private userAgentOptions$: Observable<ConfigurationWithTenant>;

    private isLoggedInSubject = new BehaviorSubject<boolean>(false);

    private userAgentAppSubject = new BehaviorSubject<UserAgentApplication>(null);
    private userAgentApp$: Observable<UserAgentApplication>;

    private accountSubject = new BehaviorSubject<Account>(null);
    private account$: Observable<Account>;

    isLoggedIn$: Observable<boolean>;

    private accessTokenProviderSubject = new BehaviorSubject<AzureAdPublicClientAccessTokenProvider>(null);
    accessTokenProvider$: Observable<AzureAdPublicClientAccessTokenProvider>;

    private accessTokenSubjectMap: { [key: string]: BehaviorSubject<AccessToken> } = {};
    private accessTokenObservableMap: { [key: string]: Observable<AccessToken> } = {};

    private accountPromise: Promise<Account>;

    constructor(private configService: ConfigService) {
        this.scopes$ = emitOnceAndCache(this.scopesSubject, this.configService.get<{ [key: string]: string }>("azure.ad.scopes"));
        this.userAgentOptions$ =
            emitOnceAndCache(
                this.userAgentOptionsSubject,
                this.configService.get<ConfigurationWithTenant>("azure.ad.config").pipe(tap(c => this.ensureAuthority(c))));

        this.userAgentApp$ = emitOnceAndCache(this.userAgentAppSubject, this.userAgentOptions$.pipe(map(config => new UserAgentApplication(config))));
        this.account$ = emitOnceAndCache(this.accountSubject, this.getAccount$);
        this.isLoggedIn$ = this.account$.pipe(map(a => !!a), distinctUntilChanged());
        this.accessTokenProvider$ =
            emitOnceAndCache(
                this.accessTokenProviderSubject,
                zip(this.userAgentOptions$, this.account$).pipe(map(([config, account]) => new AzureAdPublicClientAccessTokenProvider(config, account))));
    }

    private ensureAuthority(options: ConfigurationWithTenant) {
        if (options.tenant && !options.auth.authority) {
            options.auth.authority = "https://login.microsoftonline.com/" + options.tenant;
            delete options.tenant;
        }
    }

    private get getAccount$(): Observable<Account> {
        return this.userAgentApp$.pipe(
            switchMap(userAgentApp => {
                if (!this.accountPromise) {
                    this.accountPromise = new Promise<Account>((resolve, reject) => {
                        console.log("Logging in...");
                        userAgentApp.handleRedirectCallback(
                            (err,loginResp) => {
                                if (err) {
                                    reject(err);
                                } else {
                                    resolve(loginResp.account);
                                }
                            }
                        );

                        try {
                            let account = userAgentApp.getAccount();
                            if (account) {
                                console.log("Found account", account);
                                resolve(account);
                            } else {
                                console.log("Prompting for login via redirect...");
                                userAgentApp.loginRedirect();
                            }
                        } catch (e) {
                            reject(e);
                        }
                    });
                }

                return from(this.accountPromise);
            })
        );
    }

    getAccessToken$(scopeKey: string): Observable<AccessToken> {
        if (this.accessTokenObservableMap[scopeKey]) {
            return this.accessTokenObservableMap[scopeKey];
        }
        
        const subject = new BehaviorSubject<AccessToken>(null);
        const observable = emitOnceAndCache(
            subject,
            this.scopes$.pipe(
                map(scopes => scopes[scopeKey]),
                switchMap(scope => this.accessTokenProvider$.pipe(switchMap(accessTokenProvider => from(accessTokenProvider.getAccessToken({ scope }))))),
                tap(accessToken => subject.next(accessToken)),
                tap(accessToken => this.isLoggedInSubject.next(!!accessToken))
            )
        );

        this.accessTokenSubjectMap[scopeKey] = subject;
        this.accessTokenObservableMap[scopeKey] = observable;

        return observable;
    }
}