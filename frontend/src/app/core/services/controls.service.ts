import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  Control,
  CreateControlRequest,
  UpdateControlRequest,
} from '../../shared/models';

@Injectable({ providedIn: 'root' })
export class ControlsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/controls`;

  /** Local cache of the controls list; updated by getAll(), create(), update(), delete(). */
  readonly controls = signal<Control[]>([]);

  /**
   * Fetch all controls, optionally filtered by name/description.
   * Updates the `controls` signal on success.
   */
  getAll(search?: string): Observable<Control[]> {
    let params = new HttpParams();
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<Control[]>(this.baseUrl, { params }).pipe(
      tap(list => this.controls.set(list)),
    );
  }

  /**
   * Fetch a single control with its full definition.
   */
  getById(id: number): Observable<Control> {
    return this.http.get<Control>(`${this.baseUrl}/${id}`);
  }

  /**
   * Create a new control.
   * Appends the created control to the `controls` signal on success.
   */
  create(payload: CreateControlRequest): Observable<Control> {
    return this.http.post<Control>(this.baseUrl, payload).pipe(
      tap(created => this.controls.update(cs => [...cs, created])),
    );
  }

  /**
   * Update an existing control.
   * Replaces the matching entry in the `controls` signal on success.
   */
  update(id: number, payload: UpdateControlRequest): Observable<Control> {
    return this.http.put<Control>(`${this.baseUrl}/${id}`, payload).pipe(
      tap(updated =>
        this.controls.update(cs => cs.map(c => (c.id === id ? updated : c))),
      ),
    );
  }

  /**
   * Delete a control by id.
   * Removes the matching entry from the `controls` signal on success.
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`).pipe(
      tap(() => this.controls.update(cs => cs.filter(c => c.id !== id))),
    );
  }
}
