import {computed, inject, Injectable, signal} from '@angular/core';
import {ApiService, AuthResponse} from './api.service';
import {Router} from '@angular/router';
import {catchError, filter, Observable, of, switchMap, tap, timer} from 'rxjs';
import {toObservable, toSignal} from '@angular/core/rxjs-interop';
import {HttpEvent, HttpHandlerFn, HttpRequest} from '@angular/common/http';


export const maxNameLength = 128;
export const minPwLength = 6;
export const maxPwLength = 128;
export const tokenKey = 'auth_token';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private readonly expirationKey = 'auth_expiration';

    private api = inject(ApiService);
    private router = inject(Router);

    private isRefreshing = false;

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

    isLoggedIn = computed(() => {
        return this.token() !== null;
         //new Date(exp) > new Date(); no longer needed because of refresh token
    });
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

    refreshThenRepeatRequest(req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> {
        if (!this.isRefreshing) { // avoid infinite loop
            this.isRefreshing = true;
            return this.api.refreshAccessToken().pipe(
                switchMap((res) => {
                    this.setSession(res);
                    this.isRefreshing = false;
                    return next(this.getRequestWithAuthHeader(req))
                }),
                catchError(err => { // unable to refresh access token
                    this.isRefreshing = false;
                    this.logout();
                    throw err;
                })
            );
        }
        return timer(500).pipe(switchMap(() => next(this.getRequestWithAuthHeader(req)))); // wait for refresh operation to finish
    }

    getRequestWithAuthHeader(req: HttpRequest<unknown>) {
        if (this.token()) {
            return req.clone({
                headers: req.headers.set('Authorization', `Bearer ${this.token()}`)
            });
        }
        return req;
    }

    private setSession(auth: AuthResponse) {
        localStorage.setItem(tokenKey, auth.token);
        localStorage.setItem(this.expirationKey, auth.expiration);
        this._token.set(auth.token);
        this._expiration.set(auth.expiration);
    }

}

export interface UserInfo {
    username: string;
    role: 'User' | 'Admin';
}
