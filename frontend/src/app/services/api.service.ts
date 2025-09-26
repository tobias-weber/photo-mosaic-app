import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class ApiService {

    private http = inject(HttpClient);
    private readonly BASE_URL = '/backend-api';

    /**
     * Performs a generic GET request to a specified endpoint.
     * This method uses a relative URL, which works seamlessly with a
     * proxy configuration during development and with a reverse proxy
     * in production.
     *
     * @param endpoint The API endpoint to call (e.g., '/weatherforecast').
     * @returns An Observable of the HTTP response.
     */
    get(endpoint: string): Observable<any> {
        return this.http.get<any>(`${this.BASE_URL}/${endpoint}`);
    }

    register(userName: string, password: string) {
        return this.http.post<AuthResponse>(`${this.BASE_URL}/register`, {userName, password});
    }

    login(userName: string, password: string) {
        return this.http.post<AuthResponse>(`${this.BASE_URL}/login`, {userName, password});
    }

    getUser(userName: string) {
        return this.http.get<User>(`${this.BASE_URL}/users/${userName}`);
    }

    deleteUser(userName: string) {
        return this.http.delete<void>(`${this.BASE_URL}/users/${userName}`);
    }

    getProjects(userName: string) {
        return this.http.get<Project[]>(`${this.BASE_URL}/users/${userName}/projects`);
    }

    getProject(userName: string, projectId: string) {
        return this.http.get<Project>(`${this.BASE_URL}/users/${userName}/projects/${projectId}`);
    }

    createProject(userName: string, title: string) {
        return this.http.post<Project>(`${this.BASE_URL}/users/${userName}/projects/`, {title});
    }

    updateProject(userName: string, project: Project) {
        return this.http.put<Project>(`${this.BASE_URL}/users/${userName}/projects/${project.projectId}`, project);
    }

    deleteProject(userName: string, projectId: string) {
        return this.http.delete<void>(`${this.BASE_URL}/users/${userName}/projects/${projectId}`);
    }

    uploadImage(userName: string, projectId: string, file: File, isTarget: boolean) {
        const formData = new FormData();
        formData.append('file', file, file.name);
        formData.append('isTarget', String(isTarget));
        return this.http.post<string>(`${this.BASE_URL}/users/${userName}/projects/${projectId}/images`, formData);
    }

    getImageRefs(userName: string, projectId: string, filter: 'TILES' | 'TARGETS' | 'ALL') {
        return this.http.get<ImageRef[]>(`${this.BASE_URL}/users/${userName}/projects/${projectId}/images`,
            {params: {filter}});
    }

    getImage(userName: string, projectId: string, imageId: string) {
        return this.http.get(
            `${this.BASE_URL}/users/${userName}/projects/${projectId}/images/${imageId}`,
            {responseType: 'blob'} // important for binary data
        );
    }

    deleteImage(userName: string, projectId: string, imageId: string) {
        return this.http.delete<void>(`${this.BASE_URL}/users/${userName}/projects/${projectId}/images/${imageId}`);
    }

    deleteImages(userName: string, projectId: string, filter: 'TILES' | 'TARGETS' | 'ALL') {
        return this.http.delete<ImageRef[]>(`${this.BASE_URL}/users/${userName}/projects/${projectId}/images`,
            {params: {filter}});
    }


    // Jobs
    createJob(userName: string, projectId: string, targetId: string, n: number, algorithm: 'LAP',
              subdivisions: number, repetitions: number, cropCount: number) {
        const body = {
            algorithm: algorithm,
            subdivisions: subdivisions,
            n: n,
            target: targetId,
            repetitions: repetitions,
            cropCount: cropCount,
        }
        return this.http.post<Job>(`${this.BASE_URL}/users/${userName}/projects/${projectId}/jobs`, body);
    }

    getJobs(userName: string, projectId: string) {
        return this.http.get<Job[]>(`${this.BASE_URL}/users/${userName}/projects/${projectId}/jobs`);
    }


    getJob(userName: string, projectId: string, jobId: string) {
        return this.http.get<Job>(`${this.BASE_URL}/users/${userName}/projects/${projectId}/jobs/${jobId}`);
    }

    getMosaic(userName: string, projectId: string, jobId: string) {
        return this.http.get(
            `${this.BASE_URL}/users/${userName}/projects/${projectId}/jobs/${jobId}/mosaic`,
            {responseType: 'blob'} // important for binary data
        );
    }

    deleteJob(userName: string, projectId: string, jobId: string) {
        return this.http.delete<void>(`${this.BASE_URL}/users/${userName}/projects/${projectId}/jobs/${jobId}`);
    }

    constructDzUrl(userName: string, projectId: string, jobId: string) {
        return `${this.BASE_URL}/users/${userName}/projects/${projectId}/jobs/${jobId}/dz/dz.jpg.dzi`;
    }

    // Collections
    getCollections() {
        return this.http.get<TileCollection[]>(`${this.BASE_URL}/collections`);
    }

    installCollection(collectionId: string) {
        return this.http.post<void>(`${this.BASE_URL}/collections/${collectionId}/install`, null);
    }

    uninstallCollection(collectionId: string) {
        return this.http.post<void>(`${this.BASE_URL}/collections/${collectionId}/uninstall`, null);
    }

    getSelectedCollections(userName: string, projectId: string) {
        return this.http.get<string[]>(
            `${this.BASE_URL}/users/${userName}/projects/${projectId}/collections/`);
    }

    selectCollection(userName: string, projectId: string, collectionId: string) {
        return this.http.post<string[]>(
            `${this.BASE_URL}/users/${userName}/projects/${projectId}/collections/${collectionId}`, null);
    }

    deselectCollection(userName: string, projectId: string, collectionId: string) {
        return this.http.delete<string[]>(
            `${this.BASE_URL}/users/${userName}/projects/${projectId}/collections/${collectionId}`);
    }

    // Admin Endpoints
    getUsers() {
        return this.http.get<User[]>(`${this.BASE_URL}/users`);
    }
}

export interface AuthResponse {
    token: string;
    expiration: string;
}

export interface User {
    userId: string;
    userName: string;
    createdAt: Date;
}

export interface Project {
    projectId: string;
    title: string;
    createdAt: Date;
}

export interface ImageRef {
    imageId: string;
    name: string;
    isTarget: boolean;
}

export interface Job {
    jobId: string;
    startedAt: Date;
    finishedAt: Date;
    status: JobStatus;
    progress: number; // [0, 1] and only relevant if status === Processing
    n: number;
    target: string;
    algorithm: string;
    subdivisions?: number;
    cropCount?: number;
    repetitions?: number;
}

export interface TileCollection {
    id: string;
    name: string;
    imageCount: number;
    size: string;
    description: string;
    status: CollectionStatus;
    installDate: Date;
}

export enum JobStatus {
    'Created' = 0,
    'Submitted' = 1,
    'Processing' = 2,
    'GeneratedPreview' = 3,
    'Finished' = 4,
    'Aborted' = 5,
    'Failed' = 6,
}

export enum CollectionStatus {
    'NotInstalled' = 0,
    'Downloading' = 1,
    'Ready' = 2
}
