// -----------------------------------------------------------------------
// Shared Place D1 mapping primitives (Qual + Prog).
// Contract: Expand[i] ↔ destinationSlotKeys[i]; Remplir is authoring aid only.
// Domain-specific Expand / incomplete rules stay in each draft module.
// -----------------------------------------------------------------------

/** Prefer destinationSlotKeys; coerce legacy singular to a one-element list. */
export function coerceDestinationSlotKeys(
  keys?: string[] | null,
  singular?: string | null,
): string[] {
  if (keys != null && keys.length > 0) {
    return keys.map((k) => (typeof k === 'string' ? k.trim() : ''));
  }
  const one = singular?.trim();
  return one ? [one] : [];
}

/**
 * Resize slot keys to `count`, keeping existing keys by index where possible
 * and padding with empty strings (or truncating extras).
 */
export function resizeDestinationSlotKeys(
  keys: string[],
  count: number,
): string[] {
  if (count <= 0) return [];
  if (keys.length === count) return keys;
  if (keys.length > count) return keys.slice(0, count);
  const next = keys.slice();
  while (next.length < count) next.push('');
  return next;
}

/**
 * SPA authoring aid: fill empty Place slots from `availablePlaceIds` in view
 * order, skipping ids already used by filled rows. Never overwrites manual picks.
 */
export function fillEmptyPlaceSlotKeys(
  keys: string[],
  availablePlaceIds: string[],
): string[] {
  const used = new Set(
    keys.map((k) => k.trim()).filter((k) => k.length > 0),
  );
  const pool = availablePlaceIds
    .map((id) => id.trim())
    .filter((id) => id.length > 0 && !used.has(id));
  let poolIndex = 0;
  return keys.map((raw) => {
    if (raw.trim()) return raw;
    if (poolIndex >= pool.length) return '';
    return pool[poolIndex++];
  });
}

export function hasDuplicateSlotKeys(keys: string[]): boolean {
  const seen = new Set<string>();
  for (const raw of keys) {
    const k = raw.trim();
    if (!k) continue;
    if (seen.has(k)) return true;
    seen.add(k);
  }
  return false;
}

/**
 * D1 completeness for a Place mapping of size `expandCount`.
 * Returns MultiSlot | DuplicateSlot | null (complete).
 */
export function placeMappingGap(
  keys: string[],
  expandCount: number,
): 'MultiSlot' | 'DuplicateSlot' | null {
  if (expandCount <= 0) return 'MultiSlot';
  const aligned = resizeDestinationSlotKeys(keys, expandCount);
  if (
    aligned.length !== expandCount ||
    aligned.some((k) => !k.trim())
  ) {
    return 'MultiSlot';
  }
  if (hasDuplicateSlotKeys(aligned)) {
    return 'DuplicateSlot';
  }
  return null;
}

/** Count empty slots in Expand-aligned keys. */
export function countEmptyPlaceSlots(
  keys: string[],
  expandCount: number,
): number {
  if (expandCount <= 0) return 0;
  const aligned = resizeDestinationSlotKeys(keys, expandCount);
  let n = 0;
  for (const key of aligned) {
    if (!key.trim()) n += 1;
  }
  return n;
}
