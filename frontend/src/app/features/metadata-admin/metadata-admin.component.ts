import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';

import { MetadataService } from '../../core/services/metadata.service';
import { NotificationService } from '../../core/services/notification.service';
import {
  BusinessEntity,
  BusinessField,
  EntityRelationship,
} from '../../shared/models';
import { MetadataEntityTableComponent } from './metadata-entity-table.component';
import { MetadataFieldsTableComponent } from './metadata-fields-table.component';

@Component({
  selector: 'app-metadata-admin',
  standalone: true,
  imports: [
    MatToolbarModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MetadataEntityTableComponent,
    MetadataFieldsTableComponent,
  ],
  templateUrl: './metadata-admin.component.html',
  styleUrl: './metadata-admin.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MetadataAdminComponent {
  private readonly metadataService = inject(MetadataService);
  private readonly notificationService = inject(NotificationService);

  readonly entities = signal<BusinessEntity[]>([]);
  readonly relationships = signal<EntityRelationship[]>([]);
  readonly selectedEntityId = signal<number | null>(null);
  readonly fields = signal<BusinessField[]>([]);
  readonly isLoading = signal(false);

  readonly relColumns = ['sourceEntity', 'targetEntity'];

  constructor() {
    this.isLoading.set(true);
    forkJoin({
      entities: this.metadataService.getEntities(),
      relationships: this.metadataService.getRelationships(),
    })
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: ({ entities, relationships }) => {
          this.entities.set(entities);
          this.relationships.set(relationships);
          this.isLoading.set(false);
        },
        error: () => {
          this.notificationService.showError('שגיאה בטעינת המטאדטה');
          this.isLoading.set(false);
        },
      });
  }

  selectEntity(id: number): void {
    this.selectedEntityId.set(id);
    this.fields.set([]);
    this.metadataService
      .getEntityFields(id)
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: fields => this.fields.set(fields),
        error: () =>
          this.notificationService.showError('שגיאה בטעינת שדות הישות'),
      });
  }

  selectedEntityName(): string {
    const id = this.selectedEntityId();
    return this.entities().find(e => e.id === id)?.friendlyName ?? '';
  }

  entityName(id: number): string {
    return this.entities().find(e => e.id === id)?.friendlyName ?? `#${id}`;
  }
}
