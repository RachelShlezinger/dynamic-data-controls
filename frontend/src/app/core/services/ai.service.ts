import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AiParseRequest, AiParseResponse } from '../../shared/models';

@Injectable({ providedIn: 'root' })
export class AiService {
  private readonly http = inject(HttpClient);
  private readonly parseUrl = `${environment.apiUrl}/api/ai/parse`;

  /**
   * Send a free-text Hebrew description to the AI proxy endpoint.
   * Returns a suggested control name and a fully-structured ControlDefinition.
   * Description must be 10–1,000 characters (validated server-side; also enforced
   * in the UI before this method is called).
   */
  parseDescription(description: string): Observable<AiParseResponse> {
    const body: AiParseRequest = { description };
    return this.http.post<AiParseResponse>(this.parseUrl, body);
  }
}
