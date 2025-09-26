import {Component, effect, inject, OnDestroy, signal, Signal, untracked} from '@angular/core';
import {ToastService} from '../../../services/toast.service';
import {ApiService, Job, JobStatus} from '../../../services/api.service';
import {ModalService} from '../../../services/modal.service';
import {toObservable, toSignal} from '@angular/core/rxjs-interop';
import {combineLatest, finalize, switchMap} from 'rxjs';
import {DatePipe} from '@angular/common';
import {ActivatedRoute, Router} from '@angular/router';
import {CreateJobModalComponent} from '../create-job-modal/create-job-modal.component';
import {ProjectService} from '../../../services/project.service';

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
    private route = inject(ActivatedRoute);
    private projectService = inject(ProjectService);

    targetUser = this.projectService.targetUser;
    projectId = this.projectService.projectId;
    private refreshTrigger = signal(false);
    isCreating = signal(false);

    mosaics = signal<Record<string, { url: string }>>({});

    jobs: Signal<Job[]> = toSignal(
        combineLatest([
            toObservable(this.targetUser),
            toObservable(this.projectId),
            toObservable(this.refreshTrigger)]).pipe(
            switchMap(([user, project]) =>
                this.api.getJobs(user, project!)
            )
        ), {initialValue: []}
    );
    protected readonly JobStatus = JobStatus;

    constructor() {
        effect(() => {
            const jobIds = this.jobs().map(j => j.jobId);
            const oldMosaics = untracked(this.mosaics);
            const mosaics: Record<string, { url: string }> = {};
            const missingMosaics: string[] = [];
            for (const jobId of jobIds) {
                if (oldMosaics[jobId]) {
                    mosaics[jobId] = oldMosaics[jobId];
                } else {
                    missingMosaics.push(jobId);
                }
            }

            [...Object.entries(oldMosaics)].filter(([id]) => !jobIds.includes(id))
                .forEach(([, val]) => URL.revokeObjectURL(val.url));
            this.mosaics.set(mosaics);

            missingMosaics.forEach(id => this.loadMosaic(id));

        });
    }

    private loadMosaic(jobId: string) {
        this.api.getMosaic(this.targetUser(), this.projectId()!, jobId).subscribe({
            next: blob => {
                this.mosaics.update(oldMosaics => (
                        {...oldMosaics, [jobId]: {url: URL.createObjectURL(blob)}}
                    )
                );
            },
            error: err => this.toast.error(`Failed to load image: ${err.message}`),
        })
    }

    openJob(jobId: string) {
        this.router.navigate([`./j/${jobId}`], {relativeTo: this.route});
    }

    triggerRefresh() {
        this.refreshTrigger.update(v => !v);
    }

    private revokeMosaics() {
        [...Object.entries(this.mosaics())]
            .forEach(([, val]) => URL.revokeObjectURL(val.url));
    }

    async createJob() {
        const r = await this.modals.openComponentModal<{
            targetId: string,
            n: number,
            algorithm: 'LAP',
            colorSpace: 'RGB' | 'CIELAB' | 'CIELAB_WEIGHTED',
            subdivisions: number,
            repetitions: number,
            cropCount: number
        }>(
            CreateJobModalComponent,
            {
                projectId: this.projectId(),
                targetImages: this.projectService.targetImageRefs()
            });
        if (r) {
            this.isCreating.set(true);
            this.api.createJob(
                this.targetUser(), this.projectId()!,
                r.targetId, r.n, r.algorithm, r.subdivisions, r.repetitions, r.cropCount, r.colorSpace)
                .pipe(
                    finalize(() => this.isCreating.set(false))
                ).subscribe({
                    next: (job) => {
                        this.toast.success('Generating mosaic...');
                        this.router.navigate([`j/${job.jobId}`], {relativeTo: this.route});
                    },
                    error: error => {
                        this.toast.error(`Unable to start mosaic generation: ${error.message}`);
                    }
                }
            )
        }
    }


    ngOnDestroy(): void {
        this.revokeMosaics();
    }

    async deleteJob(jobId: string) {
        if (await this.modals.openConfirmModal("Do you really want to delete this mosaic?", "Delete Mosaic")) {
            this.api.deleteJob(this.targetUser(), this.projectId()!, jobId).subscribe({
                next: () => {
                    this.toast.success("Successfully deleted mosaic");
                    this.triggerRefresh();
                },
                error: (err) => this.toast.error(`Unable to delete mosaic: ${err.message}`)
            })
        }
    }
}
