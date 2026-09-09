/**
 * Soft guard for standing barème — Domain allows any ints; UX warns when
 * win < draw or draw < loss (min each is 0, enforced by InputNumber).
 * Caller supplies the localized message (i18n).
 */
export function pointsBaremeWarning(
  win: number | null,
  draw: number | null,
  loss: number | null,
  message: string,
): string | undefined {
  if (win == null || draw == null || loss == null) {
    return undefined;
  }
  if (win < draw || draw < loss) {
    return message;
  }
  return undefined;
}
