import { Component, Input } from "@angular/core";
import { Observable, of, BehaviorSubject } from "rxjs";
import { switchMap, tap, map } from "rxjs/operators";

import { WorkflowService } from "../../services/workflow.service";
import { ContentService } from "../../services/content.service";
import { WorkflowJobViewModel } from "../../view-models/workflow-job-vm";
import { ContentViewModel } from "../../view-models/content-vm";
import { ConfigService } from 'src/app/services/config.service';

@Component({
    selector: "mcma-monitor-detail",
    templateUrl: "./monitor-detail.component.html",
    styleUrls: ["./monitor-detail.component.scss"]
})
export class MonitorDetailComponent {
    private _conformJobVm$: Observable<WorkflowJobViewModel>;
    aiJobVm$: Observable<WorkflowJobViewModel>;
    content$: Observable<ContentViewModel>;

    private currentTimeSubject = new BehaviorSubject<number>(0);
    currentTime$ = this.currentTimeSubject.asObservable().pipe(tap(x => console.log("setting current time to " + x)));

    selectedAzureCelebrity;

    get conformJobVm$(): Observable<WorkflowJobViewModel> { return this._conformJobVm$; }
    @Input() set conformJobVm$(val: Observable<WorkflowJobViewModel>) {
        this._conformJobVm$ = val;

        if (val) {
            this.aiJobVm$ = val.pipe(
                switchMap(conformJobVm =>
                    this.configService.get<boolean>("enablePolling").pipe(
                        switchMap(enablePolling =>
                            conformJobVm.isCompleted && conformJobVm.aiJobUrl
                                ? enablePolling
                                    ? this.workflowService.pollForCompletion(conformJobVm.aiJobUrl)
                                    : this.workflowService.getWorkflowJobVm(conformJobVm.aiJobUrl)
                                : of(null)
                        )
                    )
                )
            );
            
            this.content$ = val.pipe(
                switchMap(conformJobVm =>
                    this.configService.get<boolean>("enablePolling").pipe(
                        switchMap(enablePolling =>
                            conformJobVm.isCompleted && conformJobVm.contentUrl
                                ? enablePolling
                                    ? this.contentService.pollUntil(conformJobVm.contentUrl, this.aiJobVm$.pipe(map(aiJobVm => aiJobVm && aiJobVm.isFinished)))
                                    : this.contentService.getContent(conformJobVm.contentUrl).pipe(map(c => new ContentViewModel(c)))
                                : of (null)
                        )
                    )
                ),
                tap(contentVm => console.log("got content vm", contentVm))
            );
        }
    }

    constructor(private workflowService: WorkflowService, private contentService: ContentService, private configService: ConfigService) {}

    seekVideoAws(timestamp: { timecode: string, seconds: number }): void {
        console.log("seekVideoAws", timestamp);
        this.currentTimeSubject.next(timestamp.seconds);
    }

    seekVideoAzure(instance: any): void {
        console.log(instance);

        let time = instance.start;
        let timeParts = time.split(":");

        let timeSeconds = 0;

        for (const timePart of timeParts) {
            let parsed = Number.parseFloat(timePart);
            timeSeconds *= 60;
            timeSeconds += parsed;
        }

        this.currentTimeSubject.next(timeSeconds);
    }

    logTimeUpdate(evt) {
        console.log(evt);
    }
}
