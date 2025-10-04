import {Component, inject, Input, OnInit, signal} from '@angular/core';
import {NgbActiveModal} from '@ng-bootstrap/ng-bootstrap';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {ImageRef} from '../../../services/api.service';
import {NgClass} from '@angular/common';

@Component({
    selector: 'app-create-job-modal',
    imports: [
        ReactiveFormsModule,
        NgClass
    ],
    templateUrl: './create-job-modal.component.html',
    styleUrl: './create-job-modal.component.css'
})
export class CreateJobModalComponent implements OnInit {
    protected readonly maxN = 9999;
    protected readonly maxSubdivisions = 7;
    protected readonly maxRepetitions = 5;
    protected readonly maxCropCount = 5;


    activeModal = inject(NgbActiveModal);
    private fb = inject(FormBuilder);

    isExpanded = signal(false);


    @Input() projectId: string | null = null;
    @Input() targetImages: ImageRef[] = []; // TODO: target image previews and ability to start process by selecting target


    form = this.fb.group({
        targetId: ['', [Validators.required]],
        n: [0, [Validators.required, Validators.min(0), Validators.max(this.maxN)]], // 0 = auto
        algorithm: ['LAP', [Validators.required]],

        // LAP-specific controls:
        colorSpace: ['CIELAB', [Validators.required]],
        subdivisions: [7, [Validators.required, Validators.min(1), Validators.max(this.maxSubdivisions)]],
        repetitions: [1, [Validators.required, Validators.min(1), Validators.max(this.maxRepetitions)]],
        cropCount: [1, [Validators.required, Validators.min(1), Validators.max(this.maxCropCount)]],
    });


    get tid() {
        return this.form.get('targetId')!;
    }

    get n() {
        return this.form.get('n')!;
    }

    get alg() {
        return this.form.get('algorithm')!;
    }

    get cs() {
        return this.form.get('colorSpace')!;
    }

    get sd() {
        return this.form.get('subdivisions')!;
    }

    get rep() {
        return this.form.get('repetitions')!;
    }

    get cc() {
        return this.form.get('cropCount')!;
    }

    ngOnInit(): void {
        if (this.targetImages.length > 0) {
            this.tid.setValue(this.targetImages[0].imageId);
        }
    }

    submit() {
        if (this.form.invalid) return;
        this.activeModal.close(this.form.value)
    }

    toggleExpanded() {
        this.isExpanded.update(v => !v)
    }
}
