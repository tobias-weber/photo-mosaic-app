import {Component, inject, input, OnDestroy, signal, Signal} from '@angular/core';
import {ToastService} from '../../services/toast.service';
import {ApiService, ImageRef} from '../../services/api.service';
import {toObservable, toSignal} from '@angular/core/rxjs-interop';
import {combineLatest, switchMap} from 'rxjs';
import {NgbPopover} from '@ng-bootstrap/ng-bootstrap';
import {ModalService} from '../../services/modal.service';

@Component({
    selector: 'app-image-list',
    imports: [
        NgbPopover
    ],
    templateUrl: './image-list.component.html',
    styleUrl: './image-list.component.css'
})
export class ImageListComponent implements OnDestroy {
    private toast = inject(ToastService);
    private api = inject(ApiService);
    private modals = inject(ModalService);

    isSelectingTargets = input.required<boolean>();
    targetUser = input.required<string>();
    projectId = input.required<string>();
    private refreshTrigger = signal(false);

    imageRefs: Signal<ImageRef[]> = toSignal(
        combineLatest([
            toObservable(this.targetUser),
            toObservable(this.projectId),
            toObservable(this.isSelectingTargets),
            toObservable(this.refreshTrigger)]).pipe(
            switchMap(([user, project, isTarget]) =>
                this.api.getImageRefs(user, project, isTarget ? 'TARGETS' : 'TILES')
            )
        ), {initialValue: []}
    );
    imagePreview = signal<{ url: string, imageRef: ImageRef } | null>(null);
    isLoadingPreview = signal(false);

    refresh() {
        this.refreshTrigger.update(v => !v);
    }

    deleteImage(image: ImageRef) {
        this.api.deleteImage(this.targetUser(), this.projectId(), image.imageId).subscribe({
            next: () => {
                this.toast.success(`Image '${image.name}' was successfully deleted.`);
                this.refresh();
            },
            error: err => this.toast.error(`Unable to delete image '${image.name}': ${err.message}`),
        })
    }

    showPreview(image: ImageRef) {
        this.isLoadingPreview.set(true);
        if (image.imageId !== this.imagePreview()?.imageRef.imageId) {
            this.revokePreview();
            this.api.getImage(this.targetUser(), this.projectId(), image.imageId).subscribe({
                next: blob => {
                    this.imagePreview.set(
                        {url: URL.createObjectURL(blob), imageRef: image}
                    );
                },
                error: err => this.toast.error(`Failed to load image: ${err.message}`),
            })
        }
    }

    hidePreview() {
        this.revokePreview();
    }

    onImageLoad() {
        this.isLoadingPreview.set(false);
    }

    onImageError() {
        this.isLoadingPreview.set(false);
        this.imagePreview.set(null);
    }

    private revokePreview() {
        if (this.imagePreview()) {
            URL.revokeObjectURL(this.imagePreview()?.url!);
        }
        this.imagePreview.set(null);
    }

    ngOnDestroy(): void {
        this.revokePreview();
    }

    async deleteAll() {
        if (await this.modals.openConfirmModal('Do you really want to delete all images?', 'Delete Images', 'Delete')) {
            this.api.deleteImages(this.targetUser(), this.projectId(), this.isSelectingTargets() ? 'TARGETS' : 'TILES' ).subscribe({
                next: () => {
                    this.toast.success(`Successfully deleted ${this.imageRefs().length} images`);
                    this.refresh();
                },
                error: err => this.toast.error(`Unable to delete images: '${err.message}`),
            });
        }
    }
}
