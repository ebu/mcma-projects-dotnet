import { Injectable } from "@angular/core";
import { Observable, BehaviorSubject } from "rxjs";
import { switchMap, map, zip, scan, startWith, filter } from "rxjs/operators";
import * as AzureCoreAuth from "@azure/core-auth";
import { PagedAsyncIterableIterator } from "@azure/core-paging";
import { BlobServiceClient } from "@azure/storage-blob";
import { AccessToken } from '@mcma/client';

import { AzureAdAuthService } from "./azure-ad-auth.service";
import { ConfigService } from "./config.service";
import { Container } from "../models/container";
import { BlobUpload } from "../models/blob-upload";
import { emitOnceAndCache } from '../utility-functions';

@Injectable()
export class AzureBlobStorageService {

    private containerSubject = new BehaviorSubject<Container>(null);
    container$ = this.containerSubject.asObservable();

    private blobServiceClientSubject = new BehaviorSubject<BlobServiceClient>(null);
    private blobServiceClient$: Observable<BlobServiceClient>;

    constructor(private configService: ConfigService, private azureAdAuthService: AzureAdAuthService) {
        this.blobServiceClient$ = emitOnceAndCache(this.blobServiceClientSubject, this.createBlobServiceClient$);
    }
    
    private wrapAccessTokenAsCredential(accessToken: AccessToken): AzureCoreAuth.TokenCredential {
        console.log("Creating TokenCredential object with access token", accessToken);
        return {
            getToken: (scopes: string | string[], options?: AzureCoreAuth.GetTokenOptions): Promise<AzureCoreAuth.AccessToken> => Promise.resolve({
                token: accessToken.accessToken,
                expiresOnTimestamp: accessToken.expiresOn instanceof Date ? accessToken.expiresOn.getTime() : accessToken.expiresOn
            })
        };
    }

    private get createBlobServiceClient$(): Observable<BlobServiceClient> {
        return this.azureAdAuthService.getAccessToken$("blobStorage").pipe(
            filter(accessToken => !!accessToken),
            map(accessToken => this.wrapAccessTokenAsCredential(accessToken)),
            filter(creds => !!creds),
            zip(this.configService.get<string>("azure.storage.mediaStorageAccountName")),
            map(([creds, mediaStorageAccountName]) => new BlobServiceClient(`https://${mediaStorageAccountName}.blob.core.windows.net`, creds))
        );
    }

    listBlobs(): void {
        this.containerSubject.next(null);
        this.blobServiceClient$.pipe(
            zip(this.configService.get<string>("azure.storage.uploadContainer")),
            switchMap(([blobServiceClient, uploadContainer]) => {
                console.log('uploadcontainer', uploadContainer);
                return this.asyncToObservable(blobServiceClient.getContainerClient(uploadContainer).listBlobsFlat()).pipe(
                    map((items) => {
                        return {
                            name: uploadContainer,
                            blobs: items.map(i => {
                                return {
                                    name: i.name,
                                    etag: i.properties.etag,
                                    size: i.properties.contentLength,
                                    lastModified: i.properties.lastModified
                                };
                            })
                        };
                    })
                );
            }),
        ).subscribe(this.containerSubject);
    }

    uploadBlob(file: File): Observable<BlobUpload> {
        return this.blobServiceClient$.pipe(
            zip(this.configService.get<string>("azure.storage.uploadContainer")),
            map(([blobServiceClient, containerName]) => {
                const upload = new BlobUpload(file.name, file.size);
                const blockBlobClient = blobServiceClient.getContainerClient(containerName).getBlockBlobClient(file.name);
                upload.completionPromise = blockBlobClient.uploadBrowserData(file, { onProgress: evt => upload.updateProgress(evt) });
                return upload;
            })
        );
    }

    private asyncToObservable<T, TService>(iterable: PagedAsyncIterableIterator<T, TService>): Observable<T[]> {
        return new Observable<T>(
            observer =>
                void (async () => {
                    try {
                        for await (const item of iterable as AsyncIterable<T>) {
                            if (observer.closed) return;
                            observer.next(item);
                        }
                        observer.complete();
                    } catch (e) {
                        observer.error(e);
                    }
                })()
        ).pipe(
            scan<T, T[]>((items, item) => [...items, item], []),
            startWith([] as T[])
        );
    }
}