import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'send', pathMatch: 'full' },
  { path: 'send',    loadComponent: () => import('./components/send-message/send-message.component').then(m => m.SendMessageComponent) },
  { path: 'history', loadComponent: () => import('./components/message-history/message-history.component').then(m => m.MessageHistoryComponent) },
  { path: '**', redirectTo: 'send' }
];
