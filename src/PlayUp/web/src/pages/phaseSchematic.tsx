import type { CSSProperties, ReactElement, ReactNode } from 'react';
import { useLayoutEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Trophy } from 'lucide-react';
import type {
  SchematicCase,
  SchematicConnection,
  SchematicFeedOrigin,
  SelectionMode,
  StageSchematic,
} from '../types';
import { TeamCrest } from '../design-system/TeamCrest';
import { Tooltip } from '../design-system/components/Tooltip';
import {
  ArrowRightIcon,
  DrawPendingIcon,
  PersonIcon,
} from '../design-system/icons/contentIcons';
import { nextPowerOfTwo } from './structureFixtureLabels';
import { placeChromeLabel } from './structurePlaceLabel';
import { resolveManualPlaceMode } from './manualPlacementUi';
import './phase-schematic.css';

type Translate = (key: string, opts?: Record<string, unknown>) => string;
type SchematicDensity = 'full' | 'crest' | 'compact';

type PairLabel =
  | { kind: 'fixture'; matchNumber: number }
  | { kind: 'pair'; pairKey: string };

/**
 * Phase form schematic driven by StageSchematic DTO.
 * One shared slot design (SlotBox) across formats; only the layout is format-driven.
 * `terminal` = the phase has no outgoing progression (trophy allowed on Cup).
 */
export function PhaseSchematic({
  schematic,
  terminal = false,
  /** Stage hub round count — used when schematic.cupRoundCount is absent (older Host). */
  cupRoundCount,
  /** Cup Placement manuel — click a place to open the shared dialog. */
  onPlaceActivate,
}: {
  schematic: StageSchematic;
  terminal?: boolean;
  cupRoundCount?: number | null;
  onPlaceActivate?: (place: SchematicCase) => void;
}): ReactElement {
  const { t } = useTranslation(['regulation', 'structure']);
  const format = schematic.formatKind;

  if (format === 'Groups') {
    return <GroupsSchematic schematic={schematic} t={t} />;
  }

  if (format === 'Cup') {
    return (
      <CupSchematic
        schematic={schematic}
        t={t}
        terminal={terminal}
        roundHint={cupRoundCount}
        onPlaceActivate={onPlaceActivate}
      />
    );
  }

  if (format === 'Championship') {
    return <ChampionshipSchematic schematic={schematic} t={t} />;
  }

  if (format === 'Swiss') {
    return <SwissSchematic schematic={schematic} t={t} />;
  }

  return (
    <div
      className="regulation-schematic"
      aria-label={t('regulation:schematic.championship', { teams: 0 })}
    />
  );
}

/** Shared slot design: chrome (C2 SlotKey) + S3 primary; team only without feed (B1). */
function SlotBox({
  c,
  t,
  ghost,
  density = 'full',
  style,
  onActivate,
}: {
  c?: SchematicCase | null;
  t: Translate;
  ghost?: boolean;
  density?: SchematicDensity;
  style?: CSSProperties;
  onActivate?: (place: SchematicCase) => void;
}) {
  const address = c ? placeChromeLabel(c.formPosition) : null;
  const { primary, secondary } = c
    ? structureCaseLabels(c, t)
    : { primary: null, secondary: null };
  const resolvedName =
    c?.assignment?.displayName?.trim() ||
    c?.assignment?.shortName?.trim() ||
    c?.entry?.displayName?.trim() ||
    null;
  // Solid chrome when the case shows a label (occupant or structural WhoFeeds).
  // Dashed `--empty` = blank cell only — not "no EntryId".
  const hasSurface = !!(primary || secondary);
  const canManualPlace =
    !!onActivate &&
    !!c &&
    !ghost &&
    resolveManualPlaceMode(c) === 'editable';
  const interactive = canManualPlace;
  const className = [
    'schematic-slot',
    `schematic-slot--${density}`,
    hasSurface ? '' : 'schematic-slot--empty',
    ghost ? 'schematic-slot--ghost' : '',
    interactive ? 'schematic-slot--interactive' : '',
  ]
    .filter(Boolean)
    .join(' ');

  const tip =
    !ghost && c
      ? buildSchematicCaseTooltip({
          address,
          primary,
          resolvedName,
          feed: c.feedOrigin,
          crest: c.assignment
            ? {
                logoMediaId: c.assignment.logoMediaId,
                primaryColor: c.assignment.primaryColor,
              }
            : null,
          canManualPlace,
          t,
        })
      : null;

  // Cup absolute hosts need the slot to fill the tip wrapper; flow formats
  // (Champ width:100%, Groups) must keep CSS min-height: 2rem.
  const cupAbsolute = style?.position === 'absolute';
  const fillStyle: CSSProperties | undefined = cupAbsolute
    ? { width: '100%', height: '100%', minHeight: 0 }
    : undefined;

  // Crest only with a real team name — never beside a Qual/Prog path label.
  const showTeamCrest =
    !!resolvedName && !!primary && primary === resolvedName;
  const showSlotCrest = density === 'full' && showTeamCrest;
  const slotInner = (
    <>
      {density === 'crest' && showTeamCrest ? (
        <TeamCrest
          name={resolvedName!}
          logoMediaId={c?.assignment?.logoMediaId}
          primaryColor={c?.assignment?.primaryColor}
          size="sm"
        />
      ) : null}
      {density === 'full' && address ? (
        <span className="schematic-slot__address">{address}</span>
      ) : null}
      {density === 'full' && (primary || showSlotCrest) ? (
        <span className="schematic-slot__body">
          {showSlotCrest ? (
            <TeamCrest
              name={resolvedName!}
              logoMediaId={c?.assignment?.logoMediaId}
              primaryColor={c?.assignment?.primaryColor}
              size="sm"
              className="schematic-slot__crest"
            />
          ) : null}
          <span className="schematic-slot__copy">
            {primary ? (
              <span className="schematic-slot__primary">{primary}</span>
            ) : null}
            {secondary && secondary !== primary ? (
              <span className="schematic-slot__secondary">{secondary}</span>
            ) : null}
          </span>
        </span>
      ) : null}
    </>
  );

  const slot = interactive ? (
    <button
      type="button"
      className={className}
      style={tip ? fillStyle : style}
      onClick={() => onActivate?.(c!)}
      aria-label={address ?? c!.formPosition.slotKey ?? undefined}
    >
      {slotInner}
    </button>
  ) : (
    <span
      className={className}
      style={tip ? fillStyle : style}
      aria-hidden={ghost || undefined}
    >
      {slotInner}
    </span>
  );

  if (!tip) {
    return slot;
  }

  // Geometry host OUTSIDE the DS Tooltip trigger — otherwise Cup absolute
  // top/height land on an inner node and slots collapse / drift vs wires.
  return (
    <span className="schematic-slot-tip" style={style}>
      <Tooltip content={tip} side="top">
        {slot}
      </Tooltip>
    </span>
  );
}

