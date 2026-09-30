/**
 * Shared HTTP helpers for Host JSON commands/reads.
 * One fetch path — not a generic API layer.
 */

export class ApiError extends Error {
  readonly status: number;
  readonly detail?: string;
  /** ProblemDetails extensions.code when present. */
  readonly code?: string;

  constructor(status: number, message: string, detail?: string, code?: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.detail = detail;
    this.code = code;
  }
}

/**
 * Shared failure path for GET and POST.
 * Prefer ProblemDetails.extensions.code for SPA i18n; keep detail as diagnostic fallback.
 */
export async function throwIfNotOk(response: Response): Promise<void> {
  if (response.ok) {
    return;
  }

  let detail: string | undefined;
  let code: string | undefined;
  try {
    const problem = (await response.json()) as {
      title?: string;
      detail?: string;
      code?: string;
    };
    detail = problem.detail ?? problem.title;
    code = typeof problem.code === 'string' ? problem.code : undefined;
  } catch {
    // Non-JSON body (rare)
  }

  throw new ApiError(
    response.status,
    detail ?? `HTTP ${response.status}`,
    detail,
    code,
  );
}

/**
 * Shared JSON GET helper.
 * Problem: four read endpoints would otherwise copy the same !ok / ProblemDetails parsing.
 * Not a generic “API layer” — just one fetch path with typed return.
 */
export async function getJson<T>(url: string): Promise<T> {
  const response = await fetch(url);
  await throwIfNotOk(response);
  return (await response.json()) as T;
}

/**
 * POST/PUT helpers for Host commands that return a JSON body (Structure mutations).
 */
export async function sendJson<T>(
  method: 'POST' | 'PUT' | 'DELETE',
  url: string,
  body?: unknown,
): Promise<T> {
  const response = await fetch(url, {
    method,
    headers:
      body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  await throwIfNotOk(response);
  return (await response.json()) as T;
}

/**
 * POST/PUT for Host commands that return 204 No Content.
 * Do not call response.json() — an empty body is not JSON.
 */
export async function sendNoContent(
  method: 'POST' | 'PUT' | 'DELETE',
  url: string,
  body?: unknown,
): Promise<void> {
  const response = await fetch(url, {
    method,
    headers:
      body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  await throwIfNotOk(response);
}

export async function postNoContent(
  url: string,
  body?: unknown,
): Promise<void> {
  return sendNoContent('POST', url, body);
}
