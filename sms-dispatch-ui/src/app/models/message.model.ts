export type MessageStatus = 'Pending' | 'Queued' | 'Sending' | 'Sent' | 'Failed' | 'DeadLettered';

export interface SendRequest {
  toPhoneNumber: string;
  fromPhoneNumber: string;
  message: string;
  recipientName?: string;
}

export interface SendResponse {
  id: number;
  status: MessageStatus;
}

export interface MessageRecord {
  id: number;
  toPhoneNumber: string;
  fromPhoneNumber: string;
  message: string;
  recipientName?: string;
  status: MessageStatus;
  errorMessage?: string;
  createdDateTime: string;
  modifiedDatetime: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
