import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { RatioMessageGauge } from './RatioMessageGauge';

function meterTrack(container: HTMLElement): HTMLElement {
  const meter = container.querySelector('.ds-ratio-message-gauge__meter');
  expect(meter).toBeTruthy();
  return meter as HTMLElement;
}

describe('RatioMessageGauge', () => {
  it('uses attention track when over capacity with attention message (D5 surplus)', () => {
    const { container } = render(
      <RatioMessageGauge
        current={12}
        capacity={8}
        ratioCaption="Places"
        message="4 équipes en trop"
        messageKind="attention"
        ariaLabel="Population"
      />,
    );
    expect(meterTrack(container).getAttribute('data-tone')).toBe('attention');
  });

  it('uses error track when over capacity with danger message', () => {
    const { container } = render(
      <RatioMessageGauge
        current={12}
        capacity={8}
        ratioCaption="Places"
        message="Blocking surplus"
        messageKind="danger"
        ariaLabel="Population"
      />,
    );
    expect(meterTrack(container).getAttribute('data-tone')).toBe('error');
  });

  it('uses success track at exact capacity without over-capacity override', () => {
    const { container } = render(
      <RatioMessageGauge
        current={8}
        capacity={8}
        ratioCaption="Places"
        message="Complet"
        messageKind="success"
        ariaLabel="Population"
      />,
    );
    expect(meterTrack(container).getAttribute('data-tone')).toBe('success');
  });
});
