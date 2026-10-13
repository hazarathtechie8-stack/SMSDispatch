import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { MessageService } from '../../services/message.service';
import { MessageRecord, SendResponse } from '../../models/message.model';
import { SendMessageComponent } from './send-message.component';

describe('SendMessageComponent', () => {
  let fixture: ComponentFixture<SendMessageComponent>;
  let component: SendMessageComponent;
  let messageService: jasmine.SpyObj<MessageService>;
  let statusUpdates: Subject<MessageRecord>;

  const sendResponse: SendResponse = { id: 42, status: 'Pending' };
  const statusRecord: MessageRecord = {
    id: 42,
    toPhoneNumber: '+15551234567',
    fromPhoneNumber: '+15557654321',
    message: 'Hello',
    status: 'Pending',
    createdDateTime: '2026-10-10T12:00:00Z',
    modifiedDatetime: '2026-10-10T12:00:00Z'
  };

  beforeEach(async () => {
    statusUpdates = new Subject<MessageRecord>();
    messageService = jasmine.createSpyObj<MessageService>('MessageService', ['send', 'pollStatus']);
    messageService.send.and.returnValue(of(sendResponse));
    messageService.pollStatus.and.returnValue(statusUpdates.asObservable());

    await TestBed.configureTestingModule({
      imports: [SendMessageComponent],
      providers: [{ provide: MessageService, useValue: messageService }]
    }).compileComponents();

    fixture = TestBed.createComponent(SendMessageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  function enterValidMessage() {
    component.form.setValue({
      toPhoneNumber: '+15551234567',
      fromPhoneNumber: '+15557654321',
      recipientName: 'Avery',
      message: 'Hello'
    });
  }

  it('renders the form and updates the character count', () => {
    expect(fixture.nativeElement.textContent).toContain('Send SMS Message');
    expect(fixture.nativeElement.querySelector('.char-count').textContent).toContain('0 / 1000');

    component.form.controls.message.setValue('Hello');
    fixture.detectChanges();

    expect(component.msgLen).toBe(5);
    expect(fixture.nativeElement.querySelector('.char-count').textContent).toContain('5 / 1000');
  });

  it('marks invalid fields and does not submit', () => {
    component.submit();

    expect(component.form.invalid).toBeTrue();
    expect(component.form.controls.toPhoneNumber.touched).toBeTrue();
    expect(component.form.controls.message.touched).toBeTrue();
    expect(messageService.send).not.toHaveBeenCalled();
  });

  it('submits a valid message and displays delivery updates', () => {
    enterValidMessage();

    component.submit();

    expect(messageService.send).toHaveBeenCalledWith({
      toPhoneNumber: '+15551234567',
      fromPhoneNumber: '+15557654321',
      recipientName: 'Avery',
      message: 'Hello'
    });
    expect(messageService.pollStatus).toHaveBeenCalledWith(sendResponse.id);
    expect(component.sendResponse()).toEqual(sendResponse);
    expect(component.polling()).toBeTrue();

    statusUpdates.next({ ...statusRecord, status: 'Sent' });
    fixture.detectChanges();

    expect(component.statusInfo()?.status).toBe('Sent');
    expect(component.isTerminal()).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('Send Another Message');

    statusUpdates.complete();
    expect(component.polling()).toBeFalse();
  });

  it('shows an API error when submission fails', () => {
    messageService.send.and.returnValue(
      throwError(() => ({ error: { title: 'Unable to send message.' } }))
    );
    enterValidMessage();

    component.submit();
    fixture.detectChanges();

    expect(component.submitting()).toBeFalse();
    expect(component.errorMsg()).toBe('Unable to send message.');
    expect(fixture.nativeElement.querySelector('.alert-error').textContent)
      .toContain('Unable to send message.');
    expect(messageService.pollStatus).not.toHaveBeenCalled();
  });

  it('resets the form and clears state, stopping active polling', () => {
    enterValidMessage();
    component.submit();
    statusUpdates.next(statusRecord);
    component.errorMsg.set('A previous error');

    component.reset();

    expect(component.form.pristine).toBeTrue();
    expect(component.msgLen).toBe(0);
    expect(component.sendResponse()).toBeNull();
    expect(component.statusInfo()).toBeNull();
    expect(component.errorMsg()).toBeNull();
    expect(component.polling()).toBeFalse();
    expect(statusUpdates.observed).toBeFalse();
  });

  it('unsubscribes from status polling when destroyed', () => {
    enterValidMessage();
    component.submit();
    expect(statusUpdates.observed).toBeTrue();

    fixture.destroy();

    expect(statusUpdates.observed).toBeFalse();
  });

  it('builds a status badge class with a pending fallback', () => {
    expect(component.badgeClass('Dead Lettered')).toBe('badge badge-deadlettered');
    expect(component.badgeClass()).toBe('badge badge-pending');
  });
});