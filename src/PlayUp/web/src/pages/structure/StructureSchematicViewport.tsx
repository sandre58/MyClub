import type { ReactNode } from 'react';
import { WaitMark } from '../../design-system/components/WaitMark';
import type { StageSchematic } from '../../types';

export function StructureSchematicViewport({
  schematic,
  isError,
  isFetching,
  loadingLabel,
  unavailableLabel,
  children,
}: {
  schematic: StageSchematic | undefined;
  isError: boolean;
  isFetching: boolean;
  loadingLabel: string;
  unavailableLabel: string;
  children: (schematic: StageSchematic) => ReactNode;
}) {
  const showLoading = isFetching || (!schematic && !isError);

  return (
    <div
      className="structure-schematic-viewport__scale"
      aria-busy={showLoading || undefined}
    >
      {showLoading ? (
        <div className="structure-schematic-loading">
          <WaitMark size="region">{loadingLabel}</WaitMark>
        </div>
      ) : isError ? (
        <p className="structure-panel__muted">{unavailableLabel}</p>
      ) : schematic ? (
        children(schematic)
      ) : null}
    </div>
  );
}
