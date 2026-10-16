import { describe, expect, it } from 'vitest';
import {
  inboundHubFeedKey,
  inboundSeatsForFeedKey,
  inboundSeatsForFeedRule,
  qualPathOrderFromFeedKey,
  qualPathOrderFromFeedKeyLoose,
} from './structureCompositionInboundHub';

describe('structureCompositionInboundHub', () => {
  it('maps hub seats by feed key', () => {
    const inbound = {
      configuredVolume: 1,
      resolvedCount: 1,
      rules: [
        {
          feedKey: 'p-src-P1-Winner',
          sourceStageId: 'src',
          seats: [
            {
              entryId: 'e1',
              displayName: 'Pau FC',
              placed: true,
            },
          ],
        },
      ],
    };

    const slots = inboundSeatsForFeedKey(inbound, 'p-src-P1-Winner', 1);
    expect(slots).toHaveLength(1);
    expect(slots[0]).toMatchObject({
      resolved: true,
      teamName: 'Pau FC',
      entryId: 'e1',
      placed: true,
    });
  });

  it('pads unresolved seats when hub rule is missing', () => {
    const slots = inboundSeatsForFeedKey(null, 'missing', 2);
    expect(slots).toHaveLength(2);
    expect(slots.every((slot) => !slot.resolved)).toBe(true);
  });

  it('parses qual path order when stage id casing differs in feed key', () => {
    const upper = '550E8400-E29B-41D4-A716-446655440000';
    const lower = upper.toLowerCase();
    expect(
      qualPathOrderFromFeedKey(`q-${upper}-3`, lower),
    ).toBe(3);
    expect(qualPathOrderFromFeedKeyLoose(`q-${upper}-3`)).toBe(3);
  });

  it('resolves hub rule via qual path order when UI key is intent-based', () => {
    const inbound = {
      configuredVolume: 1,
      resolvedCount: 1,
      rules: [
        {
          feedKey: 'q-src-1',
          sourceStageId: 'src',
          seats: [
            {
              entryId: 'e1',
              displayName: 'Team A',
              placed: true,
            },
          ],
        },
      ],
    };

    const slots = inboundSeatsForFeedRule(
      inbound,
      {
        uiFeedKey: 'q-i-src-intent-abc',
        sourceStageId: 'src',
        qualPathOrder: 1,
      },
      1,
    );
    expect(slots[0]).toMatchObject({
      resolved: true,
      placed: true,
      entryId: 'e1',
    });
    expect(
      inboundHubFeedKey({
        uiFeedKey: 'q-i-src-x',
        sourceStageId: 'src',
        qualPathOrder: 2,
      }),
    ).toBe('q-src-2');
  });

  it('forwards placed from hub for unresolved structural seats', () => {
    const inbound = {
      configuredVolume: 1,
      resolvedCount: 0,
      rules: [
        {
          feedKey: 'p-src-P1-Winner',
          sourceStageId: 'src',
          seats: [{ entryId: null, displayName: null, placed: true }],
        },
      ],
    };

    const slots = inboundSeatsForFeedKey(inbound, 'p-src-P1-Winner', 1);
    expect(slots[0].placed).toBe(true);
    expect(slots[0].resolved).toBe(false);
  });
});
