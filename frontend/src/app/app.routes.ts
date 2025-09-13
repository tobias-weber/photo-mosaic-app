import {Routes} from '@angular/router';
import {LoginComponent} from './components/auth/login/login.component';
import {RegisterComponent} from './components/auth/register/register.component';
import {DashboardComponent} from './components/dashboard/dashboard.component';
import {authGuard} from './components/auth/auth-guard';
import {HomeComponent} from './components/home/home.component';
import {ProjectComponent} from './components/projects/project/project.component';
import {ProjectListComponent} from './components/projects/project-list/project-list.component';

export const routes: Routes = [
    {path: '', redirectTo: '/home', pathMatch: 'full'},
    {path: 'login', component: LoginComponent},
    {path: 'register', component: RegisterComponent},
    {path: 'home', component: HomeComponent},
    {path: 'dashboard', component: DashboardComponent, canActivate: [authGuard]},
    {path: 'projects', component: ProjectListComponent, canActivate: [authGuard]},
    {path: 'projects/:projectId', component: ProjectComponent, canActivate: [authGuard]},
    {path: '**', redirectTo: '/home'}
];
