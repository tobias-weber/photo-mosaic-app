import {Component, computed, effect, inject, OnDestroy, signal} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {ApiService, Job, JobStatus} from '../../../services/api.service';
import {ToastService} from '../../../services/toast.service';
import {DatePipe} from '@angular/common';
import {ProjectService} from '../../../services/project.service';
import {MosaicViewerComponent} from '../mosaic-viewer/mosaic-viewer.component';
import {NgbProgressbar} from '@ng-bootstrap/ng-bootstrap';

const statusTexts: Record<JobStatus, string> = {
    [JobStatus.Created]: 'Initializing...',
    [JobStatus.Submitted]: 'Initializing...',
    [JobStatus.Processing]: 'Computing optimal mosaic...',
    [JobStatus.GeneratedPreview]: 'Assembling mosaic...',
    [JobStatus.Finished]: 'Finished',
    [JobStatus.Aborted]: 'Aborted',
    [JobStatus.Failed]: 'Failed',
};

@Component({
    selector: 'app-job',
    imports: [
        DatePipe,
        RouterLink,
        MosaicViewerComponent,
        NgbProgressbar
    ],
    templateUrl: './job.component.html',
    styleUrl: './job.component.css'
})
export class JobComponent implements OnDestroy {
    private route = inject(ActivatedRoute);
    private api = inject(ApiService);
    private toast = inject(ToastService);
    private projectService = inject(ProjectService);

    username = this.projectService.targetUser;
    projectId = this.projectService.projectId;
    jobId = signal<string | null>(null);

    job = signal<Job | null>(null);
    targetImageId = computed(() => this.job()?.target)
    targetImage = signal<{ url: string } | null>(null);
    hasPreview = computed(() => this.job() && [JobStatus.GeneratedPreview, JobStatus.Finished].includes(this.job()!.status))
    isComplete = computed(() => this.job() && [JobStatus.Finished, JobStatus.Failed, JobStatus.Aborted].includes(this.job()!.status))
    jobRefreshInterval: number;
    mosaic = signal<{ url: string } | null>(null);

    statusText = computed(() => statusTexts[this.job()?.status ?? JobStatus.Created]);

    protected readonly JobStatus = JobStatus;

    constructor() {
        this.route.params.subscribe((params) => this.jobId.set(params['jobId']));

        effect(() => this.updateJob()); // ensure job signal is updated when the username, projectId or jobId changes

        effect(() => {
            if (this.targetImageId()) {
                this.api.getImage(this.username()!, this.projectId()!, this.targetImageId()!).subscribe({
                    next: blob => {
                        this.revokeTargetImage();
                        this.targetImage.set({url: URL.createObjectURL(blob)});
                    },
                    error: err => this.toast.error(`Failed to load target image: ${err.message}`),
                })
            } else {
                this.targetImage.set(null);
            }
        });

        effect(() => { // fetch mosaic when job preview exists
            if (this.hasPreview()) {
                this.api.getMosaic(this.username()!, this.projectId()!, this.jobId()!).subscribe({
                    next: blob => {
                        this.revokeMosaic();
                        this.mosaic.set({url: URL.createObjectURL(blob)});
                    },
                    error: err => this.toast.error(`Failed to load mosaic: ${err.message}`),
                })
            } else {
                this.mosaic.set(null);
            }
        });

        // Polling strategy is sufficient for this simple use case. If longer waiting is expected: switch to websockets
        this.jobRefreshInterval = setInterval(() => {
            if (this.jobId()) {
                if (this.job()?.status != null &&
                    [JobStatus.Finished, JobStatus.Aborted, JobStatus.Failed].includes(this.job()?.status!)) {
                    clearInterval(this.jobRefreshInterval);
                } else {
                    this.updateJob();
                }
            }
        }, 1000);
    }

    private updateJob() {
        this.api.getJob(this.username(), this.projectId()!, this.jobId()!).subscribe({
            next: job => this.job.set(job)
        })
    }

    private revokeTargetImage() {
        if (this.targetImage()) {
            URL.revokeObjectURL(this.targetImage()?.url!);
        }
        this.targetImage.set(null);
    }

    private revokeMosaic() {
        if (this.mosaic()) {
            URL.revokeObjectURL(this.mosaic()?.url!);
        }
        this.mosaic.set(null);
    }

    ngOnDestroy(): void {
        this.revokeTargetImage();
        this.revokeMosaic();
        clearInterval(this.jobRefreshInterval);
    }
}
