import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { AiService } from '../../core/services/ai.service';
import { ControlsService } from '../../core/services/controls.service';
import { NotificationService } from '../../core/services/notification.service';
import { ConditionBuilderComponent } from '../../shared/components';
import { ControlDefinition } from '../../shared/models';

@Component({
  selector: 'app-ai-interface',
  standalone: true,
  imports: [
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ConditionBuilderComponent,
  ],
  templateUrl: './ai-interface.component.html',
  styleUrl: './ai-interface.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AiInterfaceComponent {
  private readonly aiService = inject(AiService);
  private readonly controlsService = inject(ControlsService);
  private readonly router = inject(Router);
  private readonly notificationService = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly description = signal<string>('');
  protected readonly isAnalyzing = signal<boolean>(false);
  protected readonly parsedDefinition = signal<{
    controlName: string;
    definition: ControlDefinition;
  } | null>(null);
  protected readonly isSaving = signal<boolean>(false);

  protected readonly descriptionLength = computed(() => this.description().length);
  protected readonly canSubmit = computed(
    () =>
      this.description().length >= 10 &&
      this.description().length <= 1000 &&
      !this.isAnalyzing(),
  );

  protected analyzeDescription(): void {
    if (!this.canSubmit()) return;
    this.isAnalyzing.set(true);
    this.aiService
      .parseDescription(this.description())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          this.parsedDefinition.set(result);
          this.isAnalyzing.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.isAnalyzing.set(false);
          if (err.status === 503) {
            this.notificationService.showError('שירות ה-AI אינו זמין, נסה שנית מאוחר יותר');
          } else if (err.status === 422 && err.error?.missingFields) {
            const fields = (err.error.missingFields as string[]).join(', ');
            this.notificationService.showError(`שגיאה בתשובת ה-AI — שדות חסרים: ${fields}`);
          } else {
            this.notificationService.showError('אירעה שגיאה בניתוח התיאור, נסה שנית');
          }
        },
      });
  }

  protected confirmAndSave(): void {
    const parsed = this.parsedDefinition();
    if (!parsed) return;
    this.isSaving.set(true);
    this.controlsService
      .create({ name: parsed.controlName, definition: parsed.definition })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.notificationService.showSuccess('הבקרה נשמרה בהצלחה');
          this.router.navigate(['/controls']);
        },
        error: () => {
          this.isSaving.set(false);
          this.notificationService.showError('שמירת הבקרה נכשלה, נסה שנית');
        },
      });
  }

  protected reset(): void {
    this.parsedDefinition.set(null);
    this.description.set('');
  }

  protected onControlNameChange(name: string): void {
    this.parsedDefinition.update(p => (p ? { ...p, controlName: name } : null));
  }

  protected onDefinitionChange(definition: ControlDefinition): void {
    this.parsedDefinition.update(p => (p ? { ...p, definition } : null));
  }
}
