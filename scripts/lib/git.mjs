import { execFileSync } from 'node:child_process';

export function git(args, cwd) {
  return execFileSync('git', args, { cwd, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim();
}

/** Branch name; works before the first commit; "detached-<sha>" when detached. */
export function currentBranch(cwd) {
  try {
    return git(['symbolic-ref', '--short', 'HEAD'], cwd);
  } catch {
    try {
      return `detached-${git(['rev-parse', '--short', 'HEAD'], cwd)}`;
    } catch {
      return 'unknown';
    }
  }
}
