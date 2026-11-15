import { DataType } from './enums';

/**
 * A business-friendly representation of a database column.
 * Technical details (columnName, entityId) are present for admin use
 * but are never displayed to end-users in the condition builder.
 */
export interface BusinessField {
  id: number;
  /** FK to the owning BusinessEntity. */
  entityId: number;
  /** User-visible label (unique within the entity, 1–100 chars). */
  friendlyName: string;
  /** Actual database column name — not shown to end-users. */
  columnName: string;
  dataType: DataType;
  /** Optional tooltip / description shown in the condition builder (≤ 200 chars). */
  description: string | null;
}

/**
 * A business-friendly representation of a database table.
 * The tableName is technical metadata used by the backend only.
 */
export interface BusinessEntity {
  id: number;
  /** User-visible label (unique across the system, 1–100 chars). */
  friendlyName: string;
  /** Actual database table name — not shown to end-users. */
  tableName: string;
  description: string | null;
  /** Populated when fetching a single entity with its fields. */
  fields?: BusinessField[];
}

/**
 * A defined JOIN relationship between two business entities.
 * Used by QueryBuilderService to generate INNER JOIN clauses.
 */
export interface EntityRelationship {
  id: number;
  sourceEntityId: number;
  targetEntityId: number;
  /** FK field in the source entity used for the JOIN. */
  sourceFieldId: number;
  /** FK field in the target entity used for the JOIN. */
  targetFieldId: number;
}
