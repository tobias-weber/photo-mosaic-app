import {Component, inject, input, OnDestroy, signal, Signal} from '@angular/core';
import {ToastService} from '../../../services/toast.service';
import {ApiService, Job, JobStatus} from '../../../services/api.service';
import {ModalService} from '../../../services/modal.service';
import {toObservable, toSignal} from '@angular/core/rxjs-interop';
import {combineLatest, switchMap} from 'rxjs';
import {DatePipe} from '@angular/common';

@Component({
    selector: 'app-mosaic-list',
    imports: [
        DatePipe
    ],
    templateUrl: './mosaic-list.component.html',
    styleUrl: './mosaic-list.component.css'
})
export class MosaicListComponent implements OnDestroy {
    private toast = inject(ToastService);
    private api = inject(ApiService);
    private modals = inject(ModalService);

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
    private revokeMosaic() {
        if (this.mosaic()) {
            URL.revokeObjectURL(this.mosaic()?.url!);
        }
        this.mosaic.set(null);
    }

    ngOnDestroy(): void {
        this.revokeMosaic();
    }
}
