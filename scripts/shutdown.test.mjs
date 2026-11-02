import { test } from 'node:test';
import assert from 'node:assert/strict';
import { exitCodeFor, createShutdown, shouldForward } from './lib/shutdown.mjs';

// Default clock advances 1000 ms per reading so signals are never treated as duplicates; pass a manual clock to control time.
function harness(platform, interactive = false, clock = null) {
  const calls = [];
  let tick = 0;
  const now = clock ? () => clock.t : () => (tick += 1000);
  const s = createShutdown({
    platform,
    interactive,
    pid: 42,
    killGroup: (pid, sig) => calls.push(['kill', pid, sig]),
    killChild: (sig) => calls.push(['killChild', sig]),
    removeHandlers: () => calls.push(['remove']),
    exit: (c) => calls.push(['exit', c]),
    now,
  });
  return { calls, ...s };
}

test('exitCodeFor maps codes and signals', () => {
  assert.equal(exitCodeFor(0, null), 0);
  assert.equal(exitCodeFor(7, null), 7);
  assert.equal(exitCodeFor(null, 'SIGINT'), 130);
  assert.equal(exitCodeFor(null, 'SIGTERM'), 143);
  assert.equal(exitCodeFor(null, 'SIGHUP', { SIGHUP: 1 }), 129);
  assert.equal(exitCodeFor(null, 'SIGWAT', {}), 1);
  assert.equal(exitCodeFor(null, null), 1);
});

test('posix: first signal forwarded once to group, second SIGINT kills group and exits 130', () => {
  const h = harness('darwin');
  h.onSignal('SIGINT');
  h.onSignal('SIGTERM');
  assert.deepEqual(h.calls, [['kill', 42, 'SIGINT']]);
  h.onSignal('SIGINT');
  assert.deepEqual(h.calls.slice(1), [['remove'], ['kill', 42, 'SIGKILL'], ['exit', 130]]);
});

test('win32: never forwards; second SIGINT exits 130 without kill', () => {
  const h = harness('win32');
  h.onSignal('SIGINT');
  h.onSignal('SIGINT');
  assert.deepEqual(h.calls, [['remove'], ['exit', 130]]);
});

test('child exit after signal reports 130/143 whatever the child code', () => {
  let h = harness('win32');
  h.onSignal('SIGINT');
  h.onChildExit(null, null);
  assert.deepEqual(h.calls.at(-1), ['exit', 130]);
  h = harness('linux');
  h.onSignal('SIGTERM');
  h.onChildExit(null, null);
  assert.deepEqual(h.calls.at(-1), ['exit', 143]);
  h = harness('linux');
  h.onSignal('SIGINT');
  h.onChildExit(0, null);
  assert.deepEqual(h.calls.at(-1), ['exit', 130]);
});

test('child exit without signal passes code through', () => {
  const h = harness('linux');
  h.onChildExit(3, null);
  assert.deepEqual(h.calls, [['exit', 3]]);
});

test('SIGHUP on posix is forwarded once, exit code is 129 even if child exits 0', () => {
  const h = harness('linux');
  h.onSignal('SIGHUP');
  h.onSignal('SIGHUP');
  assert.deepEqual(h.calls, [['kill', 42, 'SIGHUP']]);
  h.onChildExit(0, null);
  assert.deepEqual(h.calls.at(-1), ['exit', 129]);
});

test('process exit while child runs sends SIGTERM to group once (posix only)', () => {
  let h = harness('linux');
  h.onProcessExit();
  h.onProcessExit();
  assert.deepEqual(h.calls, [['kill', 42, 'SIGTERM']]);
  h = harness('linux');
  h.onChildExit(0, null);
  h.onProcessExit();
  assert.deepEqual(h.calls, [['exit', 0]]);
  h = harness('win32');
  h.onProcessExit();
  assert.deepEqual(h.calls, []);
});

test('shouldForward: only non-interactive posix', () => {
  assert.equal(shouldForward({ platform: 'linux', interactive: false }), true);
  assert.equal(shouldForward({ platform: 'darwin', interactive: true }), false);
  assert.equal(shouldForward({ platform: 'win32', interactive: false }), false);
});

