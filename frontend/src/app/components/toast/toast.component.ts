import {Component, inject} from '@angular/core';
import {ToastService} from '../../services/toast.service';
import {NgbToast} from '@ng-bootstrap/ng-bootstrap';

@Component({
    selector: 'app-toast',
    imports: [
        NgbToast
    ],
    templateUrl: './toast.component.html',
    styleUrl: './toast.component.css'
})
export class ToastComponent {
    readonly defaultDelay: number = 5000;
    toastService = inject(ToastService);

    // See https://ng-bootstrap.github.io/#/components/toast/examples#prevent-autohide
    autohide = true;

}
