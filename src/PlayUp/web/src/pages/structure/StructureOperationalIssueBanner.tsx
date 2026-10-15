import { useTranslation } from 'react-i18next';
import { structureDiagnosticTone } from './structureDiagnosticPresentation';
import { resolveStructureDiagnosticIssueTitleKey } from './structurePausedDiagnosticCopy';
import type { StructureDiagnosticSeverityContext } from './structureDiagnosticSeverity';
import { StripOperationalDiagnosticMessage } from './structureOperationalStripUi';

export function StructureOperationalIssueBanner({
  codes,
  diagnosticContext,
}: {
  codes: string[];
  diagnosticContext?: StructureDiagnosticSeverityContext;
}) {
  const { t } = useTranslation('structure');

  if (codes.length === 0) {
    return null;
  }

  const stageStatus = diagnosticContext?.stageStatus ?? null;

  return (
    <div className="structure-operational-strip__message-stack">
      {codes.map((code) => {
        const tone = structureDiagnosticTone(code, diagnosticContext);
        const titleKey = resolveStructureDiagnosticIssueTitleKey(
          code,
          stageStatus,
        );
        return (
          <StripOperationalDiagnosticMessage key={code} tone={tone}>
            {t(titleKey, {
              defaultValue: code,
            })}
          </StripOperationalDiagnosticMessage>
        );
      })}
    </div>
  );
}
