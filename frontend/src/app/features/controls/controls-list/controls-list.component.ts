import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';

import { ControlsService } from '../../../core/services/controls.service';

/** Shared date format for all date cells — Hebrew short month. */
const HE_DATE: Intl.DateTimeFormatOptions = {
  year: 'numeric',
  month: 'short',
  day: 'numeric',
};

@Component({
  selector: 'app-controls-list',
  standalone: true,
  imports: [
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatTableModule,
    MatTooltipModule,
  ],
  templateUrl: './controls-list.component.html',
  styleUrl: './controls-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ControlsListComponent {
  private readonly controlsService = inject(ControlsService);
  private readonly router = inject(Router);

  /** Source-of-truth list from the service signal. */
  readonly controls = this.controlsService.controls;

  /** Bound to the search input; triggers the debounced pipeline below. */
  readonly searchText = signal<string>('');

  /** True while the initial getAll() request is in-flight. */
  readonly isLoading = signal(false);

  /**
   * Debounced copy of searchText — updated after 300 ms of silence.
   * `filteredControls` reads this so the computed only re-runs after the debounce.
   */
  private readonly debouncedSearch = signal<string>('');

  /** Case-insensitive filter over both name and description. */
  readonly filteredControls = computed(() => {
    const q = this.debouncedSearch().toLowerCase().trim();
    if (!q) return this.controls();
    return this.controls().filter(
      c =>
        c.name.toLowerCase().includes(q) ||
        (c.description ?? '').toLowerCase().includes(q),
    );
  });

  readonly displayedColumns = [
    'name',
    'description',
    'createdAt',
    'lastRun',
    'actions',
  ];

  constructor() {
    // Wire 300 ms debounce for the search signal — must be in injection context.
    toObservable(this.searchText)
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(v => this.debouncedSearch.set(v));

    // Initial load — also auto-unsubscribed via takeUntilDestroyed().
    this.isLoading.set(true);
    this.controlsService
      .getAll()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: () => this.isLoading.set(false),
        error: () => this.isLoading.set(false),
      });
  }

  // ── Event handlers ──────────────────────────────────────────────────────────

  onSearch(value: string): void {
    this.searchText.set(value);
  }

  runControl(id: number): void {
    this.router.navigate(['/controls', id, 'run']);
  }

  editControl(id: number): void {
    this.router.navigate(['/controls', id, 'edit']);
  }

  // ── Template helpers ─────────────────────────────────────────────────────────

  /** Returns "טרם הורצה" for controls that have never been executed. */
  formatLastRun(date?: string | null): string {
    if (!date) return 'טרם הורצה';
    return new Date(date).toLocaleDateString('he-IL', HE_DATE);
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('he-IL', HE_DATE);
  }

  /** Clips text to `max` characters and appends an ellipsis when truncated. */
  truncate(text: string | null | undefined, max = 100): string {
    if (!text) return '';
    return text.length > max ? `${text.slice(0, max)}…` : text;
  }
}
