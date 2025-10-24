import {computed, inject, Injectable, signal} from '@angular/core';
import {ApiService, AuthResponse} from './api.service';
import {Router} from '@angular/router';
import {BehaviorSubject, catchError, filter, finalize, Observable, of, switchMap, take, tap, timer} from 'rxjs';
import {toObservable, toSignal} from '@angular/core/rxjs-interop';
import {HttpEvent, HttpHandlerFn, HttpRequest} from '@angular/common/http';


export const maxNameLength = 128;
export const minPwLength = 6;
export const maxPwLength = 128;
export const tokenKey = 'auth_token';
export const refreshBeforeExpirationMs = 120 * 1000;

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private readonly expirationKey = 'auth_expiration';

    private api = inject(ApiService);
    private router = inject(Router);

    private refreshSubject = new BehaviorSubject<boolean>(false);

    private _token = signal<string | null>(localStorage.getItem(tokenKey));
    private _expiration = signal<string | null>(localStorage.getItem(this.expirationKey));

    get token() {
        return this._token.asReadonly();
    }

    get expiration() {
        return this._expiration.asReadonly();
    }

    private payload = computed(() => {
        const token = this._token();
        if (!token) return null;

        try {
            const payload = token.split('.')[1]; // middle part of JWT
            const decoded = atob(payload);       // decode Base64
            return JSON.parse(decoded);
        } catch {
            return null;
        }
    });

    isLoggedIn = computed(() => this.token() !== null); // we assume we're still logged in even if the auth token expired
    userName = computed<string | null>(() =>
        this.payload()?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || null);
    userRole = computed<string | null>(() =>
        this.payload()?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || null);
    isAdmin = computed(() => this.userRole() === 'Admin');
    user = toSignal(
        toObservable(this.userName).pipe(
            filter(userName => !!userName), // Only proceed if the username is not null
            switchMap(userName => this.api.getUser(userName!).pipe(
                catchError(() => {
                    // log out
                    this.logout();
                    return of(null);
                })
            ))
        )
    );


    register(username: string, password: string) {
        return this.api.register(username, password).pipe(
            tap(res => this.setSession(res))
        );
    }

    login(username: string, password: string) {
        return this.api.login(username, password).pipe(
            tap(res => this.setSession(res))
        )
    }

    guestLogin() {
        return this.login('guest', '')
    }

    logout() {
        this.api.logout().subscribe(() => {
            localStorage.removeItem(tokenKey);
            localStorage.removeItem(this.expirationKey);
            this._token.set(null);
            this._expiration.set(null);
            this.router.navigate(['/login']);
        });
    }

    onRefreshFinished() {
        return this.refreshSubject.pipe(
            filter(isRefreshing => !isRefreshing),
            take(1) // Observable completes as soon as refresh is finished
        );
    }

    triggerRefresh() {
        if (this.refreshSubject.value) {
            return
        }
        this.refreshSubject.next(true);
        this.api.refreshAccessToken().pipe(
            finalize(() => this.refreshSubject.next(false))
        ).subscribe({
            next: res => this.setSession(res),
            error: () => this.logout() // unable to refresh access token
        });
    }

    isRefreshRequest(request: HttpRequest<unknown>) {
        return request.url === this.api.REFRESH_URL;
    }

    tokenExpiresSoon() {
        return this.expiration() &&
            new Date(this.expiration()!).getTime() - refreshBeforeExpirationMs < (Date.now());
    }

    private setSession(auth: AuthResponse) {
        localStorage.setItem(tokenKey, auth.token);
        localStorage.setItem(this.expirationKey, auth.expiration);
        this._token.set(auth.token);
        this._expiration.set(auth.expiration);
    }

}
