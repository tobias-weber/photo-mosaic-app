import {Component, inject, signal, WritableSignal} from '@angular/core';
import {ApiService} from '../../services/api.service';
import {FormsModule} from '@angular/forms';
import {NgbAlert} from '@ng-bootstrap/ng-bootstrap';

@Component({
    selector: 'app-endpoint-tester',
    imports: [
        FormsModule,
        NgbAlert
    ],
    templateUrl: './endpoint-tester.component.html',
    styleUrl: './endpoint-tester.component.css'
})
export class EndpointTesterComponent {

    private _api = inject(ApiService);
    endpoint = signal('weatherforecast');
    result: WritableSignal<string | null> = signal(null);
    error: WritableSignal<string | null> = signal(null);

    sendGet() {
        this._api.get(this.endpoint()).subscribe({
            next: data => {
                this.error.set(null);
                this.result.set(JSON.stringify(data));
            },
            error: err => {
                console.log('we got an error', err);
                this.result.set(null);
                this.error.set(err.message);
            },
        })
    }


    register() {
        this._api.register(this.endpoint(), 'password').subscribe({
            next: data => {
                console.log(data)
                localStorage.setItem("jwt_token", data.token);
            },
        })
    }

    login() {
        this._api.login(this.endpoint(), 'password').subscribe({
            next: data => {
                console.log(data)
                localStorage.setItem("jwt_token", data.token);
            },
        })
    }

    logout() {
        localStorage.removeItem("jwt_token");
    }
}
