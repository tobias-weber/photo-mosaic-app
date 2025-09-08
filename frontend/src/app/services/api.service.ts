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

}
