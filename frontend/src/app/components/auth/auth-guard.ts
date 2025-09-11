import {CanActivateFn, Router} from '@angular/router';
import {inject} from '@angular/core';
import {AuthService} from '../../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
    const auth = inject(AuthService);
    const router: Router = inject(Router);
    if (auth.isLoggedIn()) {
        return true; // user is authenticated
    }
    return router.parseUrl('/login');
};
