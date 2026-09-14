import type { CSSProperties } from 'react';
import { useTranslation } from 'react-i18next';
import { Trophy } from 'lucide-react';
import type {
  SchematicCase,
  SchematicConnection,
  SchematicFeedOrigin,
  SelectionMode,
  StageSchematic,
} from '../types';
import { nextPowerOfTwo } from './structureFixtureLabels';
import './phase-schematic.css';

type Translate = (key: string, opts?: Record<string, unknown>) => string;

/**
 * Phase form schematic driven by StageSchematic DTO.
 * One shared slot design (SlotBox) across formats; only the layout is format-driven.
 * `terminal` = the phase has no outgoing progression (trophy allowed on Cup).
 */
export function PhaseSchematic({
  schematic,
  terminal = false,
}: {
  schematic: StageSchematic;
  terminal?: boolean;
}) {
  const { t } = useTranslation(['regulation', 'structure']);
  const format = schematic.formatKind;

  if (format === 'Groups') {
    return <GroupsSchematic schematic={schematic} t={t} />;
  }

  if (format === 'Cup') {
    return <CupSchematic schematic={schematic} t={t} terminal={terminal} />;
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

/** Shared slot design: primary line (structural identity) + discreet resolution line. */
function SlotBox({
  c,
  t,
  ghost,
  style,
}: {
  c?: SchematicCase | null;
  t: Translate;
  ghost?: boolean;
  style?: CSSProperties;
}) {
  const primary = c ? casePrimaryLabel(c, t) : null;
  const secondary = c ? caseSecondaryLabel(c) : null;
  const placed = !!c?.entry;
  const className = [
    'schematic-slot',
    placed ? '' : 'schematic-slot--empty',
    ghost ? 'schematic-slot--ghost' : '',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <span className={className} style={style} aria-hidden={ghost || undefined}>
      {primary ? (
        <span className="schematic-slot__primary" title={primary}>
          {primary}
        </span>
      ) : null}
      {secondary && secondary !== primary ? (
        <span className="schematic-slot__secondary" title={secondary}>
          {secondary}
        </span>
      ) : null}
    </span>
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
              {group.name}
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
  return (
    <div
      className="regulation-schematic regulation-schematic--championship"
      aria-label={t('regulation:schematic.championship', {
        teams: rows.length,
      })}
    >
      <div className="regulation-schematic__league">
        {rows.map((c, i) => (
          <span
            key={c.formPosition.index ?? i}
            className="regulation-schematic__league-row"
          >
            <span
              className="regulation-schematic__league-rank"
              aria-hidden="true"
            >
              {c.formPosition.index ?? i + 1}
            </span>
            <SlotBox c={c} t={t} style={{ flex: '1 1 auto' }} />
          </span>
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

/* -- Cup: HTML slot column + SVG bracket wires, shared pixel geometry. -- */

const CUP_SLOT_H = 40;
const CUP_PAIR_GAP = 6;
const CUP_GROUP_GAP = 16;
const CUP_COL_GAP = 76;

function CupSchematic({
  schematic,
  t,
  terminal,
}: {
  schematic: StageSchematic;
  t: Translate;
  terminal: boolean;
}) {
  const roundOrders = [
    ...new Set(schematic.connections.map((c) => c.roundOrder)),
  ].sort((a, b) => a - b);
  const wireRounds = Math.max(roundOrders.length, 1);
  const firstRoundOrder = roundOrders[0] ?? 0;

  // Leaf order follows real first-round pairings (match number order) when known.
  const cases = orderLeafCases(
    schematic.cases,
    schematic.connections.filter((c) => c.roundOrder === firstRoundOrder),
  );
  const leafCount = nextPowerOfTwo(Math.max(cases.length, 2));
  const pairCount = leafCount / 2;
  const fullDepth = Math.log2(leafCount);
  const showTrophy = terminal && wireRounds >= fullDepth;

  const pairSpan = 2 * CUP_SLOT_H + CUP_PAIR_GAP;
  const height = pairCount * pairSpan + (pairCount - 1) * CUP_GROUP_GAP;

  const leafYs: number[] = [];
  for (let p = 0; p < pairCount; p += 1) {
    const top = p * (pairSpan + CUP_GROUP_GAP);
    leafYs.push(top + CUP_SLOT_H / 2);
    leafYs.push(top + CUP_SLOT_H + CUP_PAIR_GAP + CUP_SLOT_H / 2);
  }

  const columns: number[][] = [leafYs];
  for (let col = 1; col <= wireRounds; col += 1) {
    const prev = columns[col - 1]!;
    const next: number[] = [];
    for (let i = 0; i < prev.length / 2; i += 1) {
      next.push((prev[i * 2]! + prev[i * 2 + 1]!) / 2);
    }
    columns.push(next);
  }

  const bySlotPair = new Map<string, SchematicConnection>();
  const byFixture = new Map<string, SchematicConnection>();
  for (const conn of schematic.connections) {
    if (conn.roundOrder !== firstRoundOrder) continue;
    byFixture.set(conn.fixtureId, conn);
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

  const svgWidth = wireRounds * CUP_COL_GAP + 12;

  return (
    <div
      className="regulation-schematic regulation-schematic--cup"
      aria-label={
        wireRounds > 1
          ? t('regulation:schematic.bracketRounds', { rounds: wireRounds })
          : t('regulation:schematic.bracket')
      }
    >
      <div className="schematic-cup">
        <div className="schematic-cup__slots" style={{ height }}>
          {Array.from({ length: leafCount }, (_, i) => {
            const c = cases[i] ?? null;
            const pair = Math.floor(i / 2);
            const inPairGap = i % 2 === 1 ? 0 : CUP_PAIR_GAP;
            const afterPairGap =
              i % 2 === 1 && pair < pairCount - 1 ? CUP_GROUP_GAP : 0;
            return (
              <SlotBox
                key={i}
                c={c}
                t={t}
                ghost={!c && i >= cases.length}
                style={{
                  height: CUP_SLOT_H,
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
          {columns.slice(0, wireRounds).map((ys, col) => {
            const x = col * CUP_COL_GAP + 2;
            const xNext = (col + 1) * CUP_COL_GAP + 2;
            const xMid = x + CUP_COL_GAP / 2;
            return (
              <g key={`col-${col}`}>
                {Array.from({ length: ys.length / 2 }, (_, pair) => {
                  const y1 = ys[pair * 2]!;
                  const y2 = ys[pair * 2 + 1]!;
                  const mid = (y1 + y2) / 2;
                  return (
                    <path
                      key={`w-${col}-${pair}`}
                      d={`M ${x} ${y1} H ${xMid} M ${x} ${y2} H ${xMid} M ${xMid} ${y1} V ${y2} M ${xMid} ${mid} H ${xNext}`}
                      className="schematic-cup__wire-line"
                    />
                  );
                })}
                {col > 0
                  ? ys.map((y, i) => (
                      <circle
                        key={`n-${col}-${i}`}
                        cx={x}
                        cy={y}
                        r={3}
                        className="schematic-cup__wire-node"
                      />
                    ))
                  : null}
              </g>
            );
          })}
          {columns[wireRounds]?.map((y, i) => (
            <circle
              key={`end-${i}`}
              cx={wireRounds * CUP_COL_GAP + 2}
              cy={y}
              r={3}
              className="schematic-cup__wire-node"
            />
          ))}
          {Array.from({ length: pairCount }, (_, p) => {
            const conn = pairConnection(p);
            if (!conn) return null;
            const y1 = leafYs[p * 2]!;
            const y2 = leafYs[p * 2 + 1]!;
            return (
              <text
                key={`match-${p}`}
                x={2 + CUP_COL_GAP / 2 + 8}
                y={(y1 + y2) / 2 - 5}
                textAnchor="start"
                className="schematic-cup__match-n"
              >
                {`#${conn.matchNumber}`}
              </text>
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
  const sorted = [...firstRoundConnections].sort(
    (a, b) => a.matchNumber - b.matchNumber,
  );
  for (const conn of sorted) {
    const a =
      (conn.slotAKey ? bySlotKey.get(conn.slotAKey) : undefined) ??
      byFixtureSide.get(`${conn.fixtureId}|A`);
    const b =
      (conn.slotBKey ? bySlotKey.get(conn.slotBKey) : undefined) ??
      byFixtureSide.get(`${conn.fixtureId}|B`);
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

/** Primary: feed origin when present; else assignment/entry name. */
function casePrimaryLabel(c: SchematicCase, t: Translate): string | null {
  if (c.feedOrigin) {
    return feedOriginLabel(c.feedOrigin, t);
  }
  return (
    c.assignment?.displayName?.trim() || c.entry?.displayName?.trim() || null
  );
}

/** Secondary: resolved participant when feed origin is primary (S3: resolution stays discreet). */
function caseSecondaryLabel(c: SchematicCase): string | null {
  if (!c.feedOrigin) return null;
  return (
    c.assignment?.displayName?.trim() || c.entry?.displayName?.trim() || null
  );
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
          ? t('structure:fiche.rule.loser')
          : t('structure:fiche.rule.winner');
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