export type SchematicCaseTooltipOriginKind =
  | 'from'
  | 'draw'
  | 'affectation';

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
  /** When the place accepts Placement manuel (click to open dialog). */
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
  /** True when this place is clickable for Placement manuel. */
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

/** Structured DS Tooltip body for a schematic case (all formats). */
export function buildSchematicCaseTooltip(
  args: Parameters<typeof buildSchematicCaseTooltipModel>[0],
): ReactNode {
  const model = buildSchematicCaseTooltipModel(args);
  if (!model) return null;

  const OriginIcon =
    model.origin?.kind === 'draw'
      ? DrawPendingIcon
      : model.origin?.kind === 'affectation'
        ? PersonIcon
        : ArrowRightIcon;

  return (
    <div className="schematic-case-tip">
          {model.address ? (
            <p className="schematic-case-tip__address">{model.address}</p>
          ) : null}
          {model.subject ? (
        <div className="schematic-case-tip__subject">
          {model.subject.kind === 'team' ? (
            <TeamCrest
              name={model.subject.name}
              logoMediaId={model.subject.logoMediaId}
              primaryColor={model.subject.primaryColor}
              size="sm"
              className="schematic-case-tip__crest"
            />
          ) : null}
          <p
            className={
              model.subject.kind === 'label'
                ? 'schematic-case-tip__primary'
                : 'schematic-case-tip__subject-name'
            }
          >
            {model.subject.name}
          </p>
        </div>
      ) : null}
      {model.resolvedTeam ? (
        <div className="schematic-case-tip__resolved">
          <TeamCrest
            name={model.resolvedTeam.name}
            logoMediaId={model.resolvedTeam.logoMediaId}
            primaryColor={model.resolvedTeam.primaryColor}
            size="sm"
            className="schematic-case-tip__crest"
          />
          <p className="schematic-case-tip__resolved-name">
            {model.resolvedTeam.name}
          </p>
        </div>
      ) : null}
      {model.origin ? (
        <>
          <div className="schematic-case-tip__rule" aria-hidden="true" />
          <p className="schematic-case-tip__origin">
            <OriginIcon
              size="sm"
              className="schematic-case-tip__origin-icon"
              aria-hidden
            />
            {model.origin.kind === 'from' && model.origin.lead ? (
              <span className="schematic-case-tip__origin-copy">
                <span className="schematic-case-tip__origin-lead">
                  {model.origin.lead}
                </span>{' '}
                <span className="schematic-case-tip__origin-emphasis">
                  {model.origin.text}
                </span>
              </span>
            ) : (
              <span className="schematic-case-tip__origin-copy">
                {model.origin.text}
              </span>
            )}
          </p>
        </>
      ) : null}
      {model.action ? (
        <>
          {model.subject || model.resolvedTeam || model.origin ? (
            <div className="schematic-case-tip__rule" aria-hidden="true" />
          ) : null}
          <p className="schematic-case-tip__action">{model.action}</p>
        </>
      ) : null}
    </div>
  );
}