test('SIGTERM then SIGINT forces (posix forward mode)', () => {
  const h = harness('linux');
  h.onSignal('SIGTERM');
  h.onSignal('SIGINT');
  assert.deepEqual(h.calls, [['kill', 42, 'SIGTERM'], ['remove'], ['kill', 42, 'SIGKILL'], ['exit', 130]]);
});

test('SIGHUP then SIGINT forces; later SIGTERM is ignored', () => {
  const h = harness('linux');
  h.onSignal('SIGHUP');
  h.onSignal('SIGTERM');
  h.onSignal('SIGINT');
  assert.deepEqual(h.calls, [['kill', 42, 'SIGHUP'], ['remove'], ['kill', 42, 'SIGKILL'], ['exit', 130]]);
});

test('onProcessExit after a forced kill sends nothing', () => {
  const h = harness('linux');
  h.onSignal('SIGINT');
  h.onSignal('SIGINT');
  const before = h.calls.length;
  h.onProcessExit();
  assert.equal(h.calls.length, before);
});

test('child killed by a signal with no signal received gives 128 + signal number', () => {
  const h = harness('linux');
  h.onChildExit(null, 'SIGKILL');
  assert.deepEqual(h.calls, [['exit', 137]]);
});

test('interactive: never forwards, exits 130 after child exits', () => {
  const h = harness('darwin', true);
  h.onSignal('SIGINT');
  h.onSignal('SIGTERM');
  assert.deepEqual(h.calls, []);
  h.onChildExit(0, null);
  h.onProcessExit();
  assert.deepEqual(h.calls, [['exit', 130]]);
});

test('interactive: second SIGINT exits 130 without kill; process exit sends nothing', () => {
  const h = harness('linux', true);
  h.onSignal('SIGINT');
  h.onSignal('SIGINT');
  h.onProcessExit();
  assert.deepEqual(h.calls, [['remove'], ['exit', 130]]);
});

test('interactive posix: lone SIGTERM is sent once to the child, exit 143 after child exits', () => {
  const h = harness('darwin', true);
  h.onSignal('SIGTERM');
  h.onSignal('SIGTERM');
  assert.deepEqual(h.calls, [['killChild', 'SIGTERM']]);
  h.onChildExit(0, null);
  h.onProcessExit();
  assert.deepEqual(h.calls.slice(1), [['exit', 143]]);
});

test('interactive win32: SIGTERM does not kill the child', () => {
  const h = harness('win32', true);
  h.onSignal('SIGTERM');
  assert.deepEqual(h.calls, []);
  h.onChildExit(null, null);
  assert.deepEqual(h.calls, [['exit', 143]]);
});

test('interactive posix: SIGINT and SIGHUP do not kill the child', () => {
  const h = harness('linux', true);
  h.onSignal('SIGINT');
  assert.deepEqual(h.calls, []);
  const g = harness('linux', true);
  g.onSignal('SIGHUP');
  assert.deepEqual(g.calls, []);
});

test('duplicate SIGINT within 500 ms is ignored (npm re-sends it)', () => {
  const clock = { t: 1000 };
  const h = harness('linux', false, clock);
  h.onSignal('SIGINT');
  clock.t += 499;
  h.onSignal('SIGINT');
  assert.deepEqual(h.calls, [['kill', 42, 'SIGINT']]);
});

test('SIGINT after 500 ms forces', () => {
  const clock = { t: 1000 };
  const h = harness('linux', false, clock);
  h.onSignal('SIGINT');
  clock.t += 500;
  h.onSignal('SIGINT');
  assert.deepEqual(h.calls.slice(1), [['remove'], ['kill', 42, 'SIGKILL'], ['exit', 130]]);
});

test('SIGTERM then SIGINT within 500 ms is ignored; a later SIGINT forces', () => {
  const clock = { t: 1000 };
  const h = harness('darwin', true, clock);
  h.onSignal('SIGTERM');
  clock.t += 100;
  h.onSignal('SIGINT');
  assert.deepEqual(h.calls, [['killChild', 'SIGTERM']]);
  clock.t += 900;
  h.onSignal('SIGINT');
  assert.deepEqual(h.calls.slice(1), [['remove'], ['exit', 130]]);
});
