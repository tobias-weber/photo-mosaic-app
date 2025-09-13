import {inject, Injectable} from '@angular/core';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {ConfirmModalComponent} from '../components/confirm-modal/confirm-modal.component';

@Injectable({
    providedIn: 'root'
})
export class ModalService {
    private ngbModal = inject(NgbModal);

    async openConfirmModal(message: string, title?: string, btnText?: string): Promise<boolean> {
        const modalRef = this.ngbModal.open(ConfirmModalComponent);
        modalRef.componentInstance.message = message;
        if (title) {
            modalRef.componentInstance.title = title;
        }
        if (btnText) {
            modalRef.componentInstance.btnText = btnText;
        }

        try {
            await modalRef.result;
            return true;
        } catch (e) {
            return false;
        }
    }

}
