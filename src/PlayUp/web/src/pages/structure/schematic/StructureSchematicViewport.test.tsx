import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { StageSchematic } from '../../../types';
import { StructureSchematicViewport } from './StructureSchematicViewport';

const schematic = {
  cases: [],
} as unknown as StageSchematic;

describe('StructureSchematicViewport', () => {
  it('shows regional loader on initial load', () => {
    render(
      <StructureSchematicViewport
        schematic={undefined}
        isError={false}
        isFetching
        loadingLabel="Loading schematic…"
        unavailableLabel="Unavailable"
      >
        {() => <div data-testid="schematic-body">body</div>}
      </StructureSchematicViewport>,
    );

    expect(screen.getByText('Loading schematic…')).toBeInTheDocument();
    expect(screen.queryByTestId('schematic-body')).not.toBeInTheDocument();
  });

  it('replaces schematic with WaitMark during background refetch', () => {
    render(
      <StructureSchematicViewport
        schematic={schematic}
        isError={false}
        isFetching
        loadingLabel="Loading schematic…"
        unavailableLabel="Unavailable"
      >
        {() => <div data-testid="schematic-body">body</div>}
      </StructureSchematicViewport>,
    );

    expect(screen.getByText('Loading schematic…')).toBeInTheDocument();
    expect(screen.queryByTestId('schematic-body')).not.toBeInTheDocument();
  });

  it('shows schematic when fetch is idle', () => {
    render(
      <StructureSchematicViewport
        schematic={schematic}
        isError={false}
        isFetching={false}
        loadingLabel="Loading schematic…"
        unavailableLabel="Unavailable"
      >
        {() => <div data-testid="schematic-body">body</div>}
      </StructureSchematicViewport>,
    );

    expect(screen.getByTestId('schematic-body')).toBeInTheDocument();
    expect(screen.queryByText('Loading schematic…')).not.toBeInTheDocument();
  });
});
