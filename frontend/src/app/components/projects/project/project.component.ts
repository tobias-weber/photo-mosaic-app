import {Component, inject, signal, viewChild} from '@angular/core';
import {ApiService} from '../../../services/api.service';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {rxResource, toObservable, toSignal} from '@angular/core/rxjs-interop';
import {filter, switchMap} from 'rxjs';
import {AuthService} from '../../../services/auth.service';
import {DatePipe, NgClass} from '@angular/common';
import {ImageUploaderComponent} from '../../image-uploader/image-uploader.component';
import {ImageListComponent} from '../../image-list/image-list.component';
import {DropZoneComponent} from '../../image-uploader/drop-zone/drop-zone.component';
import {ToastService} from '../../../services/toast.service';

@Component({
    selector: 'app-project',
    imports: [
        DatePipe,
        RouterLink,
        ImageUploaderComponent,
        NgClass,
        ImageListComponent
    ],
    templateUrl: './project.component.html',
    styleUrl: './project.component.css'
})
export class ProjectComponent {
    private route = inject(ActivatedRoute);
    private api = inject(ApiService);
    private auth = inject(AuthService);
    private toast = inject(ToastService);

    imageList = viewChild(ImageListComponent);

    userName = this.auth.userName;
    projectId = signal<string | null>(null);
    project = toSignal(
        toObservable(this.projectId).pipe(
            filter(projectId => !!projectId),
            switchMap(projectId => this.api.getProject(this.auth.userName()!, projectId!))
        )
    );
    isSelectingTargets = signal(true);

    constructor() {
        this.route.params.subscribe((params) => {
            this.projectId.set(params['projectId']);
        });
    }

    createMosaic() {
        this.api.getImageRefs(this.userName()!, this.projectId()!, 'TARGETS').subscribe(images => {
            if (images.length === 0) {
                this.toast.error('No target images available.');
            }
            this.api.createJob(this.userName()!, this.projectId()!, images[0].imageId).subscribe({
                next: (result) => console.log(result),
                error: error => console.log(error),
            })
        })
    }
}
