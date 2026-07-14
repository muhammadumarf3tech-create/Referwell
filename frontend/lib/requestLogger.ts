/** Safe request metadata only — never passwords, tokens, PHI, or bodies. */
export type RequestLogEntry = {
  method: string;
  path: string;
  status: number;
  durationMs: number;
};

const SENSITIVE_QUERY_KEYS = new Set([
  'access_token',
  'token',
  'password',
  'authorization',
  'api_key',
  'apikey',
  'secret',
]);

/** Strip credential-bearing query params before anything is logged. */
export function sanitizeUrlForLog(url: string): string {
  try {
    const base = typeof window !== 'undefined' ? window.location.origin : 'http://localhost';
    const u = new URL(url, base);
    const apiBase = process.env.NEXT_PUBLIC_API_URL;
    let path = u.pathname + u.search;
    if (apiBase) {
      try {
        const api = new URL(apiBase);
        if (u.origin === api.origin) {
          path = u.pathname + u.search;
        }
      } catch {
        /* keep path */
      }
    }

    if (!u.search) return path;

    const params = new URLSearchParams(u.search);
    const cleaned = new URLSearchParams();
    params.forEach((value, key) => {
      cleaned.set(key, SENSITIVE_QUERY_KEYS.has(key.toLowerCase()) ? '[REDACTED]' : value);
    });
    const qs = cleaned.toString();
    return qs ? `${u.pathname}?${qs}` : u.pathname;
  } catch {
    return url.split('?')[0] || url;
  }
}

/**
 * Persist a redacted request line under frontend/logs via the Next.js route.
 * Uses native fetch (not apiFetch) to avoid recursion.
 */
export function logApiRequest(entry: RequestLogEntry): void {
  if (typeof window === 'undefined') return;

  const payload: RequestLogEntry = {
    method: entry.method.toUpperCase(),
    path: sanitizeUrlForLog(entry.path),
    status: Number.isFinite(entry.status) ? entry.status : 0,
    durationMs: Math.max(0, Math.round(entry.durationMs)),
  };

  if (process.env.NODE_ENV === 'development') {
    // eslint-disable-next-line no-console
    console.debug(
      `[api] ${payload.method} ${payload.path} → ${payload.status} (${payload.durationMs}ms)`
    );
  }

  void fetch('/api/request-log', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    keepalive: true,
  }).catch(() => {
    /* never block UI on logging failures */
  });
}
