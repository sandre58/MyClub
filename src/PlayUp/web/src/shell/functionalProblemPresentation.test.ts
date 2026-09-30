import { describe, expect, it } from 'vitest';
import {
  functionalProblemPresentation,
  situationPresentation,
  structureIssuePresentation,
} from './functionalProblemPresentation';

describe('functionalProblemPresentation', () => {
  it('maps StructureGraphInvalid to Structure SoT + ConfigureStructure CTA', () => {
    const presentation = functionalProblemPresentation({
      source: 'StructureGraphInvalid',
      competitionId: 'comp-1',
      targetType: 'Structure',
      targetId: 'comp-1',
    });

    expect(presentation.canal).toBe('overview-echo');
    expect(presentation.tone).toBe('danger');
    expect(presentation.sotHref).toBe('/competitions/comp-1/structure');
    expect(presentation.cta?.actionCode).toBe('ConfigureStructure');
    expect(presentation.titleKey).toBe('attentionSource.StructureGraphInvalid');
    expect(presentation.titleNs).toBe('enums');
  });

  it('maps InsufficientParticipants to Teams SoT (not Structure)', () => {
    const presentation = functionalProblemPresentation({
      source: 'InsufficientParticipants',
      competitionId: 'comp-1',
      params: { activeCount: '2', minimumTeams: '8' },
    });

    expect(presentation.canal).toBe('teams-plateau');
    expect(presentation.sotHref).toBe('/competitions/comp-1/teams');
    expect(presentation.cta?.actionCode).toBe('AddEntry');
  });

  it('maps construction blockers to Structure PageHead canal', () => {
    const presentation = functionalProblemPresentation({
      source: 'MissingStructure',
      competitionId: 'comp-1',
    });

    expect(presentation.canal).toBe('structure-pagehead');
    expect(presentation.tone).toBe('attention');
    expect(presentation.sotHref).toBe('/competitions/comp-1/structure');
  });

  it('maps draw create blocks never-danger (info|warning)', () => {
    expect(
      functionalProblemPresentation({
        source: 'emptyPool',
        competitionId: 'comp-1',
      }).tone,
    ).toBe('info');
    expect(
      functionalProblemPresentation({
        source: 'missingPots',
        competitionId: 'comp-1',
      }).tone,
    ).toBe('warning');
  });

  it('maps structureIssues children via structureIssuePresentation', () => {
    const presentation = structureIssuePresentation({
      code: 'MissingQualificationDestinationSlot',
      competitionId: 'comp-1',
      stageId: 'stage-9',
    });

    expect(presentation.canal).toBe('structure-topology');
    expect(presentation.titleKey).toBe(
      'graph.issues.MissingQualificationDestinationSlot',
    );
    expect(presentation.titleNs).toBe('structure');
    expect(presentation.sotHref).toContain('stage=stage-9');
  });

  it('situationPresentation forwards Overview actionCode', () => {
    const presentation = situationPresentation(
      {
        source: 'StructureGraphInvalid',
        nature: 'Blocking',
        targetType: 'Structure',
        targetId: 'comp-1',
        matchId: null,
        actionable: true,
        actionCode: 'ConfigureStructure',
        impactCode: 'BlocksConstruction',
        params: {},
      },
      'comp-1',
    );

    expect(presentation.cta?.actionCode).toBe('ConfigureStructure');
    expect(presentation.sotHref).toBe('/competitions/comp-1/structure');
  });
});
