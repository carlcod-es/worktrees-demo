#!/usr/bin/env node
// Starts the Aspire AppHost for this worktree with stable, collision-free ports.
import { spawn } from 'node:child_process';
import path from 'node:path';
import { realpathSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { pickPorts } from './lib/ports.mjs';
import { currentBranch } from './lib/git.mjs';

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

const child = spawn('dotnet', ['run', '--project', path.join(root, 'src', 'uShopDemo.AppHost'), '--no-launch-profile'], {
  cwd: root,
  env,
  stdio: 'inherit',
});

// Ctrl+C reaches the child through the shared TTY; make sure termination signals do too.
for (const sig of ['SIGINT', 'SIGTERM']) process.on(sig, () => child.kill(sig));
child.on('exit', (code) => process.exit(code ?? 0));
