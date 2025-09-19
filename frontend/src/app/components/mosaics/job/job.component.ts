import {Component, effect, inject, OnDestroy, signal} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {ApiService, Job, JobStatus} from '../../../services/api.service';
import {AuthService} from '../../../services/auth.service';
import {ToastService} from '../../../services/toast.service';
import {DatePipe} from '@angular/common';

@Component({
    selector: 'app-job',
    imports: [
        DatePipe,
        RouterLink
    ],
    templateUrl: './job.component.html',
    styleUrl: './job.component.css'
})
export class JobComponent implements OnDestroy {
    private route = inject(ActivatedRoute);
    private api = inject(ApiService);
    private auth = inject(AuthService);
    private toast = inject(ToastService);

    username = this.auth.userName; // TODO: make target user independent of logged in user (for admins)
    projectId = signal<string | null>(null);
    jobId = signal<string | null>(null);

    job = signal<Job | null>(null);
    jobRefreshInterval: number;
    mosaic = signal<{ url: string } | null>(null);

    protected readonly JobStatus = JobStatus;

    constructor() {
        this.route.params.subscribe((params) => {
            this.projectId.set(params['projectId']);
            this.jobId.set(params['jobId']);
        });

        effect(() => this.updateJob()); // ensure job signal is updated when the username, projectId or jobId changes

        effect(() => { // fetch mosaic when job is finished
            if (this.job()?.status === JobStatus.Finished) {
                this.api.getMosaic(this.username()!, this.projectId()!, this.jobId()!).subscribe({
                    next: blob => {
                        this.mosaic.set(
                            {url: URL.createObjectURL(blob)}
                        );
                    },
                    error: err => this.toast.error(`Failed to load image: ${err.message}`),
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
        this.api.getJob(this.auth.userName()!, this.projectId()!, this.jobId()!).subscribe({
            next: job => this.job.set(job)
        })
    }

    private revokeMosaic() {
        if (this.mosaic()) {
            URL.revokeObjectURL(this.mosaic()?.url!);
        }
        this.mosaic.set(null);
    }

    ngOnDestroy(): void {
        this.revokeMosaic();
        clearInterval(this.jobRefreshInterval);
    }
}
