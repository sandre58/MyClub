/**
 * Manual mirrors of Application Reads DTOs.
 * ASP.NET Core JSON uses camelCase property names.
 * Enums are JSON strings (enum member names), not numbers.
 * See docs/guides/http-api-contract.md.
 *
 * TypeScript types document the expected shape at compile time.
 * They do NOT validate JSON at runtime — a mismatched API still type-checks.
 */

export type * from './types/enums';
export type * from './types/competitions';
export type * from './types/attention';
export type * from './types/matches';
export type * from './types/structure';
export type * from './types/overview';
export type * from './types/consultation';
export type * from './types/stages';

export {
  COMPETITION_NAME_MAX_LENGTH,
  MEMBER_DISPLAY_NAME_MAX_LENGTH,
} from './types/enums';
export { sideLabel, formatScore } from './types/matches';
