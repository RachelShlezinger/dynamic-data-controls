import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { ExecutionService } from '../../../core/services/execution.service';
import { ControlsService } from '../../../core/services/controls.service';
import { NotificationService } from '../../../core/services/notification.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ResultsTableComponent } from '../../../shared/components/results-table/results-table.component';
import { ExecutionResult, ResultsPage } from '../../../shared/models';

@Component({
  selector: 'app-control-execution',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './control-execution.component.html',
  styleUrl: './control-execution.component.scss',
  imports: [
    MatButtonModule,
    MatIconModule,
    LoadingSpinnerComponent,
    ResultsTableComponent,
  ],
})
export class ControlExecutionComponent implements OnInit {
  private readonly executionService = inject(ExecutionService);
  private readonly controlsService = inject(ControlsService);
  private readonly notificationService = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);

  /** Route param :id coerced to a number. Evaluated once on first access. */
  readonly controlId = computed(() => +this.route.snapshot.params['id']);

  protected readonly controlName = signal<string>('');

  readonly isRunning = signal(false);
  readonly executionId = signal<number | null>(null);
  readonly result = signal<ExecutionResult | null>(null);
  readonly showCancelWarning = signal(false);
  protected readonly resultsPage = signal<ResultsPage | null>(null);

  /** Handle for the 30-second warning timer — cleared on completion/cancel. */
  private warningTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    this.destroyRef.onDestroy(() => this.clearTimer());
  }

  ngOnInit(): void {
    this.controlsService
      .getById(this.controlId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: ctrl => this.controlName.set(ctrl.name) });
  }

  // ── Actions ─────────────────────────────────────────────────────────────────

  /**
   * Start the control execution.
   * Guards against duplicate runs both locally (isRunning) and via server 409.
   * Requirements: 4.4, 4.5, 4.7, 4.8
   */
  run(): void {
    if (this.isRunning()) {
      this.notificationService.showError('הרצה כבר פעילה עבור בקרה זו');
      return;
    }

    this.isRunning.set(true);
    this.result.set(null);
    this.resultsPage.set(null);
    this.showCancelWarning.set(false);

    // Show cancel-warning banner after 30 s — Requirement 4.5, 4.7
    this.warningTimer = setTimeout(() => this.showCancelWarning.set(true), 30_000);

    this.executionService
      .run(this.controlId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          this.clearTimer();
          this.result.set(result);
          this.executionId.set(result.executionId);
          this.isRunning.set(false);
          this.showCancelWarning.set(false);
          this.loadResults(result.executionId);
        },
        error: (err: HttpErrorResponse) => {
          this.clearTimer();
          this.isRunning.set(false);
          this.showCancelWarning.set(false);
          if (err.status === 409) {
            // Requirement 4.8 — concurrent execution rejected by server
            this.notificationService.showError('הרצה כבר פעילה עבור בקרה זו');
          }
          // All other errors are handled globally by ErrorInterceptor
        },
      });
  }

  /**
   * Cancel the in-flight execution and clear the warning timer.
   * Requirement: 4.9
   */
  cancel(): void {
    const id = this.executionId();
    if (id == null) return;

    this.clearTimer();
    this.executionService
      .cancel(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isRunning.set(false);
          this.showCancelWarning.set(false);
          this.notificationService.showSuccess('הרצה בוטלה');
        },
      });
  }

  // ── Private helpers ──────────────────────────────────────────────────────────

  private loadResults(executionId: number): void {
    this.executionService
      .getResults(executionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: page => this.resultsPage.set(page) });
  }

  private clearTimer(): void {
    if (this.warningTimer !== null) {
      clearTimeout(this.warningTimer);
      this.warningTimer = null;
    }
  }
}
