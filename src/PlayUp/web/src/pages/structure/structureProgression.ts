/** True when the progression destination targets phase Population (no Place). */
export function isPopulationDestination(
  slotKey: string | null | undefined,
): boolean {
  return slotKey == null || slotKey.trim() === '';
}
