import {Component, inject, OnDestroy, signal} from '@angular/core';
import {ApiService, CollectionStatus, TileCollection} from '../../../services/api.service';
import {ToastService} from '../../../services/toast.service';
import {DatePipe} from '@angular/common';

@Component({
    selector: 'app-collection-list',
    imports: [
        DatePipe
    ],
    templateUrl: './collection-list.component.html',
    styleUrl: './collection-list.component.css'
})
export class CollectionListComponent implements OnDestroy {
    protected readonly CollectionStatus = CollectionStatus;

    private api = inject(ApiService);
    private toast = inject(ToastService);

    collections = signal<TileCollection[]>([]);
    installingId = signal<string | null>(null);
    collectionsRefreshInterval: number | null = null;

    constructor() {
        this.loadCollections();
    }

    private loadCollections() {
        this.api.getCollections().subscribe({
            next: result => this.collections.set(result),
            error: error => this.toast.error(`Unable to load collections: ${error.message}`),
        });
    }

    install(id: string) {
        this.api.installCollection(id).subscribe({
            next: () => {
                this.toast.info(`Installing collection...`);
                this.checkInstallation(id);
            },
            error: error => this.toast.error(`Unable to start installation: ${error.message}`),
        })
    }

    uninstall(id: string) {
        this.api.uninstallCollection(id).subscribe({
            next: () => {
                this.toast.success(`Successfully uninstalled collection`);
                this.loadCollections();
            },
            error: error => this.toast.error(`Unable to start installation: ${error.message}`),
        })

    }

    private checkInstallation(id: string) {
        this.installingId.set(id);

        this.api.getCollections().subscribe({
            next: result => {
                this.collections.set(result);
                this.collectionsRefreshInterval = setInterval(() => {

                    if (this.collections().find(c => c.id === id)?.status === CollectionStatus.Downloading) {
                        this.loadCollections();
                    } else {
                        if (this.collectionsRefreshInterval != null) {
                            clearInterval(this.collectionsRefreshInterval);
                            this.collectionsRefreshInterval = null;
                        }
                        this.installingId.set(null);
                    }
                }, 1000);
            },
            error: error => this.toast.error(`Unable to load collections: ${error.message}`),
        });
    }

    ngOnDestroy(): void {
        if (this.collectionsRefreshInterval != null) {
            clearInterval(this.collectionsRefreshInterval);
        }
    }
}
