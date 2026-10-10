import { useTranslation } from 'react-i18next';
import {
  structureDiagnosticTitleKey,
  structureDiagnosticTone,
} from './structureDiagnosticPresentation';
import { StripOperationalDiagnosticMessage } from './structureOperationalStripUi';

export function StructureOperationalIssueBanner({ codes }: { codes: string[] }) {
  const { t } = useTranslation('structure');

  if (codes.length === 0) {
    return null;
  }

  return (
    <div className="structure-operational-strip__message-stack">
      {codes.map((code) => {
        const tone = structureDiagnosticTone(code);
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
