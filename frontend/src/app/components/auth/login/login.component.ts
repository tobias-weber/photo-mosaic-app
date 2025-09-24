import {Component, inject, signal} from '@angular/core';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AuthService, maxNameLength, maxPwLength, minPwLength} from '../../../services/auth.service';
import {ToastService} from '../../../services/toast.service';
import {ModalService} from '../../../services/modal.service';

@Component({
    selector: 'app-login',
    imports: [
        ReactiveFormsModule,
        RouterLink
    ],
    templateUrl: './login.component.html',
    styleUrl: './login.component.css'
})
export class LoginComponent {
    private fb = inject(FormBuilder);
    protected auth = inject(AuthService);
    private router = inject(Router);
    private route = inject(ActivatedRoute);
    private toast = inject(ToastService);
    private modal = inject(ModalService);

    form = this.fb.group({
        username: ['', [Validators.required, Validators.maxLength(maxNameLength), Validators.pattern('[a-zA-Z0-9]*')]],
        password: ['', [Validators.required, Validators.minLength(minPwLength), Validators.maxLength(maxPwLength)]]
    });

    error = signal('');

    submit() {
        if (this.form.invalid) return;

        const {username, password} = this.form.value;
        if (!username || !password) return;

        this.auth.login(username, password).subscribe({
            next: () => this.navigateToReturnUrl(),
            error: () => this.error.set('Invalid username or password')
        });
    }

    logout() {
        this.auth.logout();
    }

    async loginAsGuest() {
        if (await this.modal.openConfirmModal(
            "Note that as a guest all of your projects will be publicly accessible by any other guests.",
            "Continue as Guest?", "Continue", "btn-primary"))
            this.auth.guestLogin().subscribe({
                next: () => this.navigateToReturnUrl(),
                error: () => this.toast.error('Unable to continue as guest.')
            });
    }

    private navigateToReturnUrl() {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/dashboard';
        this.router.navigateByUrl(returnUrl);
    }
}
