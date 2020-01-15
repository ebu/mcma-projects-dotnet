import { Component, OnInit } from "@angular/core";
import { FormControl } from "@angular/forms";
import { MatSelectionListChange } from "@angular/material";
import { Observable, BehaviorSubject, zip } from "rxjs";
import { tap, share, filter, debounceTime, map, startWith, withLatestFrom } from "rxjs/operators";

import { Container } from "../models/container";
import { Blob } from "../models/blob";
import { BlobUpload } from "../models/blob-upload";
import { AzureBlobStorageService } from "../services/azure-blob-storage.service";
import { WorkflowService } from "../services/workflow.service";
import { ModalService } from "../services/modal.service";
import { RunCompleteModalComponent } from "./run-complete-modal/run-complete-modal.component";
import { RunMetadataModalComponent } from "./run-metadata-modal/run-metadata-modal.component";

@Component({
  selector: "mcma-run",
  templateUrl: "./run.component.html",
  styleUrls: ["./run.component.scss"]
})
export class RunComponent implements OnInit {
  isLoading = true;
  container$: Observable<Container>;
  blobs$: Observable<Blob[]>;
  currentUpload$: Observable<BlobUpload>;
  selectedName: string;

  filter = new FormControl("");

  private runningWorkflowSubject = new BehaviorSubject(false);
  runningWorkflow$ = this.runningWorkflowSubject.asObservable().pipe(tap(val => console.log(val)), share());

  constructor(private blobStorageService: AzureBlobStorageService, private workflowService: WorkflowService, private modalService: ModalService) {
    this.container$ = this.blobStorageService.container$.pipe(tap(b => this.isLoading = !b));
    
    this.blobs$ =
      this.filter.valueChanges.pipe(
        debounceTime(300),
        startWith(""),
        withLatestFrom(this.container$),
        map(([val, container]) => !!container ? container.blobs.filter(o => !val || val === "" || o.name.indexOf(val) >= 0) : []));
  }

  ngOnInit() {
    this.refresh();
  }

  refresh() {
    this.isLoading = true;
    this.blobStorageService.listBlobs();
  }

  onSelectedBlobChanged(e: MatSelectionListChange) {
    e.source.options.forEach(opt => {
      if (opt.value !== e.option.value) {
        opt.selected = false;
      }
    });
    this.selectedName = e.option.value.name;
  }

  onDragOver(evt: DragEvent) {
    evt.preventDefault();
  }

  onDrop(evt: DragEvent) {
    evt.preventDefault();
    this.uploadFile(evt.dataTransfer.files);
  }

  uploadFileChanged(evt: Event): void {
    this.uploadFile((<HTMLInputElement>evt.target).files);
  }

  private uploadFile(files: FileList): void {
    const fileToUpload = files.item(0);
    if (fileToUpload) {
      this.selectedName = fileToUpload.name;
      this.currentUpload$ = this.blobStorageService.uploadBlob(fileToUpload).pipe(
        tap(curUpload => {
          // when upload completes
          const sub = curUpload.completed$.subscribe(
            () => {
              this.currentUpload$ = null;
              sub.unsubscribe();
              this.refresh();
            });
        })
      );
    }
  }

  runWorkflow(): void {
    if (this.selectedName) {
      this.modalService.showModal(RunMetadataModalComponent);

      const sub1 = this.modalService.currentModal$.subscribe(m => {
        // when modal clears, get data, if any
        if (m && !m.componentType && m.data) {
          this.runningWorkflowSubject.next(true);
          const sub2 = this.workflowService.runWorkflow(this.selectedName, m.data).pipe(filter(job => !!job))
            .subscribe(job => {
              this.runningWorkflowSubject.next(false);
              this.modalService.showModal(RunCompleteModalComponent, { job });
              sub2.unsubscribe();
            });
            
            sub1.unsubscribe();
        }
      });
    }
  }
}
