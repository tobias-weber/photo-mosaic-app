import {Component, EventEmitter, inject, input, Output, signal, viewChild} from '@angular/core';
import {concatMap, finalize, from, toArray} from 'rxjs';
import {DropZoneComponent} from './drop-zone/drop-zone.component';
import {ToastService} from '../../services/toast.service';
import {ApiService} from '../../services/api.service';

@Component({
    selector: 'app-image-uploader',
    imports: [
        DropZoneComponent
    ],
    templateUrl: './image-uploader.component.html',
    styleUrl: './image-uploader.component.css'
})
export class ImageUploaderComponent {
    private toast = inject(ToastService);
    private api = inject(ApiService);

    isSelectingTargets = input.required<boolean>();
    targetUser = input.required<string>();
    projectId = input.required<string>();
    @Output() uploadFinished = new EventEmitter<void>();

    dropZone = viewChild(DropZoneComponent);

    selectedFiles = signal<File[]>([]);
    isUploading = signal(false);

    onFilesSelected(files: File[]): void {
        this.selectedFiles.set(files);
    }

    onUpload(): void {
        if (this.selectedFiles().length === 0) {
            return;
        }

        this.isUploading.set(true);

        from(this.selectedFiles()).pipe(
            concatMap(file =>
                this.api.uploadImage(this.targetUser(), this.projectId(), file, this.isSelectingTargets())
            ),
            toArray(), // gathers all results once all uploads complete
            finalize(() => {
                this.isUploading.set(false);
                this.uploadFinished.emit();
            })
        ).subscribe({
            next: (results) => {
                this.toast.success(`${results.length} image${results.length > 1 ? 's' : ''} uploaded successfully.`);
                this.dropZone()?.clearFiles();
            },
            error: (err) => {
                console.error('Upload failed:', err);
                this.toast.error('Upload failed. Please check the console and try again.');
            }
        });
    }
}
