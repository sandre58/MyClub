import { useTranslation } from 'react-i18next';

import type { StructureStageHubSummary, StructureView } from '../../types';
import { StructureDiagnosticIssueIcon } from './StructureDiagnosticIssueIcon';
import { structureDiagnosticContextFromStage } from './structureDiagnosticContext';
import { structureDiagnosticTone } from './structureDiagnosticPresentation';
import {
  resolveStructureDiagnosticIssueDescriptionKey,
  resolveStructureDiagnosticIssueTitleKey,
} from './structurePausedDiagnosticCopy';
import { structureDiagnosticIssueCopyFromStage } from './structurePopulationShortfallPresentation';

export function StructurePhaseRailIssueBanner({
  codes,
  data,
  stage,
}: {
  codes: readonly string[];
  data: StructureView;
  stage: StructureStageHubSummary;
}) {
  const { t } = useTranslation('structure');

  if (codes.length === 0) {
    return null;
  }

  const diagnosticContext = structureDiagnosticContextFromStage(data, stage);
  const issueCopyContext = structureDiagnosticIssueCopyFromStage(stage);

  return (
    <ul className="structure-phase-rail__issues" role="list">
      {codes.map((code) => {
        const tone = structureDiagnosticTone(code, diagnosticContext);
        const titleKey = resolveStructureDiagnosticIssueTitleKey(
          code,
          diagnosticContext.stageStatus,
          issueCopyContext,
        );
        const descriptionKey = resolveStructureDiagnosticIssueDescriptionKey(
          code,
          diagnosticContext.stageStatus,
          issueCopyContext,
        );
        return (
          <li
            key={code}
            className="structure-phase-rail__issue"
            data-tone={tone}
            role="status"
          >
            <span
              className="structure-phase-rail__issue-icon"
              aria-hidden="true"
            >
              <StructureDiagnosticIssueIcon tone={tone} />
            </span>
            <span className="structure-phase-rail__issue-copy">
              <span className="structure-phase-rail__issue-title">
                {t(titleKey, {
                  defaultValue: code,
                })}
              </span>
              {descriptionKey ? (
                <span className="structure-phase-rail__issue-description">
                  {t(descriptionKey)}
                </span>
              ) : null}
            </span>
          </li>
        );
      })}
    </ul>
  );
}
