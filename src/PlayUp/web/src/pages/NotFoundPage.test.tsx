import { screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { renderWithI18n } from '../test/renderWithI18n';
import { NotFoundPage } from './NotFoundPage';

function renderNotFound() {
  renderWithI18n(
    <MemoryRouter initialEntries={['/missing']}>
      <Routes>
        <Route path="/missing" element={<NotFoundPage />} />
        <Route path="/" element={<p>Home route</p>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('NotFoundPage', () => {
  it('shows a localised 404 and a home link', () => {
    renderNotFound();

    expect(
      screen.getByRole('heading', { name: 'Page introuvable' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: 'Retour à l’accueil' }),
    ).toHaveAttribute('href', '/');
  });
});
