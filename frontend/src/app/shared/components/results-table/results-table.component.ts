import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  OnDestroy,
  computed,
  input,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { toObservable } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, Subject, takeUntil } from 'rxjs';

import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

import { ColumnDefinition, ResultsPage } from '../../models/execution.model';

export const PAGE_SIZE = 100;

/** Pure sort helper — exported for property-based tests (Property 7). */
export function sortRecords(
  records: Record<string, unknown>[],
  col: string,
  direction: 'asc' | 'desc'
): Record<string, unknown>[] {
  return [...records].sort((a, b) => {
    const av = a[col];
    const bv = b[col];
    if (av === null || av === undefined) return 1;
    if (bv === null || bv === undefined) return -1;
    const cmp = av < bv ? -1 : av > bv ? 1 : 0;
    return direction === 'asc' ? cmp : -cmp;
  });
}

/** Pure filter helper — exported for property-based tests (Property 6). */
export function filterRecords(
  records: Record<string, unknown>[],
  term: string
): Record<string, unknown>[] {
  if (!term.trim()) return records;
  const lower = term.toLowerCase();
  return records.filter(r =>
    Object.values(r).some(
      v => v !== null && v !== undefined && String(v).toLowerCase().includes(lower)
    )
  );
}

@Component({
  selector: 'app-results-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatInputModule,
    MatFormFieldModule,
    MatIconModule,
    MatButtonModule,
  ],
  templateUrl: './results-table.component.html',
  styleUrls: ['./results-table.component.scss'],
})
export class ResultsTableComponent implements OnInit, OnDestroy {
  // ── Signal inputs ─────────────────────────────────────────────────────────
  results = input.required<ResultsPage>();
  columns = input.required<ColumnDefinition[]>();

  // ── State signals ─────────────────────────────────────────────────────────
  searchText = signal('');
  sortColumn = signal<string | null>(null);
  sortDirection = signal<'asc' | 'desc'>('asc');
  currentPage = signal(1);

  /** Debounced mirror of searchText — updated after 300 ms of silence. */
  private debouncedSearch = signal('');
  private readonly destroy$ = new Subject<void>();

  // ── Derived computed signals ──────────────────────────────────────────────

  columnNames = computed(() => this.columns().map(c => c.friendlyName));

  /** Count of rows after filtering (for paginator length). */
  filteredTotal = computed(() =>
    filterRecords(this.results().records, this.debouncedSearch()).length
  );

  /** Visible rows: filtered → sorted → paginated. */
  filteredRows = computed(() => {
    let rows = filterRecords(this.results().records, this.debouncedSearch());
    const col = this.sortColumn();
    if (col) rows = sortRecords(rows, col, this.sortDirection());
    const start = (this.currentPage() - 1) * PAGE_SIZE;
    return rows.slice(start, start + PAGE_SIZE);
  });

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    toObservable(this.searchText)
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(v => {
        this.debouncedSearch.set(v);
        this.currentPage.set(1);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Sort ──────────────────────────────────────────────────────────────────

  toggleSort(column: string): void {
    if (this.sortColumn() === column) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColumn.set(column);
      this.sortDirection.set('asc');
    }
    this.currentPage.set(1);
  }

  // ── Pagination ────────────────────────────────────────────────────────────

  onPage(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
  }

  // ── CSV Export ────────────────────────────────────────────────────────────

  exportCsv(): void {
    const cols = this.columns();
    const headers = cols.map(c => `"${c.friendlyName}"`).join(',');
    const rows = this.results().records.map(r =>
      cols
        .map(c => {
          const val = r[c.friendlyName];
          const str = val === null || val === undefined ? '' : String(val);
          return `"${str.replace(/"/g, '""')}"`;
        })
        .join(',')
    );
    const csv = [headers, ...rows].join('\r\n');
    const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'תוצאות_בקרה.csv';
    link.click();
    URL.revokeObjectURL(url);
  }
}
