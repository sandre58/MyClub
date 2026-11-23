import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';
import { redoBlockReason } from './drawActions';

const root = dirname(fileURLToPath(import.meta.url));
const fr = JSON.parse(
  readFileSync(join(root, '../../../i18n/locales/fr/draw.json'), 'utf8'),
) as { redoBlocked: Record<string, string> };
const en = JSON.parse(
  readFileSync(join(root, '../../../i18n/locales/en/draw.json'), 'utf8'),
) as { redoBlocked: Record<string, string> };

const partialCodes = [
  ['Application.DrawNoFreeSlot', 'noFreeSlot'],
  ['Application.DrawNotEnoughTeams', 'notEnoughTeams'],
  ['Application.DrawTooManyTeams', 'tooManyTeams'],
  ['Application.DrawLaunchGridNotVacant', 'launchGridNotVacant'],
] as const;

const codes = [
  ['Application.DrawGroupSeatsReservedByInboundFeed', 'reservedSeats'],
  ['Application.DrawFixedPotConflict', 'fixedPotConflict'],
  ['Application.DrawFixedOutsideComposition', 'fixedOutsideComposition'],
  ['Application.DrawFixedExceedsCapacity', 'fixedExceedsCapacity'],
  ['Application.DrawGroupShapeInvalid', 'groupShape'],
  ['Application.DrawPotsMissing', 'missingPots'],
] as const;

describe('redoBlockReason', () => {
  it('maps partial cup redo codes away from lifecycle', () => {
    for (const [code, reason] of partialCodes) {
      expect(redoBlockReason(code)).toBe(reason);
      expect(redoBlockReason(code)).not.toBe('lifecycle');
    }
  });

  it('maps the six group launch codes away from lifecycle', () => {
    for (const [code, reason] of codes) {
      expect(redoBlockReason(code)).toBe(reason);
      expect(fr.redoBlocked[reason]).toMatch(
        /^Le tirage ne peut pas être refait : /,
      );
      expect(en.redoBlocked[reason].length).toBeGreaterThan(0);
    }
  });
});
