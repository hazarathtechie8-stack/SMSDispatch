import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="app-header">
      <a class="brand" routerLink="/">📱 SMS Dispatch</a>
      <nav>
        <a routerLink="/send"    routerLinkActive="active">Send Message</a>
        <a routerLink="/history" routerLinkActive="active">Message History</a>
      </nav>
    </header>
    <router-outlet />
  `
})
export class AppComponent {}
