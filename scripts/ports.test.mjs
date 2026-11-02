import { test } from 'node:test';
import assert from 'node:assert/strict';
import net from 'node:net';
import { BASE, BLOCK, slotFor, blockPorts, pickPorts, isFree } from './lib/ports.mjs';

test('slot is stable and case-insensitive', () => {
  assert.equal(slotFor('/a/b'), slotFor('/a/b'));
  assert.equal(slotFor('C:\\Work\\X'), slotFor('c:\\work\\x'));
});

test('stable ports for repeat runs', async () => {
  const a = await pickPorts('/some/worktree', async () => true);
  const b = await pickPorts('/some/worktree', async () => true);
  assert.deepEqual(a, b);
  assert.equal(a.shifted, false);
});

test('shifts to next block when a port is taken', async () => {
  const slot = slotFor('/some/worktree');
  const taken = blockPorts(slot).dashboard;
  const r = await pickPorts('/some/worktree', async (p) => p !== taken);
  assert.equal(r.shifted, true);
  assert.notEqual(r.slot, slot);
});

test('isFree detects a listening port', async () => {
  const srv = net.createServer();
  await new Promise((r) => srv.listen(0, r));
  const port = srv.address().port;
  assert.equal(await isFree(port), false);
  srv.close();
});

test('api port is inside the block and distinct', () => {
  const slot = slotFor('/some/worktree');
  const ports = blockPorts(slot);
  const start = BASE + slot * BLOCK;
  assert.ok(ports.api >= start && ports.api < start + BLOCK);
  assert.equal(new Set(Object.values(ports)).size, Object.keys(ports).length);
});

test('block probe covers the api port', async () => {
  const slot = slotFor('/some/worktree');
  const taken = blockPorts(slot).api;
  const r = await pickPorts('/some/worktree', async (p) => p !== taken);
  assert.equal(r.shifted, true);
});
