#!/usr/bin/env node
// Manage demo worktrees.  node scripts/worktrees.mjs <up|list|down> [branch...]
import { existsSync, realpathSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { git } from './lib/git.mjs';
import { slotFor, blockPorts } from './lib/ports.mjs';

const root = realpathSync(path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..'));
const DEFAULTS = ['layout/minimal', 'layout/catalog', 'layout/showcase'];
const [cmd, ...rest] = process.argv.slice(2);
const branches = rest.length ? rest : DEFAULTS;

const slug = (b) => b.replace(/[\\/]+/g, '-');
const wtPath = (b) => path.join(path.dirname(root), `${path.basename(root)}.${slug(b)}`);
const branchExists = (b) => {
  try { git(['rev-parse', '--verify', '--quiet', `refs/heads/${b}`], root); return true; } catch { return false; }
};

function up() {
  for (const b of branches) {
    const dir = wtPath(b);
    if (existsSync(dir)) { console.log(`exists   ${b} -> ${dir}`); continue; }
    if (branchExists(b)) git(['worktree', 'add', dir, b], root);
    else git(['worktree', 'add', '-b', b, dir], root); // new branch from current HEAD
    console.log(`created  ${b} -> ${dir}`);
  }
  list();
  console.log('\nStart each in its own terminal:  cd <path> && node scripts/dev.mjs');
}

function list() {
  const lines = git(['worktree', 'list', '--porcelain'], root).split('\n\n');
  for (const block of lines) {
    const wt = /^worktree (.+)$/m.exec(block)?.[1];
    const br = /^branch refs\/heads\/(.+)$/m.exec(block)?.[1] ?? '(detached)';
    if (!wt) continue;
    const p = blockPorts(slotFor(realpathSync(wt)));
    console.log(`${br.padEnd(20)} site https://localhost:${p.web}  dashboard https://localhost:${p.dashboard}  ${wt}`);
  }
}

function down() {
  for (const b of branches) {
    const dir = wtPath(b);
    if (!existsSync(dir)) { console.log(`absent   ${b}`); continue; }
    git(['worktree', 'remove', '--force', dir], root);
    console.log(`removed  ${dir} (branch ${b} kept)`);
  }
  git(['worktree', 'prune'], root);
}

const commands = { up, list, down };
if (!commands[cmd]) {
  console.error('usage: node scripts/worktrees.mjs <up|list|down> [branch...]\ndefault branches: ' + DEFAULTS.join(', '));
  process.exit(1);
}
try { commands[cmd](); } catch (e) { console.error(e.stderr?.toString() || e.message); process.exit(1); }
