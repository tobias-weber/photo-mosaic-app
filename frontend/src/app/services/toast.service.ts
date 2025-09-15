import {Injectable, signal} from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class ToastService {
    // See https://ng-bootstrap.github.io/#/components/toast/examples
    private readonly _toasts = signal<Toast[]>([]);
    readonly toasts = this._toasts.asReadonly();

    show(toast: Toast) {
        this._toasts.update((toasts) => [...toasts, toast]);
    }

    success(message: string, delay?: number) {
        this.show({
            message,
            classname: 'bg-success bg-opacity-75 text-light',
            delay
        })
    }

    info(message: string, delay?: number) {
        this.show({
            message,
            classname: 'bg-secondary text-light',
            delay
        })
    }

    warning(message: string, delay?: number) {
        this.show({
            message,
            classname: 'bg-warning text-warning-emphasis',
            delay
        })
    }

    error(message: string, delay?: number) {
        this.show({
            message,
            classname: 'bg-danger text-light',
            delay
        })
    }

    remove(toast: Toast) {
        this._toasts.update((toasts) => toasts.filter((t) => t !== toast));
    }

    clear() {
        this._toasts.set([]);
    }
}

export interface Toast {
    message: string;
    classname?: string;
    delay?: number;
}
