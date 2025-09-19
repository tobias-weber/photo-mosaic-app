import {Component, inject, Input} from '@angular/core';
import {NgbActiveModal} from '@ng-bootstrap/ng-bootstrap';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';

@Component({
    selector: 'app-create-job-modal',
    imports: [
        ReactiveFormsModule
    ],
    templateUrl: './create-job-modal.component.html',
    styleUrl: './create-job-modal.component.css'
})
export class CreateJobModalComponent {
    protected readonly maxN = 9999;
    protected readonly maxSubdivisions = 7;

    activeModal = inject(NgbActiveModal);
    private fb = inject(FormBuilder);


    @Input() projectId: string | null = null;
    // TODO: ability to select target image from list of images, using a route-scoped ProjectService


    form = this.fb.group({
        targetId: ['', [Validators.required]],
        n: [0, [Validators.required, Validators.min(0), Validators.max(this.maxN)]], // 0 = auto
        algorithm: ['LAP', [Validators.required]],

        // LAP-specific controls:
        subdivisions: [1, [Validators.required, Validators.min(1), Validators.max(this.maxSubdivisions)]]
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


    get sd() {
        return this.form.get('subdivisions')!;
    }

    submit() {
        if (this.form.invalid) return;
        this.activeModal.close(this.form.value)
    }
}
