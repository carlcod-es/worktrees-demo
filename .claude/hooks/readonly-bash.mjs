// PreToolUse hook: blocks mutating Bash commands from the reviewer and supervisor subagents.
// Exit 2 = block; stderr is returned to the agent.
const READONLY_AGENTS = new Set(['reviewer', 'supervisor']);

const REDIRECT = /(^|[^<>&0-9])>{1,2}(?!&)/; // output redirection (allows 2>&1)

// Removes quoted text so a `>` in a grep pattern isn't read as a redirect. Fails closed: keeps an
// unterminated quote, and a double-quoted string containing a command substitution, which can redirect.
function withoutQuotedText(command) {
  let out = '';
  let i = 0;
  while (i < command.length) {
    const c = command[i];
    if (c === '\\') {
      out += command.slice(i, i + 2);
      i += 2;
    } else if (c === "'" || c === '"') {
      let end = i + 1;
      while (end < command.length && command[end] !== c) end += c === '"' && command[end] === '\\' ? 2 : 1;
      const quoted = command.slice(i, end + 1);
      if (end >= command.length || (c === '"' && /\$\(|`/.test(quoted))) out += quoted;
      i = end + 1;
    } else {
      out += c;
      i += 1;
    }
  }
  return out;
}

const DENY = [
  /\bgit\s+(?:-C\s+\S+\s+)?(add|am|apply|branch\s+-[dDmMcC]|checkout|cherry-pick|clean|clone|commit|config\s+(?!--get|-l|--list)|fetch|init|merge|mv|pull|push|rebase|reset|restore|revert|rm|stash|switch|tag|update-ref|worktree\s+(add|remove|move|prune))\b/,
  /\btee\b/,
  /\b(rm|rmdir|mv|cp|mkdir|touch|chmod|chown|ln|truncate)\b/,
  /\bsed\b[^|]*\s-i/,
  /\bdotnet\s+(add|remove|new|ef\s+migrations\s+(add|remove)|ef\s+database|tool\s+(install|uninstall|update))\b/,
  /\bdotnet\s+format\b(?![^|;&]*--verify-no-changes)/,
  /\b(npm|pnpm|yarn)\s+(i|install|add|remove|uninstall|update|ci)\b/,
];

let input = '';
for await (const chunk of process.stdin) input += chunk;

const data = JSON.parse(input || '{}');
if (!READONLY_AGENTS.has(data.agent_type)) process.exit(0);

const command = data.tool_input?.command ?? '';
const hit = REDIRECT.test(withoutQuotedText(command)) || DENY.some((re) => re.test(command));
if (hit) {
  process.stderr.write(`Blocked: ${data.agent_type} is read-only (tests, linters, type checks, read-only git only). Command: ${command}`);
  process.exit(2);
}
