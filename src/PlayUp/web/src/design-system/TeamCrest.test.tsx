import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { TeamCrest } from './TeamCrest';

describe('TeamCrest', () => {
  it('renders logo image with native lazy loading when logoMediaId is set', () => {
    const { container } = render(
      <TeamCrest
        name="Paris Saint-Germain"
        logoMediaId="aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
      />,
    );

    const image = container.querySelector('img');
    expect(image).not.toBeNull();
    expect(image).toHaveAttribute('loading', 'lazy');
    expect(image).toHaveAttribute(
      'src',
      '/media/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/content',
    );
  });

  it('renders placeholder crest when logoMediaId is absent', () => {
    const { container } = render(<TeamCrest name="Paris Saint-Germain" />);

    expect(screen.queryByRole('img')).toBeNull();
    expect(container.querySelector('.team-crest__initial')).toHaveTextContent(
      'PS',
    );
  });
});
