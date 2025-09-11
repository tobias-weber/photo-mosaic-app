import {HttpInterceptorFn} from '@angular/common/http';
import {tokenKey} from '../../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const token = localStorage.getItem(tokenKey);

    if (token) {
        const clonedRequest = req.clone({
            headers: req.headers.set('Authorization', `Bearer ${token}`)
        });
        return next(clonedRequest);
    }

    // If no token exists, just pass the original request
    return next(req);
};
