import {computed, inject, Injectable, signal, Signal} from '@angular/core';
import {ApiService, ImageRef} from './api.service';
import {ActivatedRoute} from '@angular/router';
import {combineLatest, distinctUntilChanged, filter, map, switchMap} from 'rxjs';
import {UserContextService} from './user-context.service';
import {toObservable, toSignal} from '@angular/core/rxjs-interop';

@Injectable()
export class ProjectService {
    private api = inject(ApiService);
    private route = inject(ActivatedRoute);
    private userContext = inject(UserContextService);

    projectId = toSignal(this.route.paramMap.pipe(
        map(params => params.get('projectId')),
        distinctUntilChanged()
    ), {initialValue: null})//signal<string | null>(null);
    targetUser = this.userContext.targetUser;
    project = toSignal(
        toObservable(this.projectId).pipe(
            filter(projectId => !!projectId),
            switchMap(projectId => this.api.getProject(this.targetUser(), projectId!))
        )
    );


    private refreshTrigger = signal(false);
    private imageRefs: Signal<ImageRef[]> = toSignal(
        combineLatest([
            toObservable(this.targetUser),
            toObservable(this.projectId),
            toObservable(this.refreshTrigger)]).pipe(
            switchMap(([user, project]) =>
                this.api.getImageRefs(user, project!, 'ALL')
            )
        ), {initialValue: []}
    );
    targetImageRefs = computed(() => this.imageRefs().filter(i => i.isTarget))
    tileImageRefs = computed(() => this.imageRefs().filter(i => !i.isTarget))

    refreshImages() {
        this.refreshTrigger.update(v => !v);
    }

}
