import {Component, inject, signal} from '@angular/core';
import {ApiService} from '../../../services/api.service';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {toObservable, toSignal} from '@angular/core/rxjs-interop';
import {filter, switchMap} from 'rxjs';
import {AuthService} from '../../../services/auth.service';
import {DatePipe} from '@angular/common';

@Component({
    selector: 'app-project',
    imports: [
        DatePipe,
        RouterLink
    ],
    templateUrl: './project.component.html',
    styleUrl: './project.component.css'
})
export class ProjectComponent {
    private route = inject(ActivatedRoute);
    private api = inject(ApiService);
    private auth = inject(AuthService);


    projectId = signal<string | null>(null);
    project = toSignal(
        toObservable(this.projectId).pipe(
            filter(projectId => !!projectId),
            switchMap(projectId => this.api.getProject(this.auth.userName()!, projectId!))
        )
    );

    constructor() {
        this.route.params.subscribe((params) => {
            this.projectId.set(params['projectId']);
        });
    }

}
