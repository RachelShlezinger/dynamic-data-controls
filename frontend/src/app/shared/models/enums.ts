/**
 * Operator types for condition definitions.
 * Values match the backend C# OperatorType enum strings exactly.
 */
export enum OperatorType {
  Equal = 'Equal',
  NotEqual = 'NotEqual',
  GreaterThan = 'GreaterThan',
  LessThan = 'LessThan',
  GreaterThanOrEqual = 'GreaterThanOrEqual',
  LessThanOrEqual = 'LessThanOrEqual',
  Contains = 'Contains',
  StartsWith = 'StartsWith',
  IsNull = 'IsNull',
  IsNotNull = 'IsNotNull',
}

/**
 * Logical operators combining conditions within a control definition.
 * Values match the JSON schema: 'AND' | 'OR'.
 */
export enum LogicalOperator {
  And = 'AND',
  Or = 'OR',
}

/**
 * Data type of a business field.
 * Values match the DB CHECK constraint: 'Number' | 'String' | 'Date' | 'Boolean'.
 */
export enum DataType {
  Number = 'Number',
  String = 'String',
  Date = 'Date',
  Boolean = 'Boolean',
}

/**
 * Execution lifecycle status.
 * Values match the DB CHECK constraint: 'Running' | 'Success' | 'Error' | 'Cancelled'.
 */
export enum ExecutionStatus {
  Running = 'Running',
  Success = 'Success',
  Error = 'Error',
  Cancelled = 'Cancelled',
}
