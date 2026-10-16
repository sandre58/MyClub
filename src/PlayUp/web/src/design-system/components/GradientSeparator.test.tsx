import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { GradientSeparator } from './GradientSeparator';

describe('GradientSeparator', () => {
  it('renders horizontal orientation with aria-hidden', () => {
    const { container } = render(<GradientSeparator orientation="horizontal" />);
    const el = container.firstElementChild;
    expect(el).toHaveClass('ds-gradient-separator', 'ds-gradient-separator--horizontal');
    expect(el).toHaveAttribute('aria-hidden', 'true');
  });

  it('merges custom className', () => {
    const { container } = render(
      <GradientSeparator orientation="vertical" className="custom-sep" />,
    );
    expect(container.firstElementChild).toHaveClass('custom-sep');
  });
});
