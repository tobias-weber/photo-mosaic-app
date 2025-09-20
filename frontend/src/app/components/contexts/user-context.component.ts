import {Component, inject, OnInit} from '@angular/core';
import {ActivatedRoute, RouterOutlet} from '@angular/router';
import {UserContextService} from '../../services/user-context.service';

@Component({
    selector: 'app-user-context',
    imports: [
        RouterOutlet
    ],
    template: `
        <router-outlet></router-outlet>`,
    providers: [UserContextService]
})
export class UserContextComponent implements OnInit {
    private route = inject(ActivatedRoute);
    private userContext = inject(UserContextService);

    ngOnInit(): void {
        this.userContext.initialize(this.route);
    }
}
