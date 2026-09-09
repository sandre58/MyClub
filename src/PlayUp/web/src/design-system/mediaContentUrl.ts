/** Relative URL for Media binary content (Vite proxy → Host). */
export function mediaContentUrl(mediaId: string): string {
  return `/media/${mediaId}/content`;
}
