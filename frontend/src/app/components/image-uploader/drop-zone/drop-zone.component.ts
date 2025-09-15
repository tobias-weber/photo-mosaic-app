import {
    Component,
    computed,
    ElementRef,
    EventEmitter, HostBinding,
    HostListener,
    inject, input, OnDestroy,
    Output,
    signal,
    viewChild
} from '@angular/core';
import {DomSanitizer, SafeUrl} from '@angular/platform-browser';
import {ToastService} from '../../../services/toast.service';


export interface FilePreview {
    file: File;
    previewUrl: SafeUrl | null;
}

@Component({
  selector: 'app-drop-zone',
  imports: [],
  templateUrl: './drop-zone.component.html',
  styleUrl: './drop-zone.component.scss'
})
export class DropZoneComponent implements OnDestroy{
    protected readonly maxFiles = 500;
    private readonly maxPreviews = 250;
    protected readonly allowedFileTypes = [
        'image/jpeg',
        'image/png',
        'image/gif',
    ];

    private sanitizer = inject(DomSanitizer); // safely bind object URLs to image src
    private toast = inject(ToastService);

    multiple = input(false);
    disabled = input(false);
    @Output() filesSelected = new EventEmitter<File[]>();

    fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');

    isDragging = signal(false);
    files = signal<FilePreview[]>([]);
    hasFiles = computed(() => this.files().length > 0);
    showPreviews = signal(false);

    // --- Host Listeners for Drag & Drop ---
    @HostListener('dragover', ['$event'])
    onDragOver(event: DragEvent): void {
        event.preventDefault();
        event.stopPropagation();
        this.isDragging.set(true);
    }

    @HostListener('dragleave', ['$event'])
    onDragLeave(event: DragEvent): void {
        event.preventDefault();
        event.stopPropagation();
        this.isDragging.set(false);
    }

    @HostListener('drop', ['$event'])
    onDrop(event: DragEvent): void {
        event.preventDefault();
        event.stopPropagation();
        this.isDragging.set(false);

        if (this.hasFiles() || this.disabled()) {
            return;
        }

        const files = event.dataTransfer?.files;
        if (files && files.length > 0) {
            let allowedFiles = Array.from(files)
                .filter(file => this.allowedFileTypes.includes(file.type));
            const removedCount = files.length - allowedFiles.length;
            if (removedCount > 0) {
                this.toast.warning(`${removedCount} file(s) with unsupported types have been excluded.`)
            }
            if (!this.multiple() && allowedFiles.length > 1) {
                allowedFiles = [allowedFiles[0]];
            }
            if (allowedFiles.length > 0) {
                this.handleFiles(allowedFiles);
            }
        }
    }

    @HostBinding('class.is-dragging')
    get draggingClass() {
        return this.isDragging();
    }

    onZoneClicked(): void {
        if (!this.disabled()) {
            this.fileInput()?.nativeElement.click();
        }
    }

    onFileSelected(event: Event): void {
        const element = event.target as HTMLInputElement;
        const files = element.files;
        if (files && files.length > 0) {
            this.handleFiles(files);
        }
    }

    private handleFiles(files: FileList | File[]): void {
        if (files.length > this.maxFiles) {
            this.toast.warning(`At most ${this.maxFiles} images can be uploaded at once.`)
            return;
        }

        this.showPreviews.set(files.length <= this.maxPreviews);
        const newFiles: FilePreview[] = Array.from(files).map(file => ({
            file: file,
            // Create a temporary, safe URL for the image preview
            previewUrl: this.showPreviews() ? this.sanitizer.bypassSecurityTrustUrl(URL.createObjectURL(file)) : null
        }));
        this.files.set(newFiles);
        this.filesSelected.emit(newFiles.map(f => f.file));
    }

    // Allow removing a single file from the preview list
    removeFile(fileToRemove: FilePreview): void {
        if (fileToRemove.previewUrl) {
            URL.revokeObjectURL(fileToRemove.previewUrl as string)
        }
        this.files.update(currentFiles =>
            currentFiles.filter(f => f !== fileToRemove)
        );
        this.filesSelected.emit(this.files().map(f => f.file));
    }

    clearFiles(): void {
        // Revoke object URLs to prevent memory leaks
        this.files().forEach(f => URL.revokeObjectURL(f.previewUrl as string));
        this.files.set([]);
        this.filesSelected.emit([]);
    }

    ngOnDestroy(): void {
        this.clearFiles();
    }
}
