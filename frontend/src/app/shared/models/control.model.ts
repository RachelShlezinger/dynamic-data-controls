import { LogicalOperator, OperatorType } from './enums';

/**
 * A single condition within a control definition.
 * Aligns with the ControlDefinition JSON schema in design.md.
 */
export interface ConditionDefinition {
  /** Unique identifier within the definition (e.g. 'c1', 'c2'). */
  id: string;
  /** Friendly name of the business entity this condition targets. */
  entityName: string;
  /** Friendly name of the business field this condition targets. */
  fieldName: string;
  /** Comparison operator — must be valid for the field's DataType. */
  operator: OperatorType;
  /**
   * The comparison value.
   * null for IsNull / IsNotNull operators.
   * string for Date values in ISO-8601 format.
   */
  value: string | number | boolean | null;
  /**
   * Logical connector to the NEXT condition in the list.
   * null on the last condition.
   */
  logicalOperator: LogicalOperator | null;
}

/**
 * Structured definition of a data-quality control rule.
 * Serialised as JSON in the Controls.DefinitionJson column.
 */
export interface ControlDefinition {
  /** Friendly name of the primary (driving) business entity. */
  primaryEntity: string;
  /** Friendly names of additional entities joined in the query. */
  additionalEntities: string[];
  /** Ordered list of conditions; at least one is required. */
  conditions: ConditionDefinition[];
}

/**
 * Request body for POST /api/controls.
 */
export interface CreateControlRequest {
  name: string;
  description?: string;
  definition: ControlDefinition;
}

/**
 * Request body for PUT /api/controls/{id}.
 */
export interface UpdateControlRequest {
  name: string;
  description?: string;
  definition: ControlDefinition;
}

/**
 * A data-quality control as returned by the API.
 * The `definition` field is present on single-item GET responses;
 * it may be absent on list responses to reduce payload size.
 */
export interface Control {
  id: number;
  name: string;
  description: string | null;
  definition?: ControlDefinition;
  createdAt: string;
  updatedAt: string;
  /** ISO timestamp of the most recent execution; null / undefined = "טרם הורצה". */
  lastExecutedAt?: string | null;
}
