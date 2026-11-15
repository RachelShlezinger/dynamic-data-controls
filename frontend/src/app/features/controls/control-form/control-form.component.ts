import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';

import { ControlsService } from '../../../core/services/controls.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConditionBuilderComponent } from '../../../shared/components';
import { ControlDefinition } from '../../../shared/models';
import { OperatorType } from '../../../shared/models/enums';

/** Default empty definition for new controls. */
const EMPTY_DEFINITION: ControlDefinition = {
  primaryEntity: '',
  additionalEntities: [],
  conditions: [
    {
      id: 'c1',
      entityName: '',
      fieldName: '',
      operator: OperatorType.Equal,
      value: null,
      logicalOperator: null,
    },
  ],
};

@Component({
  selector: 'app-control-form',
  standalone: true,
  imports: [
    MatToolbarModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatIconModule,
    ConditionBuilderComponent,
  ],
  templateUrl: './control-form.component.html',
  styleUrl: './control-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ControlFormComponent implements OnInit {
  private readonly controlsService = inject(ControlsService);
  private readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);
  protected readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  /**
   * Bound to the `:id` route param via withComponentInputBinding() in app.config.
   * Undefined on the /controls/new route; a string id on /controls/:id/edit.
   */
  readonly controlId = input<string | undefined>(undefined, { alias: 'id' });
  readonly isEditMode = computed(() => !!this.controlId());

  // ── Form state ─────────────────────────────────────────────────────────────
  protected readonly name = signal('');
  protected readonly description = signal('');
  protected readonly definition = signal<ControlDefinition>(
    structuredClone(EMPTY_DEFINITION),
  );
  protected readonly isSaving = signal(false);
  protected readonly isLoading = signal(false);
  protected readonly nameError = signal('');

  // ── Lifecycle ──────────────────────────────────────────────────────────────

  ngOnInit(): void {
    if (this.isEditMode()) this.loadControl();
  }

  private loadControl(): void {
    this.isLoading.set(true);
    this.controlsService
      .getById(+this.controlId()!)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: c => {
          this.name.set(c.name);
          this.description.set(c.description ?? '');
          if (c.definition) this.definition.set(c.definition);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false),
      });
  }

  // ── Actions ────────────────────────────────────────────────────────────────

  protected save(): void {
    const trimmedName = this.name().trim();

    // Client-side validation
    if (!trimmedName) {
      this.nameError.set('שם הבקרה הוא שדה חובה');
      return;
    }
    if (this.definition().conditions.length === 0) {
      this.notificationService.showError('נדרש לפחות תנאי אחד');
      return;
    }

    this.nameError.set('');
    this.isSaving.set(true);

    const payload = {
      name: trimmedName,
      description: this.description().trim() || undefined,
      definition: this.definition(),
    };

    const req$ = this.isEditMode()
      ? this.controlsService.update(+this.controlId()!, payload)
      : this.controlsService.create(payload);

    req$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notificationService.showSuccess(
          this.isEditMode() ? 'הבקרה עודכנה בהצלחה' : 'הבקרה נוצרה בהצלחה',
        );
        this.router.navigate(['/controls']);
      },
      error: (err: HttpErrorResponse) => {
        this.isSaving.set(false);
        // 409: duplicate name — show specific warning (replaces the interceptor's generic toast)
        if (err.status === 409) {
          this.notificationService.showError(`בקרה בשם זה כבר קיימת: ${trimmedName}`);
        }
      },
    });
  }

  protected cancel(): void {
    this.router.navigate(['/controls']);
  }
}
