import {HttpErrorResponse, HttpInterceptorFn, HttpRequest} from '@angular/common/http';
import {AuthService} from '../../services/auth.service';
import {inject} from '@angular/core';
import {catchError} from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const authService = inject(AuthService);
    const token = authService.token();
    const expiration = authService.expiration();

    // if a token exist, we add the auth header (even if it might have expired)
    req = authService.getRequestWithAuthHeader(req);

    return next(req).pipe(
        catchError(err => {
            if (err instanceof HttpErrorResponse && err.status === 401) {
                return authService.refreshThenRepeatRequest(req, next)
            }
            throw err;
        })
    );
};
