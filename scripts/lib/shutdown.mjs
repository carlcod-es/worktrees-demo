// Signal handling and exit-code mapping for scripts/dev.mjs.
import os from 'node:os';

const DUPLICATE_WINDOW_MS = 500;
const SIGNAL_CODES = { SIGINT: 130, SIGTERM: 143, SIGHUP: 129 };

/** Exit code for a finished child: its own code, else 128 + signal number, else 1. */
export function exitCodeFor(code, signal, signals = os.constants.signals) {
  if (typeof code === 'number') return code;
  if (signal) {
    if (SIGNAL_CODES[signal]) return SIGNAL_CODES[signal];
    if (signals[signal]) return 128 + signals[signal];
  }
  return 1;
}

/** Forward signals to a detached process group only on non-interactive POSIX; otherwise the terminal/console delivers them. */
export function shouldForward({ platform, interactive }) {
  return platform !== 'win32' && !interactive;
}

/**
 * Wires shutdown behaviour. All side effects are injected so it is unit-testable.
 * Modes (see shouldForward):
 * - forward (non-interactive POSIX; child spawned detached): forward the first signal to the child's process group.
 * - no forward (Windows, or any interactive terminal; child not detached): the terminal/console delivers Ctrl+C,
 *   Ctrl+Z and SIGHUP to the whole foreground tree; just wait for the child.
 * - no forward on POSIX: a first SIGTERM is additionally sent once to the child via killChild (SIGINT/SIGHUP are not).
 * - once a signal was received, the exit code is 130/143/129 whatever the child's own code.
 * - a SIGINT within 500 ms of the first signal is treated as npm's forwarded duplicate and ignored (clock: now()).
 * - any later SIGINT after a first signal of any kind forces: drop handlers, SIGKILL the group (forward mode only), exit 130.
 * - onProcessExit: last-resort SIGTERM to the group if we exit while the child still runs (forward mode only).
 * Returns { onSignal, onChildExit, onProcessExit }.
 */
export function createShutdown({ platform, interactive = false, pid, killGroup, killChild = () => {}, removeHandlers, exit, now = Date.now }) {
  let received = null;
  let receivedAt = 0;
  let childGone = false;
  const forward = shouldForward({ platform, interactive });

  function onSignal(sig) {
    if (received) {
      if (sig !== 'SIGINT') return;
      // Under `npm run dev` on POSIX npm re-sends SIGINT/SIGTERM to its child, so one Ctrl+C can arrive twice back to back.
      if (now() - receivedAt < DUPLICATE_WINDOW_MS) return;
      // Forced: the AppHost gets no chance to clean up, so the SQL container may be left running (docker rm -f <name>).
      // Interactive/Windows: the terminal already delivered this Ctrl+C to the whole tree, so there is no group to kill.
      removeHandlers();
      if (forward) killGroup(pid, 'SIGKILL');
      childGone = true;
      exit(130);
      return;
    }
    received = sig;
    receivedAt = now();
    if (forward) killGroup(pid, sig);
    // Interactive POSIX: the terminal never generates SIGTERM, so a SIGTERM here comes from outside (kill, IDE, task runner)
    // and cannot double-signal. Send it once to the `dotnet run` pid. Verified on macOS (pty,
    // kill -TERM <dev.mjs pid>): dotnet run relays it to aspire and the AppHost, which stop and remove the SQL container. Windows: child.kill is a forced terminate, so never.
    else if (sig === 'SIGTERM' && platform !== 'win32') killChild('SIGTERM');
  }

  function onChildExit(code, signal) {
    childGone = true;
    exit(received ? (SIGNAL_CODES[received] ?? 1) : exitCodeFor(code, signal));
  }

  function onProcessExit() {
    if (forward && !childGone) {
      childGone = true;
      killGroup(pid, 'SIGTERM');
    }
  }

  return { onSignal, onChildExit, onProcessExit };
}
