import {Component, signal} from '@angular/core';
import {RouterOutlet} from '@angular/router';
import {EndpointTesterComponent} from './components/endpoint-tester/endpoint-tester.component';

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, EndpointTesterComponent],
    templateUrl: './app.html',
    styleUrl: './app.css'
})
export class App {
    protected readonly title = signal('frontend');
}
