import {Component, inject} from '@angular/core';
import {AuthService} from '../../services/auth.service';
import {JsonPipe} from '@angular/common';
import {ProjectListComponent} from '../projects/project-list/project-list.component';
import {ReactiveFormsModule} from '@angular/forms';

@Component({
    selector: 'app-dashboard',
    imports: [
        JsonPipe,
        ProjectListComponent,
        ReactiveFormsModule
    ],
    templateUrl: './dashboard.component.html',
    styleUrl: './dashboard.component.css'
})
export class DashboardComponent {

    private auth = inject(AuthService);

    isLoggedIn = this.auth.isLoggedIn;
    user = this.auth.user;
}
