import { describe, expect, it } from 'vitest';
import { inboundSeatsForFeedKey } from './structureCompositionInboundHub';

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
