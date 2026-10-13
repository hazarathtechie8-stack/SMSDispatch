import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { MessageService } from './message.service';
import { MessageRecord, SendRequest, SendResponse } from '../models/message.model';
import { environment } from '../../environments/environment';

describe('MessageService', () => {
  let service: MessageService;
  let httpMock: HttpTestingController;
  const messagesUrl = `${environment.apiBaseUrl}/messages`;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(MessageService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('sends a message and returns the API response', () => {
    const request: SendRequest = {
      toPhoneNumber: '+15551234567',
      fromPhoneNumber: '+15557654321',
      message: 'Hello',
      recipientName: 'Avery'
    };
    const response: SendResponse = { id: 12, status: 'Pending' };

    service.send(request).subscribe(result => expect(result).toEqual(response));

    const httpRequest = httpMock.expectOne(messagesUrl);
    expect(httpRequest.request.method).toBe('POST');
    expect(httpRequest.request.body).toEqual(request);
    httpRequest.flush(response);
  });

  it('gets a message by ID', () => {
    const response: MessageRecord = {
      id: 12,
      toPhoneNumber: '+15551234567',
      fromPhoneNumber: '+15557654321',
      message: 'Hello',
      status: 'Sent',
      createdDateTime: '2026-10-10T12:00:00Z',
      modifiedDatetime: '2026-10-10T12:00:03Z'
    };

    service.getById(12).subscribe(result => expect(result).toEqual(response));

    const httpRequest = httpMock.expectOne(`${messagesUrl}/12`);
    expect(httpRequest.request.method).toBe('GET');
    httpRequest.flush(response);
  });

  it('polls until it emits the final message status', fakeAsync(() => {
    const pending: MessageRecord = {
      id: 12,
      toPhoneNumber: '+15551234567',
      fromPhoneNumber: '+15557654321',
      message: 'Hello',
      status: 'Pending',
      createdDateTime: '2026-10-10T12:00:00Z',
      modifiedDatetime: '2026-10-10T12:00:00Z'
    };
    const sent = { ...pending, status: 'Sent' as const };
    const results: MessageRecord[] = [];
    const subscription = service.pollStatus(12).subscribe(result => results.push(result));

    tick(3000);
    httpMock.expectOne(`${messagesUrl}/12`).flush(pending);
    tick(3000);
    httpMock.expectOne(`${messagesUrl}/12`).flush(sent);

    expect(results).toEqual([pending, sent]);
    expect(subscription.closed).toBeTrue();
  }));

  it('searches using the supplied filters, sorting, and pagination', () => {
    const params = {
      search: 'Avery Smith',
      criteria: 'contains' as const,
      sortBy: 'createdDateTime',
      sortDir: 'desc' as const,
      page: 2,
      pageSize: 10
    };
    const response = { items: [], totalCount: 0, page: 2, pageSize: 10 };

    service.search(params).subscribe(result => expect(result).toEqual(response));

    const httpRequest = httpMock.expectOne(request => request.url === messagesUrl);
    expect(httpRequest.request.method).toBe('GET');
    expect(httpRequest.request.params.keys()).toEqual([
      'search', 'criteria', 'sortBy', 'sortDir', 'page', 'pageSize'
    ]);
    expect(httpRequest.request.params.get('search')).toBe('Avery Smith');
    expect(httpRequest.request.params.get('criteria')).toBe('contains');
    expect(httpRequest.request.params.get('sortBy')).toBe('createdDateTime');
    expect(httpRequest.request.params.get('sortDir')).toBe('desc');
    expect(httpRequest.request.params.get('page')).toBe('2');
    expect(httpRequest.request.params.get('pageSize')).toBe('10');
    httpRequest.flush(response);
  });
});