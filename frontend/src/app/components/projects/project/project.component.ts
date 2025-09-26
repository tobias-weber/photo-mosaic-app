import {Component, inject, signal} from '@angular/core';
import {RouterLink} from '@angular/router';
import {DatePipe, NgClass} from '@angular/common';
import {ImageUploaderComponent} from '../../image-uploader/image-uploader.component';
import {ImageListComponent} from '../../image-list/image-list.component';
import {JobListComponent} from '../../mosaics/job-list/job-list.component';
import {ProjectService} from '../../../services/project.service';
import {CollectionSelectorComponent} from '../../collections/collection-selector/collection-selector.component';

@Component({
    selector: 'app-project',
    imports: [
        DatePipe,
        RouterLink,
        ImageUploaderComponent,
        NgClass,
        ImageListComponent,
        JobListComponent,
        CollectionSelectorComponent
    ],
    templateUrl: './project.component.html',
    styleUrl: './project.component.css'
})
export class ProjectComponent {
    private projectService = inject(ProjectService);

    project = this.projectService.project;

    isSelectingTargets = signal(true);
}
