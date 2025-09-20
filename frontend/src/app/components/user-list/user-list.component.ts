import {Component, inject, OnInit, signal} from '@angular/core';
import {ApiService, User} from '../../services/api.service';
import {DatePipe} from '@angular/common';
import {RouterLink} from '@angular/router';

@Component({
    selector: 'app-user-list',
    imports: [
        DatePipe,
        RouterLink
    ],
    templateUrl: './user-list.component.html',
    styleUrl: './user-list.component.css'
})
export class UserListComponent implements OnInit {
    private api = inject(ApiService);

    users = signal<User[]>([]);


    ngOnInit(): void {
        this.loadUsers();
    }

    loadUsers() {
        this.api.getUsers().subscribe({
            next: (res) => this.users.set(res),
            error: (err) => console.error('Failed to load users', err)
        });
    }

    deleteUser(user: User) {
        //  TODO: implement delete user
    }
}
