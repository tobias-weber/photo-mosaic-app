import {Component} from '@angular/core';
import {EndpointTesterComponent} from "../endpoint-tester/endpoint-tester.component";

@Component({
    selector: 'app-home',
    imports: [
        EndpointTesterComponent
    ],
    templateUrl: './home.component.html',
    styleUrl: './home.component.css'
})
export class HomeComponent {

}
