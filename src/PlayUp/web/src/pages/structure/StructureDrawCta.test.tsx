import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import {
  DrawCtaActionBody,
  resolveDrawCtaPoolTone,
  StructureDrawCta,
} from './StructureDrawCta';

describe('resolveDrawCtaPoolTone', () => {
  it('maps resolution states without implying Draw readiness', () => {
    expect(resolveDrawCtaPoolTone(0, 8)).toBe('neutral');
    expect(resolveDrawCtaPoolTone(3, 8)).toBe('partial');
    expect(resolveDrawCtaPoolTone(8, 8)).toBe('complete');
  });
});

describe('StructureDrawCta', () => {
  it('centers a larger chevron on the whole card', () => {
    render(
      <StructureDrawCta
        tone="emphasis"
        title="Lancer le tirage"
        body={
          <DrawCtaActionBody
            filled={8}
            capacity={8}
            teamsCaption="équipes résolues"
            poolTone="complete"
          />
        }
        onClick={() => {}}
      />,
    );
    const btn = screen.getByRole('button', { name: /Lancer le tirage/i });
    const head = btn.querySelector('.structure-draw-cta__head');
    expect(head?.querySelector('.structure-draw-cta__title')).not.toBeNull();
    expect(head?.querySelector('.structure-draw-cta__leading')).not.toBeNull();
    expect(head?.querySelector('.structure-draw-cta__chevron')).toBeNull();
    expect(
      btn.querySelector(':scope > .structure-draw-cta__chevron'),
    ).not.toBeNull();
  });

  it('keeps caption secondary and colors only the ratio by pool tone', () => {
    const { container, rerender } = render(
      <DrawCtaActionBody
        filled={0}
        capacity={8}
        teamsCaption="équipes résolues"
        poolTone="neutral"
      />,
    );
    expect(
      container.querySelector('[data-pool-tone="neutral"]'),
    ).not.toBeNull();
    expect(container.querySelector('.structure-draw-cta__mode-row')).toBeNull();

    rerender(
      <DrawCtaActionBody
        filled={3}
        capacity={8}
        teamsCaption="équipes résolues"
        poolTone="partial"
      />,
    );
    expect(
      container.querySelector('[data-pool-tone="partial"]'),
    ).not.toBeNull();

    rerender(
      <DrawCtaActionBody
        filled={8}
        capacity={8}
        teamsCaption="équipes résolues"
        poolTone="complete"
      />,
    );
    expect(
      container.querySelector('[data-pool-tone="complete"]'),
    ).not.toBeNull();
    expect(screen.getByText('équipes résolues')).toBeInTheDocument();
    expect(screen.queryByText(/prêt/i)).not.toBeInTheDocument();
  });
});
