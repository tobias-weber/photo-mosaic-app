import {Component} from '@angular/core';
import {RouterOutlet} from '@angular/router';
import {ProjectService} from '../../services/project.service';

@Component({
    selector: 'app-project-context',
    template: `
        <router-outlet></router-outlet>`,
    imports: [
        RouterOutlet
    ],
    providers: [ProjectService]
})
export class ProjectContextComponent {
}
