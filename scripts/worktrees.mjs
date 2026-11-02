#!/usr/bin/env node
// Manage demo worktrees for the repo in the current directory.
// node scripts/worktrees.mjs <up|list|down> [--force] [branch...]
import { existsSync, realpathSync } from 'node:fs';
import path from 'node:path';
import { git } from './lib/git.mjs';
import { slotFor, blockPorts } from './lib/ports.mjs';
import { BASE_BRANCH, DEFAULTS, parseArgs, parseWorktrees, siblingPath, normPath, isInside } from './lib/worktrees.mjs';

const USAGE = 'usage: node scripts/worktrees.mjs <up|list|down> [--force] [branch...]\n' +
  'default branches: ' + DEFAULTS.join(', ') + '\n' +
  '--force (down): remove worktrees even with uncommitted changes';

const cwd = process.cwd();
const errText = (e) => (e.stderr?.toString().trim() || e.message);
const worktrees = () => parseWorktrees(git(['worktree', 'list', '--porcelain'], cwd));
// First porcelain entry is always the main worktree, whichever checkout we run from.
const mainPath = () => worktrees()[0].path;
const branchExists = (b) => {
  try { git(['rev-parse', '--verify', '--quiet', `refs/heads/${b}`], cwd); return true; } catch { return false; }
};
const registered = (dir) => worktrees().find((w) => normPath(w.path) === normPath(dir));

function up({ branches }) {
  const main = mainPath();
  let failed = false;
  const ready = [];
  for (const b of branches) {
    const dir = siblingPath(main, b);
    try {
      if (registered(dir)) {
        console.log(`exists   ${b} -> ${dir}`);
      } else if (existsSync(dir)) {
        throw new Error(`${dir} exists but is not a registered worktree`);
      } else if (branchExists(b)) {
        git(['worktree', 'add', dir, b], cwd);
        console.log(`created  ${b} -> ${dir}`);
      } else {
        try { git(['rev-parse', '--verify', '--quiet', `${BASE_BRANCH}^{commit}`], cwd); }
        catch { throw new Error(`branch '${BASE_BRANCH}' has no commit; commit the baseline first`); }
        git(['worktree', 'add', '-b', b, dir, BASE_BRANCH], cwd);
        console.log(`created  ${b} -> ${dir} (new branch from ${BASE_BRANCH})`);
      }
      ready.push(dir);
    } catch (e) {
      failed = true;
      console.error(`failed   ${b}: ${errText(e)}`);
    }
  }
  list();
  if (ready.length) {
    console.log('\nStart each in its own terminal:');
    for (const dir of ready) console.log(`  node ${path.join(dir, 'scripts', 'dev.mjs')}`);
  }
  return failed;
}

function list() {
  for (const w of worktrees()) {
    // Same path form dev.mjs hashes (realpath); a missing dir falls back to the recorded path.
    let real = w.path;
    let note = '';
    try { real = realpathSync(w.path); } catch { note = '  (missing)'; }
    const p = blockPorts(slotFor(real));
    console.log(`${(w.branch ?? '(detached)').padEnd(20)} site https://localhost:${p.web}  api https://localhost:${p.api}  dashboard https://localhost:${p.dashboard}  ${w.path}${note}`);
  }
  return false;
}

// Locale-independent: any porcelain output (modified or untracked) means dirty.
// A deleted worktree directory has nothing to lose; let `worktree remove` handle it.
const isDirty = (dir) => existsSync(dir) && git(['-C', dir, 'status', '--porcelain'], cwd).trim() !== '';

function down({ branches, force }) {
  const main = mainPath();
  let failed = false;
  for (const b of branches) {
    const dir = siblingPath(main, b);
    try {
      if (!registered(dir)) {
        console.log(existsSync(dir) ? `skipped  ${b}: ${dir} is not a registered worktree` : `absent   ${b}`);
        continue;
      }
      if (isInside(dir, cwd)) throw new Error(`refusing to remove the worktree you are in (${dir})`);
      if (!force && isDirty(dir)) {
        failed = true;
        console.error(`skipped  ${b}: has uncommitted changes, skipped; use --force`);
        continue;
      }
      git(['worktree', 'remove', ...(force ? ['--force'] : []), dir], cwd);
      console.log(`removed  ${dir} (branch ${b} kept)`);
    } catch (e) {
      failed = true;
      console.error(`failed   ${b}: ${errText(e)}`);
    }
  }
  git(['worktree', 'prune'], cwd);
  return failed;
}

const commands = { up, list, down };
const args = parseArgs(process.argv.slice(2));
if (!Object.hasOwn(commands, args.cmd) || args.unknown.length) {
  if (args.unknown.length) console.error(`unknown option: ${args.unknown.join(' ')}`);
  console.error(USAGE);
  process.exit(1);
}
try {
  process.exitCode = commands[args.cmd](args) ? 1 : 0;
} catch (e) {
  console.error(errText(e));
  process.exit(1);
}
