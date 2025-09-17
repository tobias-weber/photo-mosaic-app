import {Component, EventEmitter, inject, input, Output, signal, viewChild} from '@angular/core';
import {concatMap, delay, finalize, from, mergeMap, retry, tap, timeout, toArray} from 'rxjs';
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
    uploadCount = signal(0);

    onFilesSelected(files: File[]): void {
        this.selectedFiles.set(files);
    }

    onUpload(): void {
        if (this.selectedFiles().length === 0) {
            return;
        }

        this.uploadCount.set(0);
        this.isUploading.set(true);

        const uploadedFiles: File[] = [];
        from(this.selectedFiles()).pipe(
            mergeMap(
                file => this.api.uploadImage(this.targetUser(), this.projectId(), file, this.isSelectingTargets()).pipe(
                    tap(() => {
                        this.uploadCount.update(v => v + 1)
                        uploadedFiles.push(file);
                    }),
                    retry({ count: 2, delay: 1000 })
                ),
                3
            ),
            finalize(() => {
                // cleanup
                this.isUploading.set(false);
                this.uploadFinished.emit();
            })
        ).subscribe({
            error: (err) => {
                // 1 image and retries failed, aborting the rest
                console.error('Upload failed:', err);
                this.toast.error(`Upload failed: ${err.message}`);
                if (this.uploadCount() > 0) {
                    this.dropZone()?.removeFiles(uploadedFiles);
                }
            },
            complete: () => {
                this.toast.success(`${this.uploadCount()} image${this.uploadCount() > 1 ? 's' : ''} uploaded successfully.`);
                this.dropZone()?.clearFiles();
            }
        });
    }
}
