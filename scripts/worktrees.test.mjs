import { test } from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync, spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, realpathSync, rmSync, writeFileSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { DEFAULTS, parseArgs, parseWorktrees, siblingPath, slug } from './lib/worktrees.mjs';

const script = path.resolve(path.dirname(fileURLToPath(import.meta.url)), 'worktrees.mjs');

test('slug replaces path-unsafe characters', () => {
  assert.equal(slug('layout/minimal'), 'layout-minimal');
  assert.equal(slug('a\\b:c*d?e"f<g>h|i j'), 'a-b-c-d-e-f-g-h-i-j');
  assert.equal(slug('v1.2_x-y'), 'v1.2_x-y');
});

test('siblingPath is derived from the main worktree path', () => {
  const main = path.join(path.sep, 'work', 'repo');
  assert.equal(siblingPath(main, 'layout/catalog'), path.join(path.sep, 'work', 'repo.layout-catalog'));
});

test('parseWorktrees handles branch, detached, bare-ish and prunable entries', () => {
  const out = [
    'worktree /w/repo\nHEAD abc123\nbranch refs/heads/main',
    'worktree /w/repo.x\nHEAD def456\ndetached',
    'worktree /w/repo.gone\nHEAD 111\nbranch refs/heads/layout/a\nprunable gitdir file points to non-existent location',
  ].join('\n\n') + '\n';
  const wts = parseWorktrees(out);
  assert.deepEqual(wts.map((w) => w.path), ['/w/repo', '/w/repo.x', '/w/repo.gone']);
  assert.deepEqual(wts.map((w) => w.branch), ['main', null, 'layout/a']);
  assert.deepEqual(wts.map((w) => w.prunable), [false, false, true]);
});

test('parseArgs treats --force as a flag, not a branch', () => {
  assert.deepEqual(parseArgs(['down', '--force']), { cmd: 'down', branches: DEFAULTS, force: true, unknown: [] });
  assert.deepEqual(parseArgs(['down', 'a/b', '--force', 'c']).branches, ['a/b', 'c']);
  assert.deepEqual(parseArgs(['up']).branches, DEFAULTS);
  assert.deepEqual(parseArgs(['up', '--bogus']).unknown, ['--bogus']);
});

test('CLI without args prints usage and exits 1', () => {
  const r = spawnSync(process.execPath, [script], { encoding: 'utf8' });
  assert.equal(r.status, 1);
  assert.match(r.stderr, /usage:/);
});

// ---- integration: temp repo only; never touches this repo's worktrees ----

const git = (args, cwd) => execFileSync('git', args, { cwd, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim();
const wt = (cwd, ...args) => spawnSync(process.execPath, [script, ...args], { cwd, encoding: 'utf8' });

test('up / list / down cycle in a temp repo', (t) => {
  const parent = realpathSync(mkdtempSync(path.join(os.tmpdir(), 'wt-test-')));
  t.after(() => rmSync(parent, { recursive: true, force: true }));
  const repo = path.join(parent, 'repo');
  mkdirSync(repo);
  git(['init', '-b', 'main'], repo);
  git(['config', 'user.name', 'Test'], repo);
  git(['config', 'user.email', 'test@example.invalid'], repo);
  git(['config', 'commit.gpgsign', 'false'], repo);

  // No commit on main yet: up must fail clearly.
  let r = wt(repo, 'up', 'layout/a');
  assert.equal(r.status, 1);
  assert.match(r.stderr, /no commit/);

  writeFileSync(path.join(repo, 'README.md'), 'x\n');
  git(['add', '.'], repo);
  git(['commit', '-m', 'baseline'], repo);
  const baseline = git(['rev-parse', 'main'], repo);

  // Move the main checkout off main: new branches must still come from main.
  git(['checkout', '-b', 'feature'], repo);
  writeFileSync(path.join(repo, 'f.txt'), 'f\n');
  git(['add', '.'], repo);
  git(['commit', '-m', 'feature'], repo);

  const a = path.join(parent, 'repo.layout-a');
  const b = path.join(parent, 'repo.layout-b');
  r = wt(repo, 'up', 'layout/a', 'layout/b');
  assert.equal(r.status, 0, r.stderr);
  assert.ok(existsSync(a) && existsSync(b));
  assert.equal(git(['rev-parse', 'layout/a'], repo), baseline);
  assert.match(r.stdout, /dev\.mjs/);

  // Re-run is idempotent; running from a linked worktree builds siblings from the main one.
  r = wt(a, 'up', 'layout/a', 'layout/c');
  assert.equal(r.status, 0, r.stderr);
  assert.match(r.stdout, /exists\s+layout\/a/);
  assert.ok(existsSync(path.join(parent, 'repo.layout-c')));
  assert.ok(!existsSync(path.join(parent, 'repo.layout-a.layout-c')));

  r = wt(repo, 'list');
  assert.equal(r.status, 0, r.stderr);
  for (const br of ['feature', 'layout/a', 'layout/b', 'layout/c']) assert.ok(r.stdout.includes(br), br);

  // Unregistered directory in the way fails clearly, others still processed.
  mkdirSync(path.join(parent, 'repo.layout-d'));
  r = wt(repo, 'up', 'layout/d', 'layout/e');
  assert.equal(r.status, 1);
  assert.match(r.stderr, /not a registered worktree/);
  assert.ok(existsSync(path.join(parent, 'repo.layout-e')));

  // Refuse to remove the worktree we're standing in.
  r = wt(a, 'down', 'layout/a');
  assert.equal(r.status, 1);
  assert.match(r.stderr, /refusing/);
  assert.ok(existsSync(a));

  // Dirty worktree: skipped without --force, removed with it.
  writeFileSync(path.join(b, 'untracked.txt'), 'work\n');
  r = wt(repo, 'down', 'layout/a', 'layout/b');
  assert.equal(r.status, 1);
  assert.match(r.stderr, /uncommitted changes, skipped; use --force/);
  assert.ok(!existsSync(a), 'clean worktree removed');
  assert.ok(existsSync(b), 'dirty worktree kept');

  r = wt(repo, 'down', '--force', 'layout/b');
  assert.equal(r.status, 0, r.stderr);
  assert.ok(!existsSync(b));
  assert.ok(git(['branch', '--list', 'layout/b'], repo).includes('layout/b'), 'branch kept');

  // Missing (deleted) worktree dir does not break list.
  rmSync(path.join(parent, 'repo.layout-c'), { recursive: true, force: true });
  r = wt(repo, 'list');
  assert.equal(r.status, 0, r.stderr);
  assert.match(r.stdout, /\(missing\)/);

  r = wt(repo, 'down', 'layout/c');
  assert.equal(r.status, 0, r.stderr);
  assert.match(r.stdout, /removed/);
});
