import { BehaviorSubject, Observable, from } from "rxjs";
import { BlobUploadCommonResponse } from "@azure/storage-blob";
import { TransferProgressEvent } from "@azure/core-http";

export class BlobUpload {

    private percentCompleteSubject = new BehaviorSubject<number>(0);
    percentComplete$ = this.percentCompleteSubject.asObservable();

    completed$: Observable<BlobUploadCommonResponse>;
    
    set completionPromise(val: Promise<BlobUploadCommonResponse>) {
        this.completed$ = from(val);
    }

    constructor(public name: string, private fileSize: number) {}

    updateProgress(progressEvent: TransferProgressEvent) {
        this.percentCompleteSubject.next(Math.round((progressEvent.loadedBytes / this.fileSize) * 10000) / 100);
    }
}