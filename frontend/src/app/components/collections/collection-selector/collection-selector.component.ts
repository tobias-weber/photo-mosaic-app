import {Component, inject, signal} from '@angular/core';
import {ToastService} from '../../../services/toast.service';
import {ApiService, CollectionStatus, TileCollection} from '../../../services/api.service';
import {ProjectService} from '../../../services/project.service';
import {FormsModule} from '@angular/forms';
import {NgClass} from '@angular/common';

@Component({
    selector: 'app-collection-selector',
    imports: [
        FormsModule,
        NgClass
    ],
    templateUrl: './collection-selector.component.html',
    styleUrl: './collection-selector.component.css'
})
export class CollectionSelectorComponent {
    private toast = inject(ToastService);
    private api = inject(ApiService);
    private projectService = inject(ProjectService);

    targetUser = this.projectService.targetUser;
    projectId = this.projectService.projectId;

    collections = signal<TileCollection[]>([]);
    selectedCollectionIds = signal<string[]>([]);

    constructor() {
        this.loadCollections();
    }

    private loadCollections() {
        this.api.getCollections().subscribe({
            next: result => this.collections.set(result),
            error: error => this.toast.error(`Unable to load collections: ${error.message}`),
        });
        this.api.getSelectedCollections(this.targetUser(), this.projectId()!).subscribe({
            next: result => this.selectedCollectionIds.set(result),
            error: error => this.toast.error(`Unable to load selected collections: ${error.message}`)
        });
    }

    toggleSelection(collectionId: string) {
        if (this.selectedCollectionIds().includes(collectionId)) {
            this.api.deselectCollection(this.targetUser(), this.projectId()!, collectionId).subscribe({
                next: result => this.selectedCollectionIds.set(result),
                error: error => this.toast.error(`Unable to select collection: ${error.message}`)
            });
        } else {
            this.api.selectCollection(this.targetUser(), this.projectId()!, collectionId).subscribe({
                next: result => this.selectedCollectionIds.set(result),
                error: error => this.toast.error(`Unable to select collection: ${error.message}`)
            });
        }
    }

    protected readonly CollectionStatus = CollectionStatus;
}
