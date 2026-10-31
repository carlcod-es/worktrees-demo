import { createHash } from 'node:crypto';
import net from 'node:net';

export const BASE = 15000;
export const SLOTS = 100;
export const BLOCK = 10;

/** Offsets inside a block. */
export const OFFSETS = { web: 0, dashboard: 1, otlp: 2, resourceService: 3 };

/** Stable slot (0..SLOTS-1) derived from a path. Case-insensitive so Windows/macOS agree. */
export function slotFor(path) {
  const h = createHash('sha1').update(path.toLowerCase()).digest();
  return h.readUInt32BE(0) % SLOTS;
}

export function blockPorts(slot) {
  const start = BASE + slot * BLOCK;
  return Object.fromEntries(Object.entries(OFFSETS).map(([k, o]) => [k, start + o]));
}

export function isFree(port) {
  return new Promise((resolve) => {
    const srv = net.createServer();
    srv.once('error', () => resolve(false));
    srv.once('listening', () => srv.close(() => resolve(true)));
    srv.listen(port);
  });
}

/**
 * First free block starting at the path's slot, wrapping around.
 * `free` is injectable for tests.
 */
export async function pickPorts(path, free = isFree) {
  const first = slotFor(path);
  for (let i = 0; i < SLOTS; i++) {
    const slot = (first + i) % SLOTS;
    const ports = blockPorts(slot);
    const results = await Promise.all(Object.values(ports).map((p) => free(p)));
    if (results.every(Boolean)) return { slot, ports, shifted: i > 0 };
  }
  throw new Error('No free port block available');
}
