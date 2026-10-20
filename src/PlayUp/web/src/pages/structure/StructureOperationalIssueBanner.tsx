import { useTranslation } from 'react-i18next';
import { structureDiagnosticTone } from './structureDiagnosticPresentation';
import {
  resolveStructureDiagnosticIssueDescriptionKey,
  resolveStructureDiagnosticIssueTitleKey,
} from './structurePausedDiagnosticCopy';
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
        const copyContext = {
          compositionEntryCount: diagnosticContext?.compositionEntryCount,
          compositionCapacity: diagnosticContext?.compositionCapacity,
        };
        const titleKey = resolveStructureDiagnosticIssueTitleKey(
          code,
          stageStatus,
          copyContext,
        );
        const descriptionKey = resolveStructureDiagnosticIssueDescriptionKey(
          code,
          stageStatus,
          copyContext,
        );
        return (
          <StripOperationalDiagnosticMessage key={code} tone={tone}>
            <span className="structure-operational-strip__diagnostic-copy">
              <span>{t(titleKey, { defaultValue: code })}</span>
              {descriptionKey ? (
                <span
                  className="structure-operational-strip__diagnostic-description"
                >
                  {t(descriptionKey)}
                </span>
              ) : null}
            </span>
          </StripOperationalDiagnosticMessage>
        );
      })}
    </div>
  );
}
