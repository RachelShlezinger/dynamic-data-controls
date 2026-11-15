import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  BusinessEntity,
  BusinessField,
  EntityRelationship,
} from '../../shared/models';

@Injectable({ providedIn: 'root' })
export class MetadataService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/metadata`;

  /** Local cache of business entities; updated by getEntities(). */
  readonly entities = signal<BusinessEntity[]>([]);

  /**
   * Fetch all business entities (without fields).
   * Updates the `entities` signal on success.
   */
  getEntities(): Observable<BusinessEntity[]> {
    return this.http.get<BusinessEntity[]>(`${this.baseUrl}/entities`).pipe(
      tap(list => this.entities.set(list)),
    );
  }

  /**
   * Fetch the business fields that belong to a specific entity.
   */
  getEntityFields(entityId: number): Observable<BusinessField[]> {
    return this.http.get<BusinessField[]>(
      `${this.baseUrl}/entities/${entityId}/fields`,
    );
  }

  /**
   * Fetch all entity-to-entity JOIN relationships defined in the metadata.
   */
  getRelationships(): Observable<EntityRelationship[]> {
    return this.http.get<EntityRelationship[]>(`${this.baseUrl}/relationships`);
  }
}
