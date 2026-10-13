import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, interval } from 'rxjs';
import { switchMap, distinctUntilChanged, takeWhile, share } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { SendRequest, SendResponse, MessageRecord, PagedResult } from '../models/message.model';

@Injectable({ providedIn: 'root' })
export class MessageService {
  private http = inject(HttpClient);
  private base = environment.apiBaseUrl;

  /** Submits a new message to the API. */
  send(req: SendRequest): Observable<SendResponse> {
    return this.http.post<SendResponse>(`${this.base}/messages`, req);
  }

  /** Fetches a message and its latest status by ID. */
  getById(id: number): Observable<MessageRecord> {
    return this.http.get<MessageRecord>(`${this.base}/messages/${id}`);
  }

  /** Checks status every three seconds and emits the final update. */
  pollStatus(id: number): Observable<MessageRecord> {
    return interval(3000).pipe(
      switchMap(() => this.getById(id)),
      distinctUntilChanged((a, b) => a.status === b.status),
      takeWhile(m => m.status === 'Pending' || m.status === 'Queued' || m.status === 'Sending', true),
      share()
    );
  }

  /** Searches messages using the supplied filters, sorting, and page. */
  search(params: {
    search?: string;
    criteria?: 'contains' | 'equals';
    sortBy?: string;
    sortDir?: 'asc' | 'desc';
    page?: number;
    pageSize?: number;
  }): Observable<PagedResult<MessageRecord>> {
    let p = new HttpParams();
    if (params.search)   p = p.set('search',   params.search);
    if (params.criteria) p = p.set('criteria', params.criteria);
    if (params.sortBy)   p = p.set('sortBy',   params.sortBy);
    if (params.sortDir)  p = p.set('sortDir',  params.sortDir);
    if (params.page)     p = p.set('page',     params.page.toString());
    if (params.pageSize) p = p.set('pageSize', params.pageSize.toString());
    return this.http.get<PagedResult<MessageRecord>>(`${this.base}/messages`, { params: p });
  }
}
