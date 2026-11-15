import { DataType, ExecutionStatus } from './enums';

/**
 * Describes a single column in the anomaly results table.
 * Uses the business-friendly name — never the technical column name.
 */
export interface ColumnDefinition {
  friendlyName: string;
  dataType: DataType;
}

/**
 * A log entry for a single control execution run,
 * as stored in the ControlExecutions table.
 */
export interface ControlExecution {
  id: number;
  controlId: number;
  /** ISO timestamp of when the run started. */
  executedAt: string;
  /** Number of anomalous records found; null while still running. */
  anomalyCount: number | null;
  /** Wall-clock run duration in milliseconds; null while still running. */
  runTimeMs: number | null;
  status: ExecutionStatus;
}

/**
 * Immediate response returned by POST /api/executions.
 * Contains the execution identity and summary result.
 */
export interface ExecutionResult {
  executionId: number;
  controlId: number;
  status: ExecutionStatus;
  anomalyCount: number;
  /** True when results were capped at 10,000 records. */
  isTruncated: boolean;
}

/**
 * Paged response from GET /api/executions/{id}/results.
 * Maps to the response schema in design.md.
 */
export interface ResultsPage {
  executionId: number;
  controlId: number;
  totalCount: number;
  /** 1-based page number. */
  page: number;
  /** Fixed page size of 100. */
  pageSize: number;
  /** True when the query returned more than 10,000 rows and was truncated. */
  isTruncated: boolean;
  columns: ColumnDefinition[];
  /** Each record is a map of friendlyName → cell value. */
  records: Record<string, unknown>[];
}
