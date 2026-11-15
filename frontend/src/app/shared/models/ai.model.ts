import { ControlDefinition } from './control.model';

/**
 * Request body for POST /api/ai/parse.
 * Validated server-side: description must be 10–1,000 characters.
 */
export interface AiParseRequest {
  description: string;
}

/**
 * Successful response from POST /api/ai/parse.
 *
 * The backend validates the Python AI response against the expected schema
 * and returns both the AI-suggested control name and the structured definition.
 * The Angular AI interface displays this for user review/editing before saving.
 *
 * On schema errors the API returns 422 { error, missingFields }.
 * On service unavailability it returns 503 { error }.
 */
export interface AiParseResponse {
  /** AI-suggested name for the new control — pre-fills the name field in the form. */
  controlName: string;
  /** Structured control definition ready to pass to ConditionBuilderComponent. */
  definition: ControlDefinition;
}
