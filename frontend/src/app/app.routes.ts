import {Routes} from '@angular/router';
import {LoginComponent} from './components/auth/login/login.component';
import {RegisterComponent} from './components/auth/register/register.component';
import {adminGuard, authGuard} from './components/auth/auth-guard';
import {ProjectComponent} from './components/projects/project/project.component';
import {ProjectListComponent} from './components/projects/project-list/project-list.component';
import {JobComponent} from './components/mosaics/job/job.component';
import {UserListComponent} from './components/user-list/user-list.component';
import {UserContextComponent} from './components/contexts/user-context.component';
import {ProjectContextComponent} from './components/contexts/project-context.component';
import {CollectionListComponent} from './components/collections/collection-list/collection-list.component';
import {NotFoundComponent} from './components/not-found/not-found.component';

const projectRoutes: Routes = [
    {path: '', component: ProjectListComponent},
    {
        path: ':projectId',
        component: ProjectContextComponent, // adding project
        children: [
            {path: '', component: ProjectComponent},
            {path: 'j/:jobId', component: JobComponent},
        ]
    }
];

export const routes: Routes = [
    {path: '', redirectTo: '/projects', pathMatch: 'full'},
    {path: 'login', component: LoginComponent},
    {path: 'register', component: RegisterComponent},
    {path: 'projects', component: UserContextComponent, children: projectRoutes, canActivate: [authGuard]},
    {
        path: 'users',
        children: [
            {path: '', component: UserListComponent},
            {
                path: ':username',
                component: UserContextComponent, // adding the context here to be able to access the :username param
                children: [{path: 'projects', children: projectRoutes}]
            }
        ],
        canActivate: [adminGuard]
    },
    {path: 'collections', component: CollectionListComponent, canActivate: [authGuard]},
    {path: '**', component: NotFoundComponent}
];
