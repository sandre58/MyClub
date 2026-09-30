/**
 * Media upload (multipart).
 */
import { throwIfNotOk } from './http';

/** POST /media — multipart file upload → Media metadata. */
export async function uploadMedia(file: File): Promise<{ id: string }> {
  const form = new FormData();
  form.append('file', file);
  const response = await fetch('/media', { method: 'POST', body: form });
  await throwIfNotOk(response);
  return (await response.json()) as { id: string };
}
