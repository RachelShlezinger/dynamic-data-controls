import {
  ChangeDetectionStrategy,
  Component,
  input,
} from '@angular/core';
import { MatTableModule } from '@angular/material/table';

import { BusinessField } from '../../shared/models';

@Component({
  selector: 'app-metadata-fields-table',
  standalone: true,
  imports: [MatTableModule],
  templateUrl: './metadata-fields-table.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MetadataFieldsTableComponent {
  readonly fields = input.required<BusinessField[]>();
  readonly title = input<string>('שדות');

  readonly columns = ['friendlyName', 'columnName', 'dataType'];
}
