import {Component, inject, input, OnDestroy, signal, Signal} from '@angular/core';
import {ToastService} from '../../../services/toast.service';
import {ApiService, Job, JobStatus} from '../../../services/api.service';
import {ModalService} from '../../../services/modal.service';
import {toObservable, toSignal} from '@angular/core/rxjs-interop';
import {combineLatest, switchMap} from 'rxjs';
import {DatePipe} from '@angular/common';
import {Router} from '@angular/router';
import {CreateJobModalComponent} from '../create-job-modal/create-job-modal.component';

@Component({
    selector: 'app-job-list',
    imports: [
        DatePipe
    ],
    templateUrl: './job-list.component.html',
    styleUrl: './job-list.component.css'
})
export class JobListComponent implements OnDestroy {
    private toast = inject(ToastService);
    private api = inject(ApiService);
    private modals = inject(ModalService);
    private router = inject(Router);

    targetUser = input.required<string>();
    projectId = input.required<string>();
    private refreshTrigger = signal(false);

    mosaic = signal<{ url: string, jobId: string } | null>(null);

    jobs: Signal<Job[]> = toSignal(
        combineLatest([
            toObservable(this.targetUser),
            toObservable(this.projectId),
            toObservable(this.refreshTrigger)]).pipe(
            switchMap(([user, project]) =>
                this.api.getJobs(user, project)
            )
        ), {initialValue: []}
    );
    protected readonly JobStatus = JobStatus;

    loadMosaic(jobId: string) {
        this.mosaic.set(null);
        this.api.getMosaic(this.targetUser(), this.projectId(), jobId).subscribe({
            next: blob => {
                this.mosaic.set(
                    {url: URL.createObjectURL(blob), jobId}
                );
            },
            error: err => this.toast.error(`Failed to load image: ${err.message}`),
        })
    }

    openJob(jobId: string) {
        this.router.navigate([`/projects/${this.projectId()}/j/${jobId}`]);
    }

    private revokeMosaic() {
        if (this.mosaic()) {
            URL.revokeObjectURL(this.mosaic()?.url!);
        }
        this.mosaic.set(null);
    }

    async createJob() {
        const result = await this.modals.openComponentModal<{
            targetId: string,
            n: number,
            algorithm: 'LAP',
            subdivisions: number
        }>(
            CreateJobModalComponent,
            {
                projectId: this.projectId(),
            });
        if (result) {
            this.api.createJob(this.targetUser(), this.projectId(),
                result.targetId, result.n, result.algorithm, result.subdivisions).subscribe({
                    next: (job) => {
                        this.toast.success('Generating mosaic...');
                        this.router.navigate([`/projects/${this.projectId()}/j/${job.jobId}`]);
                    },
                    error: error => {
                        this.toast.error(`Unable to start mosaic generation: ${error.message}`);
                    },
                }
            )
        }
    }


    ngOnDestroy(): void {
        this.revokeMosaic();
    }
}
