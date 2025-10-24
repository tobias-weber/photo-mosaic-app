import {HttpInterceptorFn, HttpRequest} from '@angular/common/http';
import {AuthService} from '../../services/auth.service';
import {inject} from '@angular/core';
import {switchMap} from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const auth = inject(AuthService);

    if (auth.isRefreshRequest(req)) {
        return next(getRequestWithAuthHeader(auth.token(), req));
    }

    if (auth.tokenExpiresSoon()) {
        auth.triggerRefresh();
    }

    return auth.onRefreshFinished().pipe(switchMap(
        () => next(getRequestWithAuthHeader(auth.token(), req))
    ));
};

function getRequestWithAuthHeader(token: string | null, req: HttpRequest<unknown>) {
    // if a token exist, we add the auth header (even if it might have expired)
    if (token) {
        return req.clone({
            headers: req.headers.set('Authorization', `Bearer ${token}`)
        });
    }
    return req;
}
