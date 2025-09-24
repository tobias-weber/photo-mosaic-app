import {Component, inject, Input} from '@angular/core';
import {NgbActiveModal} from '@ng-bootstrap/ng-bootstrap';
import {NgClass} from '@angular/common';

@Component({
    selector: 'app-confirm-modal',
    imports: [
        NgClass
    ],
    templateUrl: './confirm-modal.component.html',
    styleUrl: './confirm-modal.component.css'
})
export class ConfirmModalComponent {
    activeModal = inject(NgbActiveModal);

    @Input() title: string = 'Confirm Action';
    @Input() message: string = '';
    @Input() btnText: string = 'Confirm';
    @Input() btnClass: string = 'btn-danger';

}
