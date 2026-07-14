import { logApiRequest, sanitizeUrlForLog } from '@/lib/requestLogger';

export type ApiFetchOptions = RequestInit & {
  /** JWT; sets Authorization Bearer without callers assembling the header. */
  token?: string;
  /** Skip request folder logging (rare). */
  skipLog?: boolean;
};

function resolveUrl(pathOrUrl: string): string {
  if (/^https?:\/\//i.test(pathOrUrl)) return pathOrUrl;
  const base = (process.env.NEXT_PUBLIC_API_URL ?? '').replace(/\/$/, '');
  const path = pathOrUrl.startsWith('/') ? pathOrUrl : `/${pathOrUrl}`;
  return `${base}${path}`;
}

/**
 * Shared fetch wrapper for backend API calls.
 * Logs method, sanitized path, status, and duration only — never bodies or secrets.
 */
export async function apiFetch(
  pathOrUrl: string,
  options: ApiFetchOptions = {}
): Promise<Response> {
  const { token, skipLog, headers: initHeaders, ...rest } = options;
  const url = resolveUrl(pathOrUrl);
  const headers = new Headers(initHeaders);

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  // Let the browser set multipart boundary for FormData bodies.
  if (rest.body instanceof FormData) {
    headers.delete('Content-Type');
  }

  const started = typeof performance !== 'undefined' ? performance.now() : Date.now();
  let status = 0;

  try {
    const res = await fetch(url, { ...rest, headers });
    status = res.status;
    return res;
  } catch (err) {
    status = 0;
    throw err;
  } finally {
    if (!skipLog) {
      const ended = typeof performance !== 'undefined' ? performance.now() : Date.now();
      logApiRequest({
        method: (rest.method ?? 'GET').toUpperCase(),
        path: sanitizeUrlForLog(url),
        status,
        durationMs: ended - started,
      });
    }
  }
}
