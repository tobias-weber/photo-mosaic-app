import {Component, inject} from '@angular/core';
import {AuthService} from '../../services/auth.service';
import {RouterLink, RouterLinkActive} from '@angular/router';

@Component({
    selector: 'app-navbar',
    imports: [
        RouterLink,
        RouterLinkActive
    ],
    templateUrl: './navbar.component.html',
    styleUrl: './navbar.component.css'
})
export class NavbarComponent {
    private auth = inject(AuthService);

    isLoggedIn = this.auth.isLoggedIn; // signal
    user = this.auth.user;      // signal function returning payload

    logout() {
        this.auth.logout();
    }

}
