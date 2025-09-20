import {computed, inject, Injectable, signal} from '@angular/core';
import {AuthService} from './auth.service';
import {ActivatedRoute} from '@angular/router';
import {distinctUntilChanged, map} from 'rxjs';

@Injectable()
export class UserContextService {
    private auth = inject(AuthService);

    private userFromRoute = signal<string | null>(null);
    readonly targetUser = computed(() => this.userFromRoute?.() || this.auth.userName()!);

    initialize(route: ActivatedRoute): void {
        route.paramMap.pipe(
            map(params => params.get('username')),
            distinctUntilChanged()
        ).subscribe(username => this.userFromRoute.set(username));
    }
}
