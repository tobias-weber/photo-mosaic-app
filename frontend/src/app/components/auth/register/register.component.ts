import {Component, inject, signal} from '@angular/core';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {AuthService, maxNameLength, maxPwLength, minPwLength} from '../../../services/auth.service';
import {Router} from '@angular/router';
import {passwordMatchValidator} from '../../../helpers/Validators';

@Component({
    selector: 'app-register',
    imports: [
        ReactiveFormsModule
    ],
    templateUrl: './register.component.html',
    styleUrl: './register.component.css'
})
export class RegisterComponent {
    protected readonly maxNameLength = maxNameLength;
    protected readonly minPwLength = minPwLength;
    protected readonly maxPwLength = maxPwLength;

    private fb = inject(FormBuilder);
    private auth = inject(AuthService);
    private router = inject(Router);

    form = this.fb.group({
        username: ['', [Validators.required, Validators.maxLength(maxNameLength), Validators.pattern('[a-zA-Z0-9]*')]],
        password: ['', [Validators.required, Validators.minLength(minPwLength), Validators.maxLength(maxPwLength)]],
        confirmPassword: ['', [Validators.required]]
    }, {validators: passwordMatchValidator});

    get name() {
        return this.form.get('username');
    }

    get pw() {
        return this.form.get('password');
    }

    get confirmPw() {
        return this.form.get('confirmPassword');
    }

    error = signal('');

    submit() {
        if (this.form.invalid) return;

        const {username, password, confirmPassword} = this.form.value;
        if (!username || !password || !confirmPassword || password !== confirmPassword) return;

        this.auth.register(username, password).subscribe({
            next: () => {
                this.router.navigate(['/projects']);
            },
            error: err => {
                this.error.set(`Registration failed${err.error?.[0].description && (': ' + err.error[0].description)}`);
            }
        });
    }

}
