import { DataType, OperatorType } from './enums';

/**
 * Maps each DataType to the exact set of allowed OperatorType values,
 * as defined in the design operator table.
 *
 * Number / Date → 8 operators (Equal, NotEqual, comparisons, IsNull, IsNotNull)
 * String        → 6 operators (Equal, NotEqual, Contains, StartsWith, IsNull, IsNotNull)
 * Boolean       → 4 operators (Equal, NotEqual, IsNull, IsNotNull)
 *
 * Validates: Requirements 2.3
 */
export const OPERATORS_BY_TYPE: Record<DataType, OperatorType[]> = {
  [DataType.Number]: [
    OperatorType.Equal,
    OperatorType.NotEqual,
    OperatorType.GreaterThan,
    OperatorType.LessThan,
    OperatorType.GreaterThanOrEqual,
    OperatorType.LessThanOrEqual,
    OperatorType.IsNull,
    OperatorType.IsNotNull,
  ],
  [DataType.Date]: [
    OperatorType.Equal,
    OperatorType.NotEqual,
    OperatorType.GreaterThan,
    OperatorType.LessThan,
    OperatorType.GreaterThanOrEqual,
    OperatorType.LessThanOrEqual,
    OperatorType.IsNull,
    OperatorType.IsNotNull,
  ],
  [DataType.String]: [
    OperatorType.Equal,
    OperatorType.NotEqual,
    OperatorType.Contains,
    OperatorType.StartsWith,
    OperatorType.IsNull,
    OperatorType.IsNotNull,
  ],
  [DataType.Boolean]: [
    OperatorType.Equal,
    OperatorType.NotEqual,
    OperatorType.IsNull,
    OperatorType.IsNotNull,
  ],
};
