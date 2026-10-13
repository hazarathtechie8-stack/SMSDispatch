import { Component, OnDestroy, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { MessageService } from '../../services/message.service';
import { MessageRecord, SendResponse } from '../../models/message.model';

@Component({
  selector: 'app-send-message',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './send-message.component.html',
  styleUrl: './send-message.component.scss'
})
export class SendMessageComponent implements OnDestroy {
  private fb      = inject(FormBuilder);
  private svc     = inject(MessageService);
  private pollSub?: Subscription;

  submitting   = signal(false);
  sendResponse = signal<SendResponse | null>(null);
  statusInfo   = signal<MessageRecord | null>(null);
  errorMsg     = signal<string | null>(null);
  polling      = signal(false);

  /** Checks whether delivery has reached a final status. */
  isTerminal = computed(() => {
    const s = this.statusInfo()?.status;
    return s === 'Sent' || s === 'Failed' || s === 'DeadLettered';
  });

  form = this.fb.group({
    toPhoneNumber:   ['', [Validators.required, Validators.pattern(/^\+?[1-9]\d{9,14}$/)]],
    fromPhoneNumber: ['', [Validators.required, Validators.pattern(/^\+?[1-9]\d{9,14}$/)]],
    recipientName:   [''],
    message:         ['', [Validators.required, Validators.maxLength(1000)]]
  });

  /** Returns the current message length for the character counter. */
  get msgLen() { return this.form.value.message?.length ?? 0; }

  /** Validates and submits the message, then starts status tracking. */
  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.submitting.set(true);
    this.errorMsg.set(null);
    this.sendResponse.set(null);
    this.statusInfo.set(null);
    this.pollSub?.unsubscribe();

    const val = this.form.value;
    this.svc.send({
      toPhoneNumber:   val.toPhoneNumber!,
      fromPhoneNumber: val.fromPhoneNumber!,
      message:         val.message!,
      recipientName:   val.recipientName ?? undefined
    }).subscribe({
      next: (resp) => {
        this.submitting.set(false);
        this.sendResponse.set(resp);
        this.startPolling(resp.id);
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMsg.set(err?.error?.title ?? err?.message ?? 'Failed to submit message.');
      }
    });
  }

  /** Polls the service until the message status stops changing. */
  private startPolling(id: number) {
    this.polling.set(true);
    this.pollSub = this.svc.pollStatus(id).subscribe({
      next:     (m) => this.statusInfo.set(m),
      error:    ()  => this.polling.set(false),
      complete: ()  => this.polling.set(false)
    });
  }

  /** Clears the form and submission state for another message. */
  reset() {
    this.pollSub?.unsubscribe();
    this.form.reset();
    this.sendResponse.set(null);
    this.statusInfo.set(null);
    this.errorMsg.set(null);
    this.polling.set(false);
  }

  /** Returns the CSS classes for the current delivery status. */
  badgeClass(status?: string) {
    return 'badge badge-' + (status ?? 'pending').toLowerCase().replace(/\s/g, '');
  }

  /** Stops the status subscription when the component is removed. */
  ngOnDestroy() { this.pollSub?.unsubscribe(); }
}
