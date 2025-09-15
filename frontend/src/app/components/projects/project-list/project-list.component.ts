import {Component, inject, OnInit, signal} from '@angular/core';
import {AuthService} from '../../../services/auth.service';
import {ApiService, Project} from '../../../services/api.service';
import {DatePipe} from '@angular/common';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {ToastService} from '../../../services/toast.service';
import {ModalService} from '../../../services/modal.service';
import {Router, RouterLink} from '@angular/router';

@Component({
    selector: 'app-project-list',
    imports: [
        DatePipe,
        ReactiveFormsModule,
        RouterLink
    ],
    templateUrl: './project-list.component.html',
    styleUrl: './project-list.component.css'
})
export class ProjectListComponent implements OnInit {
    protected readonly maxTitleLength = 128;

    private auth = inject(AuthService);
    private api = inject(ApiService);
    private fb = inject(FormBuilder);
    private toast = inject(ToastService);
    private modals = inject(ModalService);
    private router = inject(Router);

    userName = this.auth.userName;
    projects = signal<Project[]>([]);


    newProjectForm = this.fb.group({
        title: ['', [Validators.required, Validators.maxLength(this.maxTitleLength), Validators.pattern('[ a-zA-Z0-9_\-]*')]],
    });


    get projectTitle() {
        return this.newProjectForm.get('title');
    }

    ngOnInit(): void {
        this.loadProjects();
    }

    loadProjects() {
        const userName = this.userName();
        if (userName) {
            this.api.getProjects(userName).subscribe({
                next: (res) => this.projects.set(res),
                error: (err) => console.error('Failed to load projects', err)
            });
        }
    }

    createProject() {
        if (this.newProjectForm.invalid) {
            return;
        }
        this.api.createProject(this.auth.userName()!, this.newProjectForm.value.title!).subscribe({
            next: project => {
                this.toast.success(`Created new project "${project.title}"`);
                this.router.navigate([`/projects/${project.projectId}`]);
            },
            error: () => this.toast.error('Unable to create new project')
        })

    }

    async deleteProject(project: Project) {
        const msg = `Are you sure you want to delete the project "${project.title}" and all images and mosaics it contains?`;
        if (await this.modals.openConfirmModal(msg, 'Delete Project', 'Delete')) {
            this.api.deleteProject(this.userName()!, project.projectId!).subscribe({
                next: () => {
                    this.toast.success(`Deleted "${project.title}"`);
                    this.loadProjects();
                },
                error: () => this.toast.error('Unable to delete project')
            })
        }
    }
}
