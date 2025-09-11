import {computed, inject, Injectable, signal} from '@angular/core';
import {ApiService, AuthResponse} from './api.service';
import {Router} from '@angular/router';
import {tap} from 'rxjs';


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

    private token = signal<string | null>(localStorage.getItem(tokenKey));
    private expiration = signal<string | null>(localStorage.getItem(this.expirationKey));
    private payload = computed(() => {
        const token = this.token();
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
        const exp = this.expiration();
        if (!this.token() || !exp) return false;
        return new Date(exp) > new Date();
    });

    user = computed<UserInfo | null>(() => {
        const payload = this.payload();
        if (payload) {
            return {
                username: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'],
                role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
            }
        }
        return null;
    });

    isAdmin = computed(() => this.user()?.role === 'Admin');


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

    logout() {
        localStorage.removeItem(tokenKey);
        localStorage.removeItem(this.expirationKey);
        this.token.set(null);
        this.expiration.set(null);
        this.router.navigate(['/login']);
    }

    private setSession(auth: AuthResponse) {
        localStorage.setItem(tokenKey, auth.token);
        localStorage.setItem(this.expirationKey, auth.expiration);
        this.token.set(auth.token);
        this.expiration.set(auth.expiration);
    }

}

export interface UserInfo {
    username: string;
    role: 'User' | 'Admin';
}
