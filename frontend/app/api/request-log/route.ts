import { NextRequest, NextResponse } from 'next/server';
import { appendFile, mkdir } from 'fs/promises';
import path from 'path';

export const runtime = 'nodejs';

type IncomingLog = {
  method?: unknown;
  path?: unknown;
  status?: unknown;
  durationMs?: unknown;
};

const ALLOWED_METHODS = new Set([
  'GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS',
]);

/**
 * Receives sanitized client request metadata and appends to frontend/logs.
 * Rejects unexpected fields / oversized paths; never accepts bodies or tokens.
 */
export async function POST(req: NextRequest) {
  let body: IncomingLog;
  try {
    body = await req.json();
  } catch {
    return NextResponse.json({ error: 'Invalid JSON' }, { status: 400 });
  }

  const method = typeof body.method === 'string' ? body.method.toUpperCase() : '';
  const reqPath = typeof body.path === 'string' ? body.path : '';
  const status = typeof body.status === 'number' ? body.status : Number(body.status);
  const durationMs =
    typeof body.durationMs === 'number' ? body.durationMs : Number(body.durationMs);

  if (!ALLOWED_METHODS.has(method)) {
    return NextResponse.json({ error: 'Invalid method' }, { status: 400 });
  }
  if (!reqPath || reqPath.length > 512 || /[\r\n]/.test(reqPath)) {
    return NextResponse.json({ error: 'Invalid path' }, { status: 400 });
  }
  if (!Number.isFinite(status) || status < 0 || status > 599) {
    return NextResponse.json({ error: 'Invalid status' }, { status: 400 });
  }
  if (!Number.isFinite(durationMs) || durationMs < 0 || durationMs > 600_000) {
    return NextResponse.json({ error: 'Invalid duration' }, { status: 400 });
  }

  // Defence in depth: drop anything that looks like a bearer token or password in the path string.
  const safePath = reqPath
    .replace(/access_token=[^&]*/gi, 'access_token=[REDACTED]')
    .replace(/token=[^&]*/gi, 'token=[REDACTED]')
    .replace(/password=[^&]*/gi, 'password=[REDACTED]');

  const line =
    `${new Date().toISOString()} | ${method} ${safePath} | status=${Math.round(status)} | ${Math.round(durationMs)}ms\n`;

  try {
    const logsDir = path.join(process.cwd(), 'logs');
    await mkdir(logsDir, { recursive: true });
    const file = path.join(
      logsDir,
      `requests-${new Date().toISOString().slice(0, 10)}.log`
    );
    await appendFile(file, line, 'utf8');
  } catch {
    return NextResponse.json({ error: 'Log write failed' }, { status: 500 });
  }

  return new NextResponse(null, { status: 204 });
}
