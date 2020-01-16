import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, BehaviorSubject } from "rxjs";
import { map, share } from "rxjs/operators";
import { emitOnceAndCache } from '../utility-functions';

@Injectable()
export class ConfigService {
    private loadConfig$: Observable<any>;
    private configSubject = new BehaviorSubject<any>(null);
    private config$: Observable<any>;

    constructor(private httpClient: HttpClient) {
        this.loadConfig$ = this.httpClient.get("config.json");
        this.config$ = emitOnceAndCache(this.configSubject, this.loadConfig$);
    }

    get<T>(key: string, defaultVal: T = null): Observable<T> {
        return this.config$.pipe(
            map(c => {
                console.log("getting config for key " + key);
                let val = c;
                for (let keyPart of key.split(".")) {
                    if (!val.hasOwnProperty(keyPart)) {
                        return defaultVal;
                    }
                    val = val[keyPart];
                }
                console.log("config for key " + key, val);
                return val;
            })
        );
    }
}