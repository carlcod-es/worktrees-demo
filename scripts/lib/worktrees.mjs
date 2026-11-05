import path from 'node:path';
import { realpathSync } from 'node:fs';

export const DEFAULTS = ['theme/dark', 'theme/high-contrast'];

/** Branch name -> directory-safe suffix. Anything outside [A-Za-z0-9._-] becomes "-". */
export const slug = (branch) => branch.replace(/[^A-Za-z0-9._-]/g, '-');

/** Sibling directory of the main worktree: ../<repo>.<branch-slug>. */
export function siblingPath(mainPath, branch) {
  return path.join(path.dirname(mainPath), `${path.basename(mainPath)}.${slug(branch)}`);
}

/** Parse `git worktree list --porcelain` into [{ path, branch|null, head, prunable }]. First entry is the main worktree. */
export function parseWorktrees(porcelain) {
  return porcelain
    .split(/\r?\n\r?\n/)
    .map((block) => {
      const wt = /^worktree (.+?)\r?$/m.exec(block)?.[1];
      if (!wt) return null;
      return {
        path: wt,
        branch: /^branch refs\/heads\/(.+?)\r?$/m.exec(block)?.[1] ?? null,
        head: /^HEAD (\S+)/m.exec(block)?.[1] ?? null,
        prunable: /^prunable/m.test(block),
      };
    })
    .filter(Boolean);
}

/**
 * Path comparison key: realpath when it exists, else resolved.
 * Lowercased only on win32/darwin, whose default filesystems are case-insensitive; on Linux
 * two paths differing by case are distinct. Used for comparisons only. slotFor (ports.mjs)
 * lowercases on every platform by design (stable ports) and must not change.
 */
export function normPath(p) {
  let r;
  try { r = realpathSync(p); } catch { r = path.resolve(p); }
  return process.platform === 'win32' || process.platform === 'darwin' ? r.toLowerCase() : r;
}

/** True when `child` is `parent` or inside it. */
export function isInside(parent, child) {
  const rel = path.relative(normPath(parent), normPath(child));
  return rel === '' || (rel !== '..' && !rel.startsWith('..' + path.sep) && !path.isAbsolute(rel));
}

/** argv (without node/script) -> { cmd, branches, force, unknown }. Flags are never branch names. */
export function parseArgs(argv) {
  const [cmd, ...rest] = argv;
  const flags = rest.filter((a) => a.startsWith('-'));
  const positional = rest.filter((a) => !a.startsWith('-'));
  return {
    cmd,
    branches: positional.length ? positional : DEFAULTS,
    force: flags.includes('--force'),
    unknown: flags.filter((f) => f !== '--force'),
  };
}
