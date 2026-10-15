/**
 * Shared HTTP helpers for Host JSON commands/reads.
 * One fetch path — not a generic API layer.
 */

export class ApiError extends Error {
  readonly status: number;
  readonly detail?: string;
  /** ProblemDetails extensions.code when present. */
  readonly code?: string;
  /** Ready downstream consumers when code is Application.ReadyConsumerWouldBreak. */
  readonly consumerStageIds?: readonly string[];

  constructor(
    status: number,
    message: string,
    detail?: string,
    code?: string,
    consumerStageIds?: readonly string[],
  ) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.detail = detail;
    this.code = code;
    this.consumerStageIds = consumerStageIds;
  }
}

function readStringArray(value: unknown): string[] | undefined {
  if (!Array.isArray(value)) {
    return undefined;
  }
  const ids = value.filter(
    (item): item is string => typeof item === 'string' && item.length > 0,
  );
  return ids.length > 0 ? ids : undefined;
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
  let consumerStageIds: string[] | undefined;
  try {
    const problem = (await response.json()) as {
      title?: string;
      detail?: string;
      code?: string;
      consumerStageIds?: unknown;
      extensions?: { code?: string; consumerStageIds?: unknown };
    };
    detail = problem.detail ?? problem.title;
    code =
      typeof problem.code === 'string'
        ? problem.code
        : typeof problem.extensions?.code === 'string'
          ? problem.extensions.code
          : undefined;
    consumerStageIds =
      readStringArray(problem.consumerStageIds) ??
      readStringArray(problem.extensions?.consumerStageIds);
  } catch {
    // Non-JSON body (rare)
  }

  throw new ApiError(
    response.status,
    detail ?? `HTTP ${response.status}`,
    detail,
    code,
    consumerStageIds,
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
