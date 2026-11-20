import type { ReactNode } from 'react';

type StructureManualPlacementSectionHeadProps = {
  label: string;
  toolbar?: ReactNode;
};

export function StructureManualPlacementSectionHead({
  label,
  toolbar,
}: StructureManualPlacementSectionHeadProps) {
  return (
    <div className="structure-manual-placement__section-head">
      <span className="structure-manual-placement__label">{label}</span>
      {toolbar ? (
        <div className="structure-manual-placement__section-toolbar">
          {toolbar}
        </div>
      ) : null}
    </div>
  );
}
