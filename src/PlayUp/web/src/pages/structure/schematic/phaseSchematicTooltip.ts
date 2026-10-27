// -----------------------------------------------------------------------
// Structure schematic — case tooltip model (pure).
// Rendering: buildSchematicCaseTooltip in phaseSchematic.tsx
// -----------------------------------------------------------------------

import type { SchematicFeedOrigin } from '../../../types';

type Translate = (key: string, opts?: Record<string, unknown>) => string;

export type SchematicCaseTooltipOriginKind = 'from' | 'draw' | 'affectation';

export type SchematicCaseTooltipModel = {
  address: string | null;
  /**
   * Primary tip content — construction path (Qual/Prog) or team (Draw / Direct).
   */
  subject: {
    kind: 'team' | 'label';
    name: string;
    logoMediaId?: string | null;
    primaryColor?: string | null;
  } | null;
  /**
   * Legacy Qual/Prog tip row when case primary was the path (pre-R7). Unused when R7
   * shows the participant on the case and the path in the tip subject.
   */
  resolvedTeam: {
    name: string;
    logoMediaId?: string | null;
    primaryColor?: string | null;
  } | null;
  /** Footer provenance under the separator. */
  origin: {
    kind: SchematicCaseTooltipOriginKind;
    /** Lead copy when kind=from (e.g. "Vient de"). */
    lead?: string | null;
    /** Emphasized span (phase name) or full line for draw / affectation. */
    text: string;
  } | null;
  /** When the place accepts manual placement (click to open dialog). */
  action: string | null;
  /** Team row in tip when forfeit and case primary is a construction label (R7). */
  withdrawnTeamRow: {
    name: string;
    logoMediaId?: string | null;
    primaryColor?: string | null;
  } | null;
  /** Forfeit line under the team name (icon + short label). */
  withdrawnNotice: string | null;
};

/**
 * Case tooltip model — R7: participant on case when resolved; Qual/Prog path in tip.
 */
export function buildSchematicCaseTooltipModel({
  address,
  primary,
  resolvedName,
  constructionPathLabel = null,
  feed,
  crest,
  canManualPlace = false,
  entryWithdrawn = false,
  t,
}: {
  address: string | null;
  primary: string | null;
  resolvedName: string | null;
  /** Qual/Prog feed path label — independent of case primary after R7. */
  constructionPathLabel?: string | null;
  feed?: SchematicFeedOrigin | null;
  crest?: {
    logoMediaId?: string | null;
    primaryColor?: string | null;
  } | null;
  /** True when this place is clickable for manual placement. */
  canManualPlace?: boolean;
  entryWithdrawn?: boolean;
  t: Translate;
}): SchematicCaseTooltipModel | null {
  const teamName = resolvedName?.trim() || null;
  const label = primary?.trim() || null;
  const sourceName = feed?.sourceStageName?.trim() || null;
  const isStructuralFeed =
    feed?.kind === 'Qualification' || feed?.kind === 'Progression';

  let subject: SchematicCaseTooltipModel['subject'] = null;
  let resolvedTeam: SchematicCaseTooltipModel['resolvedTeam'] = null;

  const pathLabel = constructionPathLabel?.trim() || null;

  if (isStructuralFeed) {
    if (teamName && pathLabel) {
      // R7 — case shows team; tip carries construction path (+ from-phase footer).
      subject = { kind: 'label', name: pathLabel };
    } else if (pathLabel) {
      subject = { kind: 'label', name: pathLabel };
    } else if (label) {
      subject = { kind: 'label', name: label };
    }
  } else if (teamName) {
    subject = {
      kind: 'team',
      name: teamName,
      ...(crest
        ? {
            logoMediaId: crest.logoMediaId,
            primaryColor: crest.primaryColor,
          }
        : {}),
    };
  } else if (label) {
    subject = { kind: 'label', name: label };
  }

  let origin: SchematicCaseTooltipModel['origin'] = null;
  if (isStructuralFeed) {
    if (sourceName) {
      origin = {
        kind: 'from',
        lead: t('structure:fiche.schematicTooltipFrom'),
        text: sourceName,
      };
    }
  } else if (feed?.kind === 'Draw') {
    origin = {
      kind: 'draw',
      text: t('structure:fiche.schematicTooltipByDraw'),
    };
  } else if (canManualPlace) {
    // One provenance line with PersonIcon — filled vs empty copy.
    origin = {
      kind: 'affectation',
      text: teamName
        ? t('structure:fiche.schematicTooltipManualPlaceEdit')
        : t('structure:fiche.schematicTooltipManualPlace'),
    };
  } else if (feed?.kind === 'Direct') {
    origin = {
      kind: 'affectation',
      text: t('structure:fiche.schematicTooltipByAffectation'),
    };
  }

  const withdrawnNotice =
    entryWithdrawn && teamName ? t('teams:withdrawnBadge') : null;
  const withdrawnTeamRow =
    entryWithdrawn && teamName && subject?.kind === 'label'
      ? {
          name: teamName,
          ...(crest
            ? {
                logoMediaId: crest.logoMediaId,
                primaryColor: crest.primaryColor,
              }
            : {}),
        }
      : null;

  if (!subject && !resolvedTeam && !origin && !withdrawnNotice) {
    // Address-only empty chrome — no construction story to tip.
    return null;
  }

  return {
    address: address || null,
    subject,
    resolvedTeam,
    origin,
    action: null,
    withdrawnTeamRow,
    withdrawnNotice,
  };
}
