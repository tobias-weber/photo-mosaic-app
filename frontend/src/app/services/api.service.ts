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
        return this.http.post<AuthResponse>(`${this.BASE_URL}/register`, { userName, password });
    }

    login(userName: string, password: string) {
        return this.http.post<AuthResponse>(`${this.BASE_URL}/login`, {userName, password})
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
        return this.http.post<Project>(`${this.BASE_URL}/users/${userName}/projects/`, {title})
    }

    updateProject(userName: string, project: Project) {
        return this.http.put<Project>(`${this.BASE_URL}/users/${userName}/projects/${project.projectId}`, project)
    }

    deleteProject(userName: string, projectId: string) {
        return this.http.delete<void>(`${this.BASE_URL}/users/${userName}/projects/${projectId}`);
    }

    uploadImage(userName: string, projectId: string, file: File, isTarget: boolean) {
        const formData = new FormData();
        formData.append('file', file, file.name);
        formData.append('isTarget', String(isTarget));
        return this.http.post<string>(`${this.BASE_URL}/users/${userName}/projects/${projectId}/images`, formData)
    }

    getImageRefs(userName: string, projectId: string, filter: 'TILES' | 'TARGETS' | 'ALL') {
        return this.http.get<ImageRef[]>(`${this.BASE_URL}/users/${userName}/projects/${projectId}/images`,
            {params: {filter}});
    }

    getImage(userName: string, projectId: string, imageId: string) {
        return this.http.get(
            `${this.BASE_URL}/users/${userName}/projects/${projectId}/images/${imageId}`,
            { responseType: 'blob' } // important for binary data
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
    createJob(userName: string, projectId: string, targetId: string) {
        const body = {
            algorithm: 'LAP',
            subdivisions: 1,
            n: 100,
            target: targetId
        }
        return this.http.post(`${this.BASE_URL}/users/${userName}/projects/${projectId}/jobs`, body)
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
