import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ControlExecution,
  ExecutionResult,
  ResultsPage,
} from '../../shared/models';

@Injectable({ providedIn: 'root' })
export class ExecutionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api`;

  /**
   * Tracks in-flight executions: controlId → executionId.
   * Set when a run starts; cleared when the execution completes or is cancelled.
   */
  readonly activeExecutions = signal<Map<number, number>>(new Map());

  /**
   * Start a control execution.
   * Registers the new execution in `activeExecutions` on success.
   */
  run(controlId: number): Observable<ExecutionResult> {
    return this.http
      .post<ExecutionResult>(`${this.baseUrl}/executions`, { controlId })
      .pipe(
        tap(result => {
          this.activeExecutions.update(map => {
            const next = new Map(map);
            next.set(result.controlId, result.executionId);
            return next;
          });
        }),
      );
  }

  /**
   * Cancel a running execution by its executionId.
   * Removes the matching entry from `activeExecutions` on success.
   */
  cancel(executionId: number): Observable<void> {
    return this.http
      .delete<void>(`${this.baseUrl}/executions/${executionId}`)
      .pipe(
        tap(() => {
          this.activeExecutions.update(map => {
            const next = new Map(map);
            for (const [controlId, execId] of next.entries()) {
              if (execId === executionId) {
                next.delete(controlId);
                break;
              }
            }
            return next;
          });
        }),
      );
  }

  /**
   * Retrieve a page of anomaly results for a completed execution.
   * Page is 1-based; page size is fixed at 100 server-side.
   */
  getResults(executionId: number, page = 1): Observable<ResultsPage> {
    const params = new HttpParams().set('page', page.toString());
    return this.http.get<ResultsPage>(
      `${this.baseUrl}/executions/${executionId}/results`,
      { params },
    );
  }

  /**
   * Retrieve the full execution history for a given control.
   */
  getHistory(controlId: number): Observable<ControlExecution[]> {
    return this.http.get<ControlExecution[]>(
      `${this.baseUrl}/controls/${controlId}/executions`,
    );
  }
}
