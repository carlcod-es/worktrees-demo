#!/usr/bin/env node
// Starts the Aspire AppHost for this worktree with stable, collision-free ports.
import { spawn } from 'node:child_process';
import path from 'node:path';
import { realpathSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { pickPorts } from './lib/ports.mjs';
import { currentBranch } from './lib/git.mjs';
import { createShutdown, shouldForward } from './lib/shutdown.mjs';

const root = realpathSync(path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..'));
const branch = currentBranch(root);
const { ports, shifted } = await pickPorts(root);
// Everything is HTTPS: the ASP.NET dev cert (CN=localhost) is valid on any port, so every worktree's block works.
const url = (p) => `https://localhost:${p}`;
const site = url(ports.web);

console.log(`worktree  ${root}`);
console.log(`branch    ${branch}`);
console.log(`site      ${site}${shifted ? '  (default ports busy, shifted)' : ''}`);
console.log(`backoffice ${site}/umbraco  admin@demo.local / Demo-Password-1234!`);
console.log(`api       ${url(ports.api)}`);
console.log(`dashboard ${url(ports.dashboard)}\n`);

const env = {
  ...process.env,
  DOTNET_ENVIRONMENT: 'Development',
  ASPNETCORE_ENVIRONMENT: 'Development',
  ASPNETCORE_URLS: url(ports.dashboard),
  ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL: url(ports.otlp),
  ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL: url(ports.resourceService),
  // Anonymous dashboard access (no login token); transport is still TLS.
  ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS: 'true',
  USHOP_WEB_PORT: String(ports.web),
  USHOP_API_PORT: String(ports.api),
  USHOP_BRANCH: branch,
};

// Two modes, chosen by shouldForward (lib/shutdown.mjs):
// - Interactive (stdin and stdout are TTYs, any OS) and Windows: the child is NOT detached. It stays in the terminal's
//   foreground group, so the terminal/console delivers Ctrl+C, Ctrl+Z and SIGHUP to the whole tree (dotnet run -> AppHost).
//   Detaching here would put it in a background group: SIGTTOU/SIGTTIN on tty access, and Ctrl+Z would not reach it.
//   We forward nothing except a lone external SIGTERM on POSIX (see lib/shutdown.mjs); anything more would double-signal
//   or only hit the `dotnet run` wrapper. Otherwise we wait for the child.
// - Non-interactive POSIX (background, CI, wrapper): own process group so the whole tree can be signalled. A detached
//   group gets no terminal signals, so our forward of SIGINT/SIGTERM/SIGHUP is the single delivery; on exit we SIGTERM the group.
const interactive = Boolean(process.stdin.isTTY && process.stdout.isTTY);
const forward = shouldForward({ platform: process.platform, interactive });
const child = spawn('dotnet', ['run', '--project', path.join(root, 'src', 'uShopDemo.AppHost'), '--no-launch-profile'], {
  cwd: root,
  env,
  stdio: 'inherit',
  detached: forward,
});

child.on('error', (err) => {
  console.error(`Failed to start 'dotnet': ${err.message}. Is the .NET SDK installed and on PATH?`);
  process.exit(1);
});

// SIGHUP exists on POSIX only.
const signals = process.platform === 'win32' ? ['SIGINT', 'SIGTERM'] : ['SIGINT', 'SIGTERM', 'SIGHUP'];
const { onSignal, onChildExit, onProcessExit } = createShutdown({
  platform: process.platform,
  interactive,
  pid: child.pid,
  killGroup: (pid, sig) => {
    try { process.kill(-pid, sig); } catch { /* group already gone */ }
  },
  killChild: (sig) => {
    try { child.kill(sig); } catch { /* child already gone */ }
  },
  removeHandlers: () => signals.forEach((s) => process.removeAllListeners(s)),
  exit: (code) => process.exit(code),
});
signals.forEach((sig) => process.on(sig, () => onSignal(sig)));
child.on('exit', onChildExit);
process.on('exit', onProcessExit);
