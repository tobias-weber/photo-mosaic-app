import {Routes} from '@angular/router';
import {LoginComponent} from './components/auth/login/login.component';
import {RegisterComponent} from './components/auth/register/register.component';
import {DashboardComponent} from './components/dashboard/dashboard.component';
import {authGuard} from './components/auth/auth-guard';
import {ProjectComponent} from './components/projects/project/project.component';
import {ProjectListComponent} from './components/projects/project-list/project-list.component';
import {JobComponent} from './components/mosaics/job/job.component';

export const routes: Routes = [
    {path: '', redirectTo: '/dashboard', pathMatch: 'full'},
    {path: 'login', component: LoginComponent},
    {path: 'register', component: RegisterComponent},
    {path: 'dashboard', component: DashboardComponent, canActivate: [authGuard]},
    {path: 'projects', component: ProjectListComponent, canActivate: [authGuard]},
    {path: 'projects/:projectId', component: ProjectComponent, canActivate: [authGuard]},
    {path: 'projects/:projectId/j/:jobId', component: JobComponent, canActivate: [authGuard]},
    {path: '**', redirectTo: '/dashboard'} // TODO: 404 page
];
