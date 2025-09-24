import {inject, Injectable, TemplateRef} from '@angular/core';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {ConfirmModalComponent} from '../components/confirm-modal/confirm-modal.component';

@Injectable({
    providedIn: 'root'
})
export class ModalService {
    private ngbModal = inject(NgbModal);

    async openConfirmModal(message: string, title?: string, btnText?: string, btnClass?: string): Promise<boolean> {
        const modalRef = this.ngbModal.open(ConfirmModalComponent);
        modalRef.componentInstance.message = message;
        if (title) {
            modalRef.componentInstance.title = title;
        }
        if (btnText) {
            modalRef.componentInstance.btnText = btnText;
        }
        if (btnClass) {
            modalRef.componentInstance.btnClass = btnClass;
        }

        try {
            await modalRef.result;
            return true;
        } catch (e) {
            return false;
        }
    }

    async openTemplateModal(content: TemplateRef<any>) {
        const modalRef = this.ngbModal.open(content);
        try {
            return await modalRef.result;
        } catch (e) {
            return null;
        }
    }

    async openComponentModal<T>(component: any, context?: Record<string, any>): Promise<T | null> {
        const modalRef = this.ngbModal.open(component);
        if (context) {
            Object.entries(context).forEach(([key, value]) => {
                if (key in modalRef.componentInstance) {
                    (modalRef.componentInstance)[key] = value;
                }
            });
        }

        try {
            return (await modalRef.result) as T;
        } catch (e) {
            return null;
        }
    }

}
