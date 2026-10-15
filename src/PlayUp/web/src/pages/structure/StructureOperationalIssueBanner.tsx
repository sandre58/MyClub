import { useTranslation } from 'react-i18next';
import type { StructureFormatKind } from '../../types';
import {
  structureDiagnosticTitleKey,
  structureDiagnosticTone,
} from './structureDiagnosticPresentation';
import { StripOperationalDiagnosticMessage } from './structureOperationalStripUi';

export function StructureOperationalIssueBanner({
  codes,
  formatKind,
}: {
  codes: string[];
  formatKind?: StructureFormatKind | null;
}) {
  const { t } = useTranslation('structure');

  if (codes.length === 0) {
    return null;
  }

  return (
    <div className="structure-operational-strip__message-stack">
      {codes.map((code) => {
        const tone = structureDiagnosticTone(code, { formatKind });
        return (
          <StripOperationalDiagnosticMessage key={code} tone={tone}>
            {t(structureDiagnosticTitleKey(code), {
              defaultValue: code,
            })}
          </StripOperationalDiagnosticMessage>
        );
      })}
    </div>
  );
}
