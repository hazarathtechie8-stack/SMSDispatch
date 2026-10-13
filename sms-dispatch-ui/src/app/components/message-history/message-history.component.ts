import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { MessageService } from '../../services/message.service';
import { MessageRecord } from '../../models/message.model';

type SortField = 'createdDateTime' | 'modifiedDatetime' | 'status' | 'recipient';

@Component({
  selector: 'app-message-history',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './message-history.component.html',
  styleUrl: './message-history.component.scss'
})
export class MessageHistoryComponent implements OnInit {
  private svc = inject(MessageService);

  items      = signal<MessageRecord[]>([]);
  total      = signal(0);
  loading    = signal(false);
  error      = signal<string | null>(null);

  search     = '';
  criteria: 'contains' | 'equals' = 'contains';
  sortBy: SortField  = 'createdDateTime';
  sortDir: 'asc' | 'desc' = 'desc';
  page       = 1;
  pageSize   = 10;

  /** Returns the page count for the current result set. */
  get totalPages() { return Math.ceil(this.total() / this.pageSize); }

  /** Returns page numbers around the current page. */
  get pageNumbers() {
    const pages = [];
    for (let i = 1; i <= this.totalPages; i++) pages.push(i);
    return pages.slice(Math.max(0, this.page - 3), this.page + 2);
  }

  private searchSubject = new Subject<string>();

  /** Loads the initial results and sets up debounced search. */
  ngOnInit() {
    this.load();
    this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged()
    ).subscribe(() => { this.page = 1; this.load(); });
  }

  /** Queues the current search text for debounced loading. */
  onSearchChange() { this.searchSubject.next(this.search); }

  /** Fetches the current page of messages unless the search is too short. */
  load() {
    if (this.search && this.search.length > 0 && this.search.length < 3) return;
    this.loading.set(true);
    this.error.set(null);

    this.svc.search({
      search:   this.search   || undefined,
      criteria: this.criteria,
      sortBy:   this.sortBy,
      sortDir:  this.sortDir,
      page:     this.page,
      pageSize: this.pageSize
    }).subscribe({
      next: (res) => {
        this.items.set(res.items);
        this.total.set(res.totalCount);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load messages.');
        this.loading.set(false);
      }
    });
  }

  /** Changes the sort field or toggles its direction, then reloads results. */
  sort(field: SortField) {
    if (this.sortBy === field) this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    else { this.sortBy = field; this.sortDir = 'desc'; }
    this.page = 1;
    this.load();
  }

  /** Returns the sort indicator for a table column. */
  sortIcon(field: SortField): string {
    if (this.sortBy !== field) return '↕';
    return this.sortDir === 'asc' ? '↑' : '↓';
  }

  /** Checks whether a column is currently sorted. */
  isActive(field: SortField): boolean { return this.sortBy === field; }

  /** Navigates to a valid page and reloads its results. */
  goPage(p: number) { if (p >= 1 && p <= this.totalPages) { this.page = p; this.load(); } }

  /** Builds the CSS classes for a message status badge. */
  badgeClass(status: string): string {
    return 'badge badge-' + status.toLowerCase().replace(/\s/g, '');
  }

  /** Shortens long message text for the history table. */
  truncate(s: string, n = 60): string {
    return s.length > n ? s.substring(0, n) + '…' : s;
  }

  /** Resets pagination and reloads results when search criteria changes. */
  onCriteriaChange() { this.page = 1; this.load(); }
}
