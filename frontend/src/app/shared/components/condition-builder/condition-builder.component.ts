import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  model,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';

import { MetadataService } from '../../../core/services/metadata.service';
import {
  BusinessEntity,
  BusinessField,
  ConditionDefinition,
  ControlDefinition,
} from '../../models';
import { DataType, LogicalOperator, OperatorType } from '../../models/enums';
import { OPERATORS_BY_TYPE } from '../../models/operators';

@Component({
  selector: 'app-condition-builder',
  standalone: true,
  imports: [
    MatSelectModule,
    MatInputModule,
    MatFormFieldModule,
    MatDatepickerModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
  ],
  providers: [provideNativeDateAdapter()],
  templateUrl: './condition-builder.component.html',
  styleUrl: './condition-builder.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConditionBuilderComponent implements OnInit {
  /** Two-way bindable definition signal — required. */
  readonly definition = model.required<ControlDefinition>();

  private readonly metadataService = inject(MetadataService);
  private readonly fieldsByEntity = signal<Map<string, BusinessField[]>>(new Map());
  private readonly typeErrors = signal<Map<number, string>>(new Map());

  protected readonly entities = signal<BusinessEntity[]>([]);

  /** True when any condition has a type-mismatch error. Usable by parent validators. */
  readonly hasErrors = computed(() => this.typeErrors().size > 0);

  private readonly operatorLabels: Record<OperatorType, string> = {
    [OperatorType.Equal]: 'שווה',
    [OperatorType.NotEqual]: 'שונה',
    [OperatorType.GreaterThan]: 'גדול מ',
    [OperatorType.LessThan]: 'קטן מ',
    [OperatorType.GreaterThanOrEqual]: 'גדול שווה',
    [OperatorType.LessThanOrEqual]: 'קטן שווה',
    [OperatorType.Contains]: 'מכיל',
    [OperatorType.StartsWith]: 'מתחיל ב',
    [OperatorType.IsNull]: 'ריק',
    [OperatorType.IsNotNull]: 'אינו ריק',
  };

  ngOnInit(): void {
    this.metadataService.getEntities().subscribe(list => {
      this.entities.set(list);
      // Pre-load fields for entities already referenced in an existing definition
      const usedNames = [
        ...new Set(this.definition().conditions.map(c => c.entityName).filter(Boolean)),
      ];
      for (const name of usedNames) {
        const entity = list.find(e => e.friendlyName === name);
        if (entity) this.loadFieldsForEntity(entity.id, name);
      }
    });
  }

  // ---------------------------------------------------------------------------
  // Private helpers
  // ---------------------------------------------------------------------------

  private loadFieldsForEntity(entityId: number, entityName: string): void {
    if (this.fieldsByEntity().has(entityName)) return;
    this.metadataService.getEntityFields(entityId).subscribe(fields => {
      this.fieldsByEntity.update(map => new Map(map).set(entityName, fields));
    });
  }

  private nextId(): string {
    const nums = this.definition().conditions
      .map(c => parseInt(c.id.replace(/\D/g, ''), 10))
      .filter(n => !isNaN(n));
    return `c${nums.length > 0 ? Math.max(...nums) + 1 : 1}`;
  }

  private deriveEntityFields(conditions: ConditionDefinition[]): Partial<ControlDefinition> {
    const names = [...new Set(conditions.map(c => c.entityName).filter(Boolean))];
    return {
      primaryEntity: names[0] ?? this.definition().primaryEntity,
      additionalEntities: names.slice(1),
    };
  }

  // ---------------------------------------------------------------------------
  // Protected — used by template
  // ---------------------------------------------------------------------------

  protected fieldsFor(entityName: string): BusinessField[] {
    return this.fieldsByEntity().get(entityName) ?? [];
  }

  protected operatorsFor(fieldName: string, entityName: string): OperatorType[] {
    const field = this.fieldsFor(entityName).find(f => f.friendlyName === fieldName);
    return field ? OPERATORS_BY_TYPE[field.dataType] : Object.values(OperatorType);
  }

  protected operatorLabel(op: OperatorType): string {
    return this.operatorLabels[op] ?? op;
  }

  protected isDateField(fieldName: string, entityName: string): boolean {
    return (
      this.fieldsFor(entityName).find(f => f.friendlyName === fieldName)?.dataType ===
      DataType.Date
    );
  }

  protected isNullOperator(op: OperatorType): boolean {
    return op === OperatorType.IsNull || op === OperatorType.IsNotNull;
  }

  /**
   * Parses an ISO date string to a JS Date for the Material DatePicker.
   * Uses local-time construction to avoid UTC-midnight off-by-one issues.
   */
  protected parseDate(value: string | number | boolean | null): Date | null {
    if (!value || typeof value !== 'string') return null;
    const parts = value.split('-').map(Number);
    if (parts.length !== 3) return null;
    const [year, month, day] = parts;
    const d = new Date(year, month - 1, day);
    return isNaN(d.getTime()) ? null : d;
  }

  /** Formats a Date to an ISO date string ('YYYY-MM-DD'). */
  protected formatDate(date: Date | null): string | null {
    if (!date) return null;
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  protected hasTypeError(index: number): boolean {
    return this.typeErrors().has(index);
  }

  protected typeErrorMessage(index: number): string {
    return this.typeErrors().get(index) ?? '';
  }

  // ---------------------------------------------------------------------------
  // Public mutation API
  // ---------------------------------------------------------------------------

  protected addCondition(): void {
    if (this.definition().conditions.length >= 20) return;
    const prev = this.definition().conditions;
    // Ensure the previous last condition carries a logical operator
    const withLogic = prev.map((c, i) =>
      i === prev.length - 1
        ? { ...c, logicalOperator: c.logicalOperator ?? LogicalOperator.And }
        : c,
    );
    const entityName = this.definition().primaryEntity ?? '';
    const newCondition: ConditionDefinition = {
      id: this.nextId(),
      entityName,
      fieldName: '',
      operator: OperatorType.Equal,
      value: null,
      logicalOperator: null,
    };
    const conditions = [...withLogic, newCondition];
    this.definition.update(def => ({ ...def, conditions, ...this.deriveEntityFields(conditions) }));
    if (entityName) {
      const entity = this.entities().find(e => e.friendlyName === entityName);
      if (entity) this.loadFieldsForEntity(entity.id, entityName);
    }
  }

  protected removeCondition(index: number): void {
    this.definition.update(def => {
      const conditions = def.conditions.filter((_, i) => i !== index);
      if (conditions.length > 0) {
        conditions[conditions.length - 1] = {
          ...conditions[conditions.length - 1],
          logicalOperator: null,
        };
      }
      return { ...def, conditions, ...this.deriveEntityFields(conditions) };
    });
    // Re-index type errors after removal
    this.typeErrors.update(map => {
      const updated = new Map<number, string>();
      for (const [idx, err] of map) {
        if (idx < index) updated.set(idx, err);
        else if (idx > index) updated.set(idx - 1, err);
      }
      return updated;
    });
  }

  protected onEntityChange(index: number, entityName: string): void {
    this.definition.update(def => {
      const conditions = def.conditions.map((c, i) =>
        i === index
          ? { ...c, entityName, fieldName: '', operator: OperatorType.Equal, value: null }
          : c,
      );
      return { ...def, conditions, ...this.deriveEntityFields(conditions) };
    });
    const entity = this.entities().find(e => e.friendlyName === entityName);
    if (entity) this.loadFieldsForEntity(entity.id, entityName);
  }

  protected onFieldChange(index: number, fieldName: string): void {
    const cond = this.definition().conditions[index];
    const availableOps = this.operatorsFor(fieldName, cond.entityName);
    const operator = availableOps.includes(cond.operator) ? cond.operator : availableOps[0];
    this.updateCondition(index, { fieldName, operator, value: null });
  }

  protected updateCondition(index: number, partial: Partial<ConditionDefinition>): void {
    this.definition.update(def => ({
      ...def,
      conditions: def.conditions.map((c, i) => (i === index ? { ...c, ...partial } : c)),
    }));
  }

  /**
   * Validates the raw string value against the field's DataType on input blur.
   * Marks the condition invalid and shows a Hebrew message on type mismatch.
   */
  protected onValueBlur(
    index: number,
    rawValue: string,
    cond: ConditionDefinition,
  ): void {
    const field = this.fieldsFor(cond.entityName).find(f => f.friendlyName === cond.fieldName);
    if (!field) return;

    let parsed: string | number | boolean | null = rawValue || null;
    let error = '';

    if (rawValue && field.dataType === DataType.Number) {
      const num = Number(rawValue);
      if (isNaN(num)) error = 'ערך חייב להיות מספר';
      else parsed = num;
    } else if (rawValue && field.dataType === DataType.Boolean) {
      const lower = rawValue.toLowerCase();
      if (['true', 'כן', '1'].includes(lower)) parsed = true;
      else if (['false', 'לא', '0'].includes(lower)) parsed = false;
      else error = 'ערך חייב להיות: true / false';
    }

    this.typeErrors.update(map => {
      const updated = new Map(map);
      if (error) updated.set(index, error);
      else updated.delete(index);
      return updated;
    });

    if (!error) this.updateCondition(index, { value: parsed });
  }
}
