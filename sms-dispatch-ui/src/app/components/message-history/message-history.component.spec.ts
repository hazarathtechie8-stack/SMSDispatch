import { ComponentFixture, fakeAsync, TestBed, tick } from '@angular/core/testing';
import { throwError, of } from 'rxjs';
import { MessageService } from '../../services/message.service';
import { MessageRecord, PagedResult } from '../../models/message.model';
import { MessageHistoryComponent } from './message-history.component';

describe('MessageHistoryComponent', () => {
  let fixture: ComponentFixture<MessageHistoryComponent>;
  let component: MessageHistoryComponent;
  let messageService: jasmine.SpyObj<MessageService>;

  const emptyResult: PagedResult<MessageRecord> = {
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 10
  };
  const record: MessageRecord = {
    id: 42,
    toPhoneNumber: '+15551234567',
    fromPhoneNumber: '+15557654321',
    recipientName: 'Avery',
    message: 'a'.repeat(70),
    status: 'Sent',
    createdDateTime: '2026-10-10T12:00:00Z',
    modifiedDatetime: '2026-10-10T12:00:03Z'
  };

  beforeEach(async () => {
    messageService = jasmine.createSpyObj<MessageService>('MessageService', ['search']);
    messageService.search.and.returnValue(of(emptyResult));

    await TestBed.configureTestingModule({
      imports: [MessageHistoryComponent],
      providers: [{ provide: MessageService, useValue: messageService }]
    }).compileComponents();

    fixture = TestBed.createComponent(MessageHistoryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the initial page and shows the empty state', () => {
    expect(messageService.search).toHaveBeenCalledWith({
      search: undefined,
      criteria: 'contains',
      sortBy: 'createdDateTime',
      sortDir: 'desc',
      page: 1,
      pageSize: 10
    });
    expect(component.loading()).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('No messages yet. Send one to get started.');
  });

  it('renders message rows, status badges, and pagination', () => {
    messageService.search.and.returnValue(of({
      items: [record],
      totalCount: 25,
      page: 1,
      pageSize: 10
    }));

    component.load();
    fixture.detectChanges();

    const row = fixture.nativeElement.querySelector('tbody tr');
    expect(row.textContent).toContain('Avery');
    expect(row.textContent).toContain('+15551234567');
    expect(row.querySelector('.truncate').textContent).toBe(`${'a'.repeat(60)}…`);
    expect(row.querySelector('.badge-sent')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.record-count').textContent).toContain('25 records');
    expect(fixture.nativeElement.querySelector('.pagination').textContent).toContain('Page 1 of 3');
  });

  it('debounces searches and ignores search text shorter than three characters', fakeAsync(() => {
    component.search = 'ab';
    component.onSearchChange();
    tick(350);
    expect(messageService.search.calls.count()).toBe(1);

    component.search = 'Avery';
    component.onSearchChange();
    tick(349);
    expect(messageService.search.calls.count()).toBe(1);
    tick(1);

    expect(messageService.search.calls.count()).toBe(2);
    expect(messageService.search.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({
      search: 'Avery',
      page: 1
    }));
  }));

  it('updates the sort field and toggles sort direction', () => {
    component.sort('createdDateTime');

    expect(component.sortDir).toBe('asc');
    expect(component.isActive('createdDateTime')).toBeTrue();
    expect(component.sortIcon('createdDateTime')).toBe('↑');

    component.sort('status');

    expect(component.sortBy).toBe('status');
    expect(component.sortDir).toBe('desc');
    expect(component.sortIcon('status')).toBe('↓');
    expect(component.sortIcon('recipient')).toBe('↕');
    expect(messageService.search.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({
      sortBy: 'status',
      sortDir: 'desc',
      page: 1
    }));
  });

  it('changes pages only when the page number is valid', () => {
    component.total.set(25);
    component.goPage(3);

    expect(component.page).toBe(3);
    expect(messageService.search.calls.mostRecent().args[0].page).toBe(3);

    const callCount = messageService.search.calls.count();
    component.goPage(0);
    component.goPage(4);
    expect(messageService.search.calls.count()).toBe(callCount);
  });

  it('resets pagination when the search criteria changes', () => {
    component.page = 2;
    component.criteria = 'equals';

    component.onCriteriaChange();

    expect(component.page).toBe(1);
    expect(messageService.search.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({
      criteria: 'equals',
      page: 1
    }));
  });

  it('shows an error when loading messages fails', () => {
    messageService.search.and.returnValue(throwError(() => new Error('Request failed')));

    component.load();
    fixture.detectChanges();

    expect(component.error()).toBe('Failed to load messages.');
    expect(component.loading()).toBeFalse();
    expect(fixture.nativeElement.querySelector('.alert-error').textContent)
      .toContain('Failed to load messages.');
  });

  it('returns nearby page numbers and formats message helpers', () => {
    component.total.set(1000);
    component.page = 50;

    expect(component.pageNumbers).toEqual([48, 49, 50, 51, 52]);
    expect(component.badgeClass('Dead Lettered')).toBe('badge badge-deadlettered');
    expect(component.truncate(record.message)).toBe(`${'a'.repeat(60)}…`);
  });
});