# PCBridge prototype instructions

Read repository-root \`AGENTS.md\` first.
- PCBridge is a standalone local prototype, not connected to the current ChatGPT conversation or SOLO.Chat unless a verified transport is implemented.
- Keep the guest listener loopback-only by default; never expose port 8765 publicly. Preserve token authentication and guest consent prompts.
- Treat all paths as hostile input. Canonicalize and enforce root containment for every operation; also reject symlink/junction/reparse-point escapes, traversal, device paths and NTFS alternate data streams.
- Do not use direct file writes for game binaries. For any allowed text write: explicit request, preview, expected hash, verified backup, atomic replace, post-write hash and audit record are mandatory.
- Never execute arbitrary task text or model-generated shell commands. If PowerShell exists, retain local consent and strict timeout; do not use it as a bypass around file policy.
- Do not claim remote access from ChatGPT; MCP stdio and local Ollama are separate clients.
- Keep the VM isolated and take snapshots before experiments. Do not place credentials in source, logs or model prompts.
