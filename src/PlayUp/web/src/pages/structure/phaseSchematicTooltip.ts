// -----------------------------------------------------------------------
// Structure schematic — case tooltip model (pure).
// Rendering: buildSchematicCaseTooltip in phaseSchematic.tsx
// -----------------------------------------------------------------------

import type { SchematicFeedOrigin } from '../../types';

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
   * Qual/Prog only: resolved occupant when known (secondary — tip-only, never case).
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
};

/**
 * Case tooltip model — Structure decision A:
 * case = address + construction; tip adds resolution + provenance when useful.
 */
export function buildSchematicCaseTooltipModel({
  address,
  primary,
  resolvedName,
  feed,
  crest,
  canManualPlace = false,
  t,
}: {
  address: string | null;
  primary: string | null;
  resolvedName: string | null;
  feed?: SchematicFeedOrigin | null;
  crest?: {
    logoMediaId?: string | null;
    primaryColor?: string | null;
  } | null;
  /** True when this place is clickable for manual placement. */
  canManualPlace?: boolean;
  t: Translate;
}): SchematicCaseTooltipModel | null {
  const teamName = resolvedName?.trim() || null;
  const label = primary?.trim() || null;
  const sourceName = feed?.sourceStageName?.trim() || null;
  const isStructuralFeed =
    feed?.kind === 'Qualification' || feed?.kind === 'Progression';

  let subject: SchematicCaseTooltipModel['subject'] = null;
  let resolvedTeam: SchematicCaseTooltipModel['resolvedTeam'] = null;

  if (isStructuralFeed) {
    // Path is primary; resolved team is tip-only secondary when distinct.
    if (label) {
      subject = { kind: 'label', name: label };
    }
    if (teamName && teamName !== label) {
      resolvedTeam = {
        name: teamName,
        ...(crest
          ? {
              logoMediaId: crest.logoMediaId,
              primaryColor: crest.primaryColor,
            }
          : {}),
      };
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

  if (!subject && !resolvedTeam && !origin) {
    // Address-only empty chrome — no construction story to tip.
    return null;
  }

  return {
    address: address || null,
    subject,
    resolvedTeam,
    origin,
    action: null,
  };
}