function GroupsSchematic({
  schematic,
  t,
}: {
  schematic: StageSchematic;
  t: Translate;
}) {
  const byGroup = new Map<string, SchematicCase[]>();
  for (const c of schematic.cases) {
    const id = c.formPosition.groupId ?? '';
    const list = byGroup.get(id) ?? [];
    list.push(c);
    byGroup.set(id, list);
  }

  const groups = [...byGroup.entries()].map(([id, cases]) => ({
    id,
    name: cases[0]?.formPosition.groupName ?? id,
    cases: [...cases].sort(
      (a, b) => (a.formPosition.index ?? 0) - (b.formPosition.index ?? 0),
    ),
  }));

  return (
    <div
      className="regulation-schematic regulation-schematic--groups"
      aria-label={t('regulation:schematic.groups', { count: groups.length })}
    >
      <div className="regulation-schematic__cards">
        {groups.map((group, i) => (
          <div
            key={group.id || i}
            className={`regulation-schematic__card regulation-schematic__card--${i % 4}`}
          >
            <span className="regulation-schematic__card-label">
              {t('structure:place.group', { name: group.name })}
            </span>
            <div className="regulation-schematic__card-slots">
              {group.cases.map((c, j) => (
                <SlotBox key={`${group.id}-${j}`} c={c} t={t} />
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

/**
 * Championship = participant pool (same as Swiss on ranks): index is roster
 * order only, not standing — do not render 1…N as league ranks.
 */
function ChampionshipSchematic({
  schematic,
  t,
}: {
  schematic: StageSchematic;
  t: Translate;
}) {
  const rows = [...schematic.cases].sort(
    (a, b) => (a.formPosition.index ?? 0) - (b.formPosition.index ?? 0),
  );
  // Cases = ExpectedFormParticipants bag projected into N cells (no alimentation zone).
  return (
    <div
      className="regulation-schematic regulation-schematic--championship"
      aria-label={t('regulation:schematic.championship', {
        teams: rows.length,
      })}
    >
      <div className="regulation-schematic__league">
        {rows.map((c, i) => (
          <SlotBox
            key={c.formPosition.index ?? i}
            c={c}
            t={t}
            style={{ width: '100%' }}
          />
        ))}
      </div>
    </div>
  );
}

/**
 * Swiss ≠ Championship: places are a participant pool (no structural ranks)
 * and the planned round count K is the format's structural signature.
 */
function SwissSchematic({
  schematic,
  t,
}: {
  schematic: StageSchematic;
  t: Translate;
}) {
  const rounds = schematic.swissRoundCount ?? null;
  // Cases = ExpectedFormParticipants bag projected into N cells (no alimentation zone).
  return (
    <div
      className="regulation-schematic regulation-schematic--swiss"
      aria-label={
        rounds
          ? t('regulation:schematic.swiss', { rounds })
          : t('regulation:schematic.swissGeneric')
      }
    >
      <p className="regulation-schematic__swiss-caption">
        {rounds
          ? t('regulation:schematic.swiss', { rounds })
          : t('regulation:schematic.swissGeneric')}
      </p>
      <div className="regulation-schematic__swiss-places">
        {schematic.cases.map((c, i) => (
          <SlotBox key={c.formPosition.index ?? i} c={c} t={t} />
        ))}
      </div>
    </div>
  );
}

/* -- Cup: HTML slot columns + SVG bracket wires, shared pixel geometry. -- */

type CupMetrics = {
  slotH: number;
  pairGap: number;
  groupGap: number;
  colGap: number;
  slotColW: number;
};

function cupMetrics(density: SchematicDensity): CupMetrics {
  if (density === 'compact') {
    return { slotH: 28, pairGap: 4, groupGap: 10, colGap: 52, slotColW: 88 };
  }
  if (density === 'crest') {
    return { slotH: 34, pairGap: 5, groupGap: 12, colGap: 64, slotColW: 112 };
  }
  return { slotH: 40, pairGap: 6, groupGap: 16, colGap: 76, slotColW: 148 };
}

const CUP_MIN_FIT_SCALE = 0.72;

function CupSchematic({
  schematic,
  t,
  terminal,
  roundHint,
  onPlaceActivate,
}: {
  schematic: StageSchematic;
  t: Translate;
  terminal: boolean;
  roundHint?: number | null;
  onPlaceActivate?: (place: SchematicCase) => void;
}) {
  const roundCount = resolveCupRoundCount(
    schematic.cupRoundCount,
    roundHint,
    schematic.connections,
    schematic.cases.length,
  );

  const multiColumns = partitionCupRoundCases(schematic.cases, roundCount);
  const isMulti = multiColumns != null;
  const multiRoundCount = multiColumns?.length ?? 0;
  const multiLeafCount = multiColumns?.[0]?.length ?? 0;
  const [density, setDensity] = useState<SchematicDensity>('full');
  const [scale, setScale] = useState(1);
  const [scroll, setScroll] = useState(false);
  const viewportRef = useRef<HTMLDivElement>(null);

  const metrics = cupMetrics(density);
  const naturalSize = isMulti
    ? measureCupMultiSize(multiRoundCount, multiLeafCount, metrics, terminal)
    : measureCupSingleSize(
        nextPowerOfTwo(Math.max(schematic.cases.length, 2)),
        Math.max(roundCount, 1),
        metrics,
        terminal,
      );

  useLayoutEffect(() => {
    const el = viewportRef.current;
    if (!el) return;

    const apply = () => {
      const avail = el.clientWidth;
      // jsdom / first paint: no real width yet — keep full density.
      if (avail < 48) {
        setDensity((prev) => (prev === 'full' ? prev : 'full'));
        setScale((prev) => (prev === 1 ? prev : 1));
        setScroll((prev) => (prev ? false : prev));
        return;
      }
      const leafFallback = nextPowerOfTwo(Math.max(schematic.cases.length, 2));
      const sizes: SchematicDensity[] = ['full', 'crest', 'compact'];
      for (const d of sizes) {
        const m = cupMetrics(d);
        const size = isMulti
          ? measureCupMultiSize(multiRoundCount, multiLeafCount, m, terminal)
          : measureCupSingleSize(leafFallback, Math.max(roundCount, 1), m, terminal);
        if (size.width <= avail + 1) {
          setDensity((prev) => (prev === d ? prev : d));
          setScale((prev) => (prev === 1 ? prev : 1));
          setScroll((prev) => (prev ? false : prev));
          return;
        }
      }

      const compact = cupMetrics('compact');
      const compactSize = isMulti
        ? measureCupMultiSize(multiRoundCount, multiLeafCount, compact, terminal)
        : measureCupSingleSize(
            leafFallback,
            Math.max(roundCount, 1),
            compact,
            terminal,
          );
      const raw = avail / compactSize.width;
      setDensity((prev) => (prev === 'compact' ? prev : 'compact'));
      if (raw >= CUP_MIN_FIT_SCALE) {
        const nextScale = Math.min(1, raw);
        setScale((prev) => (prev === nextScale ? prev : nextScale));
        setScroll((prev) => (prev ? false : prev));
      } else {
        setScale((prev) => (prev === CUP_MIN_FIT_SCALE ? prev : CUP_MIN_FIT_SCALE));
        setScroll((prev) => (prev ? prev : true));
      }
    };

    apply();
    if (typeof ResizeObserver === 'undefined') {
      return;
    }
    const ro = new ResizeObserver(apply);
    ro.observe(el);
    return () => ro.disconnect();
  }, [
    isMulti,
    multiRoundCount,
    multiLeafCount,
    roundCount,
    schematic.cases.length,
    terminal,
  ]);

  const body = multiColumns ? (
    <CupMultiRoundSchematic
      columns={multiColumns}
      connections={schematic.connections}
      t={t}
      terminal={terminal}
      density={density}
      metrics={metrics}
      onPlaceActivate={onPlaceActivate}
    />
  ) : (
    <CupSingleRoundSchematic
      schematic={schematic}
      t={t}
      terminal={terminal}
      wireRounds={roundCount}
      density={density}
      metrics={metrics}
      onPlaceActivate={onPlaceActivate}
    />
  );

  return (
    <div
      ref={viewportRef}
      className={
        scroll
          ? 'schematic-cup-fit schematic-cup-fit--scroll'
          : 'schematic-cup-fit'
      }
    >
      <div
        className="schematic-cup-fit__scale"
        style={{
          width: naturalSize.width,
          height: naturalSize.height,
          transform: scale !== 1 ? `scale(${scale})` : undefined,
          transformOrigin: 'top center',
        }}
      >
        {body}
      </div>
    </div>
  );
}

function measureCupMultiSize(
  roundCount: number,
  leafCount: number,
  m: CupMetrics,
  terminal: boolean,
): { width: number; height: number } {
  const pairCount = Math.max(leafCount / 2, 1);
  const pairSpan = 2 * m.slotH + m.pairGap;
  const height = pairCount * pairSpan + (pairCount - 1) * m.groupGap;
  const lastWireEndX =
    (roundCount - 1) * (m.slotColW + m.colGap) + m.slotColW + m.colGap;
  const trophyW = terminal && leafCount >= 2 ? 20 + 12 : 0;
  return { width: lastWireEndX + trophyW, height };
}

function measureCupSingleSize(
  leafCount: number,
  wireRounds: number,
  m: CupMetrics,
  terminal: boolean,
): { width: number; height: number } {
  const pairCount = leafCount / 2;
  const pairSpan = 2 * m.slotH + m.pairGap;
  const height = pairCount * pairSpan + Math.max(0, pairCount - 1) * m.groupGap;
  const slotsW = m.slotColW;
  const wiresW = wireRounds * m.colGap + 12;
  const trophy = terminal && wireRounds >= Math.log2(leafCount) ? 36 : 0;
  return { width: slotsW + wiresW + trophy, height };
}

/**
 * Prefer authoritative round count (API / structure hub / fixtures).
 * Only infer from slot total when it matches a full multi-round tree
 * (14→3, 6→2) — never treat 8 first-round slots as 3 imaginary rounds.
 */
function resolveCupRoundCount(
  cupRoundCount: number | null | undefined,
  roundHint: number | null | undefined,
  connections: SchematicConnection[],
  slotTotal: number,
): number {
  if (cupRoundCount != null && cupRoundCount >= 1) {
    return cupRoundCount;
  }
  if (roundHint != null && roundHint >= 1) {
    return roundHint;
  }
  const fromConnections = new Set(connections.map((c) => c.roundOrder)).size;
  if (fromConnections >= 1) {
    return fromConnections;
  }
  return inferFullTreeCupRoundCount(slotTotal) ?? 1;
}

/** Full KO tree only: n = 2^(R+1) − 2 ⇒ R = log2(n+2) − 1. */
function inferFullTreeCupRoundCount(slotTotal: number): number | null {
  if (slotTotal < 2) {
    return null;
  }
  const power = Math.log2(slotTotal + 2);
  if (Number.isInteger(power) && power >= 2) {
    return power - 1;
  }
  return null;
}

/**
 * Classic KO: R rounds ⇒ first round has 2^R slots, then half each round
 * (total slots = 2^(R+1) − 2 when every round has slots).
 * Returns null when the case count does not match that structure.
 */
function partitionCupRoundCases(
  cases: SchematicCase[],
  roundCount: number,
): (SchematicCase | null)[][] | null {
  if (roundCount < 2 || cases.length === 0) {
    return null;
  }

  const firstRoundSlots = 2 ** roundCount;
  const allRoundsSlots = 2 * firstRoundSlots - 2;

  if (cases.length === allRoundsSlots) {
    const columns: (SchematicCase | null)[][] = [];
    let offset = 0;
    let size = firstRoundSlots;
    for (let r = 0; r < roundCount; r += 1) {
      columns.push(cases.slice(offset, offset + size));
      offset += size;
      size /= 2;
    }
    return columns;
  }

  if (cases.length === firstRoundSlots) {
    const columns: (SchematicCase | null)[][] = [cases];
    let size = firstRoundSlots / 2;
    for (let r = 1; r < roundCount; r += 1) {
      columns.push(Array.from({ length: size }, () => null));
      size /= 2;
    }
    return columns;
  }

  return null;
}

function buildCupColumnYs(leafCount: number, m: CupMetrics): number[][] {
  const pairCount = leafCount / 2;
  const pairSpan = 2 * m.slotH + m.pairGap;
  const leafYs: number[] = [];
  for (let p = 0; p < pairCount; p += 1) {
    const top = p * (pairSpan + m.groupGap);
    leafYs.push(top + m.slotH / 2);
    leafYs.push(top + m.slotH + m.pairGap + m.slotH / 2);
  }

  const columns: number[][] = [leafYs];
  let prev = leafYs;
  while (prev.length > 1) {
    const next: number[] = [];
    for (let i = 0; i < prev.length / 2; i += 1) {
      next.push((prev[i * 2]! + prev[i * 2 + 1]!) / 2);
    }
    columns.push(next);
    prev = next;
  }
  return columns;
}

/** Domain label from a schematic connection — never invent ordinals. */
function labelFromConnection(
  conn: SchematicConnection | undefined,
): PairLabel | null {
  if (!conn) {
    return null;
  }
  if (conn.fixtureId && conn.matchNumber > 0) {
    return { kind: 'fixture', matchNumber: conn.matchNumber };
  }
  if (conn.pairKey) {
    return { kind: 'pair', pairKey: conn.pairKey };
  }
  return null;
}

/**
 * Read-model structural relation for two leaves — never invent from case order.
 */
function connectionForLeaves(
  roundConns: SchematicConnection[],
  caseA: SchematicCase | null,
  caseB: SchematicCase | null,
): SchematicConnection | undefined {
  const fixtureId = caseA?.formPosition.fixtureId;
  if (fixtureId && fixtureId === caseB?.formPosition.fixtureId) {
    const byFixture = roundConns.find((c) => c.fixtureId === fixtureId);
    if (byFixture) {
      return byFixture;
    }
  }
  const keyA = caseA?.formPosition.slotKey;
  const keyB = caseB?.formPosition.slotKey;
  if (!keyA || !keyB) {
    return undefined;
  }
  return roundConns.find(
    (c) =>
      (c.slotAKey === keyA && c.slotBKey === keyB) ||
      (c.slotAKey === keyB && c.slotBKey === keyA),
  );
}

function PairOrdinalText({
  label,
  x,
  y,
}: {
  label: PairLabel;
  x: number;
  y: number;
}) {
  if (label.kind === 'fixture') {
    return (
      <text x={x} y={y} textAnchor="start" className="schematic-cup__match-n">
        {`#${label.matchNumber}`}
      </text>
    );
  }
  return (
    <text
      x={x}
      y={y}
      textAnchor="start"
      className="schematic-cup__pair-ordinal"
    >
      {label.pairKey}
    </text>
  );
}

function CupMultiRoundSchematic({
  columns,
  connections,
  t,
  terminal,
  density,
  metrics: m,
  onPlaceActivate,
}: {
  columns: (SchematicCase | null)[][];
  connections: SchematicConnection[];
  t: Translate;
  terminal: boolean;
  density: SchematicDensity;
  metrics: CupMetrics;
  onPlaceActivate?: (place: SchematicCase) => void;
}) {
  const leafCount = columns[0]?.length ?? 0;
  if (leafCount < 2 || leafCount % 2 !== 0) {
    return (
      <div
        className="regulation-schematic regulation-schematic--cup"
        aria-label={t('regulation:schematic.bracket')}
      />
    );
  }

  const roundCount = columns.length;
  const pairCount = leafCount / 2;
  const pairSpan = 2 * m.slotH + m.pairGap;
  const height = pairCount * pairSpan + (pairCount - 1) * m.groupGap;
  const columnYs = buildCupColumnYs(leafCount, m);
  const showTrophy = terminal;

  const connectionsByRound = new Map<number, SchematicConnection[]>();
  for (const conn of connections) {
    const list = connectionsByRound.get(conn.roundOrder) ?? [];
    list.push(conn);
    connectionsByRound.set(conn.roundOrder, list);
  }

  const lastCol = roundCount - 1;
  const lastYs = columnYs[lastCol] ?? [];
  const lastWireEndX =
    lastCol * (m.slotColW + m.colGap) + m.slotColW + m.colGap;
  const trophySize = 20;
  const trophyLeft = lastWireEndX + 6;
  const trophyTop =
    lastYs.length >= 2
      ? (lastYs[0]! + lastYs[lastYs.length - 1]!) / 2 - trophySize / 2
      : height / 2 - trophySize / 2;
  const width =
    lastWireEndX + (showTrophy ? trophySize + 12 : 0);

  return (
    <div
      className="regulation-schematic regulation-schematic--cup"
      aria-label={t('regulation:schematic.bracketRounds', { rounds: roundCount })}
    >
      <div className="schematic-cup schematic-cup--multi" style={{ height, width }}>
        {columns.map((colCases, col) => {
          const ys = columnYs[col] ?? [];
          const left = col * (m.slotColW + m.colGap);
          return (
            <div
              key={`col-${col}`}
              className="schematic-cup__round"
              style={{ left, width: m.slotColW, height }}
            >
              {colCases.map((c, i) => {
                const y = ys[i];
                if (y == null) {
                  return null;
                }
                return (
                  <SlotBox
                    key={`c-${col}-${i}`}
                    c={c}
                    t={t}
                    ghost={!c}
                    density={density}
                    onActivate={onPlaceActivate}
                    style={{
                      position: 'absolute',
                      top: y - m.slotH / 2,
                      left: 0,
                      right: 0,
                      height: m.slotH,
                      minHeight: 0,
                      marginBottom: 0,
                    }}
                  />
                );
              })}
            </div>
          );
        })}

        <svg
          className="schematic-cup__wires schematic-cup__wires--overlay"
          width={width}
          height={height}
          viewBox={`0 0 ${width} ${height}`}
          aria-hidden="true"
        >
          {columns.map((colCases, col) => {
            const ys = columnYs[col] ?? [];
            const x0 = col * (m.slotColW + m.colGap) + m.slotColW;
            const x1 = x0 + m.colGap;
            const xMid = x0 + m.colGap / 2;
            const roundConns = connectionsByRound.get(col) ?? [];
            return (
              <g key={`wire-col-${col}`}>
                {Array.from({ length: ys.length / 2 }, (_, pair) => {
                  const caseA = colCases[pair * 2] ?? null;
                  const caseB = colCases[pair * 2 + 1] ?? null;
                  const conn = connectionForLeaves(roundConns, caseA, caseB);
                  if (!conn) {
                    return null;
                  }
                  const y1 = ys[pair * 2]!;
                  const y2 = ys[pair * 2 + 1]!;
                  const mid = (y1 + y2) / 2;
                  const label = labelFromConnection(conn);
                  return (
                    <g key={`w-${col}-${pair}`}>
                      <path
                        d={`M ${x0} ${y1} H ${xMid} M ${x0} ${y2} H ${xMid} M ${xMid} ${y1} V ${y2} M ${xMid} ${mid} H ${x1}`}
                        className="schematic-cup__wire-line"
                      />
                      {label ? (
                        <PairOrdinalText label={label} x={xMid + 6} y={mid - 5} />
                      ) : null}
                    </g>
                  );
                })}
              </g>
            );
          })}
        </svg>

        {showTrophy ? (
          <span
            className="schematic-cup__trophy schematic-cup__trophy--absolute"
            style={{
              left: trophyLeft,
              top: trophyTop,
              width: trophySize,
              height: trophySize,
            }}
            aria-hidden="true"
          >
            <Trophy size={trophySize} strokeWidth={1.75} />
          </span>
        ) : null}
      </div>
    </div>
  );
}

function CupSingleRoundSchematic({
  schematic,
  t,
  terminal,
  wireRounds,
  density,
  metrics: m,
  onPlaceActivate,
}: {
  schematic: StageSchematic;
  t: Translate;
  terminal: boolean;
  wireRounds: number;
  density: SchematicDensity;
  metrics: CupMetrics;
  onPlaceActivate?: (place: SchematicCase) => void;
}) {
  const roundOrders = [
    ...new Set(schematic.connections.map((c) => c.roundOrder)),
  ].sort((a, b) => a - b);
  const firstRoundOrder = roundOrders[0] ?? 0;
  const effectiveWires = Math.max(wireRounds, roundOrders.length, 1);

  const cases = orderLeafCases(
    schematic.cases,
    schematic.connections.filter((c) => c.roundOrder === firstRoundOrder),
  );
  const leafCount = nextPowerOfTwo(Math.max(cases.length, 2));
  const pairCount = leafCount / 2;
  const fullDepth = Math.log2(leafCount);
  const showTrophy = terminal && effectiveWires >= fullDepth;

  const pairSpan = 2 * m.slotH + m.pairGap;
  const height = pairCount * pairSpan + (pairCount - 1) * m.groupGap;
  const columnYs = buildCupColumnYs(leafCount, m);
  const leafYs = columnYs[0] ?? [];

  const bySlotPair = new Map<string, SchematicConnection>();
  const byFixture = new Map<string, SchematicConnection>();
  const firstRoundConns = schematic.connections.filter(
    (c) => c.roundOrder === firstRoundOrder,
  );
  for (const conn of firstRoundConns) {
    if (conn.fixtureId) {
      byFixture.set(conn.fixtureId, conn);
    }
    if (conn.slotAKey && conn.slotBKey) {
      bySlotPair.set(`${conn.slotAKey}|${conn.slotBKey}`, conn);
      bySlotPair.set(`${conn.slotBKey}|${conn.slotAKey}`, conn);
    }
  }

  const pairConnection = (p: number): SchematicConnection | undefined => {
    const a = cases[p * 2];
    const b = cases[p * 2 + 1];
    const fixtureId = a?.formPosition.fixtureId;
    if (fixtureId && fixtureId === b?.formPosition.fixtureId) {
      return byFixture.get(fixtureId);
    }
    const keyA = a?.formPosition.slotKey;
    const keyB = b?.formPosition.slotKey;
    return keyA && keyB ? bySlotPair.get(`${keyA}|${keyB}`) : undefined;
  };

  const svgWidth = effectiveWires * m.colGap + 12;

  return (
    <div
      className="regulation-schematic regulation-schematic--cup"
      aria-label={
        effectiveWires > 1
          ? t('regulation:schematic.bracketRounds', { rounds: effectiveWires })
          : t('regulation:schematic.bracket')
      }
    >
      <div className="schematic-cup">
        <div
          className="schematic-cup__slots"
          style={{ height, width: m.slotColW }}
        >
          {Array.from({ length: leafCount }, (_, i) => {
            const c = cases[i] ?? null;
            const pair = Math.floor(i / 2);
            const inPairGap = i % 2 === 1 ? 0 : m.pairGap;
            const afterPairGap =
              i % 2 === 1 && pair < pairCount - 1 ? m.groupGap : 0;
            return (
              <SlotBox
                key={i}
                c={c}
                t={t}
                ghost={!c && i >= cases.length}
                density={density}
                onActivate={onPlaceActivate}
                style={{
                  height: m.slotH,
                  minHeight: 0,
                  marginBottom: inPairGap + afterPairGap,
                }}
              />
            );
          })}
        </div>
        <svg
          className="schematic-cup__wires"
          width={svgWidth}
          height={height}
          viewBox={`0 0 ${svgWidth} ${height}`}
          aria-hidden="true"
        >
          {/* Wires only when read-model connection exists — never from case adjacency. */}
          {Array.from({ length: pairCount }, (_, p) => {
            const conn = pairConnection(p);
            if (!conn) {
              return null;
            }
            const y1 = leafYs[p * 2]!;
            const y2 = leafYs[p * 2 + 1]!;
            const mid = (y1 + y2) / 2;
            const x = 2;
            const xMid = 2 + m.colGap / 2;
            const xNext = 2 + m.colGap;
            const label = labelFromConnection(conn);
            return (
              <g key={`pair-wire-${p}`}>
                <path
                  d={`M ${x} ${y1} H ${xMid} M ${x} ${y2} H ${xMid} M ${xMid} ${y1} V ${y2} M ${xMid} ${mid} H ${xNext}`}
                  className="schematic-cup__wire-line"
                />
                {label ? (
                  <PairOrdinalText
                    label={label}
                    x={xMid + 8}
                    y={mid - 5}
                  />
                ) : null}
              </g>
            );
          })}
        </svg>
        {showTrophy ? (
          <span className="schematic-cup__trophy" aria-hidden="true">
            <Trophy size={20} strokeWidth={1.75} />
          </span>
        ) : null}
      </div>
    </div>
  );
}

/**
 * Orders bracket leaves by real first-round pairings (match number order),
 * appending unpaired cases in their original order. No pairing is invented.
 */
function orderLeafCases(
  cases: SchematicCase[],
  firstRoundConnections: SchematicConnection[],
): SchematicCase[] {
  if (firstRoundConnections.length === 0) return cases;

  const bySlotKey = new Map<string, SchematicCase>();
  const byFixtureSide = new Map<string, SchematicCase>();
  for (const c of cases) {
    if (c.formPosition.slotKey) bySlotKey.set(c.formPosition.slotKey, c);
    if (c.formPosition.fixtureId && c.formPosition.side) {
      byFixtureSide.set(
        `${c.formPosition.fixtureId}|${c.formPosition.side}`,
        c,
      );
    }
  }

  const ordered: SchematicCase[] = [];
  const used = new Set<SchematicCase>();
  const sorted = [...firstRoundConnections].sort((a, b) => {
    const byMatch = a.matchNumber - b.matchNumber;
    if (byMatch !== 0) {
      return byMatch;
    }
    return (a.pairKey ?? '').localeCompare(b.pairKey ?? '');
  });
  for (const conn of sorted) {
    const a =
      (conn.slotAKey ? bySlotKey.get(conn.slotAKey) : undefined) ??
      (conn.fixtureId
        ? byFixtureSide.get(`${conn.fixtureId}|A`)
        : undefined);
    const b =
      (conn.slotBKey ? bySlotKey.get(conn.slotBKey) : undefined) ??
      (conn.fixtureId
        ? byFixtureSide.get(`${conn.fixtureId}|B`)
        : undefined);
    if (!a || !b || used.has(a) || used.has(b)) continue;
    ordered.push(a, b);
    used.add(a);
    used.add(b);
  }

  if (ordered.length === 0) return cases;
  for (const c of cases) {
    if (!used.has(c)) ordered.push(c);
  }
  return ordered;
}

/**
 * Structure projection (decision A / U4 B1 amended):
 * case = address + construction only (2 lines). Qual/Prog → path primary;
 * resolved team is tip-only. Draw / Direct → occupant is the case primary.
 */
function structureCaseLabels(
  c: SchematicCase,
  t: Translate,
): { primary: string | null; secondary: string | null } {
  const occupant =
    c.assignment?.displayName?.trim() ||
    c.assignment?.shortName?.trim() ||
    c.entry?.displayName?.trim() ||
    null;
  const feed = c.feedOrigin;

  if (!feed) {
    return { primary: occupant, secondary: null };
  }

  // Direct = occupant identity, not a concurrent construction rule.
  if (feed.kind === 'Direct') {
    return { primary: occupant, secondary: null };
  }

  // Draw: Published Slot WhoFeeds on Cup places.
  // Provenance after materialization — occupant primary; tip = "Placé par tirage".
  if (feed.kind === 'Draw') {
    if (occupant) {
      return { primary: occupant, secondary: null };
    }
    return { primary: feedOriginLabel(feed, t), secondary: null };
  }

  // Qualification / Progression — structural construction feeds (U4 B1: no resolved team).
  return {
    primary: feedOriginLabel(feed, t),
    secondary: null,
  };
}

function feedOriginLabel(origin: SchematicFeedOrigin, t: Translate): string {
  switch (origin.kind) {
    case 'Qualification': {
      const mode = (origin.selectionMode ?? 'Position') as SelectionMode;
      const value = origin.selectionValue ?? 1;
      const place =
        value === 1
          ? t('structure:fiche.rule.placeFirst')
          : t('structure:fiche.rule.placeNth', { value });
      if (origin.groupName) {
        return t('structure:fiche.rule.groupPosition', {
          place,
          group: origin.groupName,
        });
      }
      if (origin.rankingScope === 'AcrossGroups') {
        return t('structure:fiche.rule.acrossPosition', { place });
      }
      if (mode === 'Top') {
        return t('structure:fiche.rule.selectionTop', { value });
      }
      return t('structure:fiche.rule.overallPosition', { place });
    }
    case 'Progression': {
      const outcome =
        origin.outcome === 'Loser'
          ? t('structure:fiche.rule.loser', { count: 1 })
          : t('structure:fiche.rule.winner', { count: 1 });
      return origin.sourceFixtureNumber != null
        ? `${outcome} · ${t('structure:fiche.rule.matchNumber', {
            n: origin.sourceFixtureNumber,
          })}`
        : outcome;
    }
    case 'Direct':
      return t('structure:entries.originAffectation');
    case 'Draw':
      return t('structure:fiche.tiles.tirage');
    default:
      return origin.kind;
  }
}
