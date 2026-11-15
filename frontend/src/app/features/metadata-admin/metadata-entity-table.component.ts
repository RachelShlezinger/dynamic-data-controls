import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';

import { BusinessEntity } from '../../shared/models';

@Component({
  selector: 'app-metadata-entity-table',
  standalone: true,
  imports: [MatTableModule, MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './metadata-entity-table.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MetadataEntityTableComponent {
  readonly entities = input.required<BusinessEntity[]>();
  readonly selectedId = input<number | null>(null);
  readonly selectEntity = output<number>();

  readonly columns = ['friendlyName', 'tableName', 'fieldCount', 'actions'];
}
