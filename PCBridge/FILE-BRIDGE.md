# PCBridge CSS File Bridge

## Added in this iteration
- Guest agent has a configurable **Allowed root** (defaults to `C:\Games\Counter-Strike Source v34`).
- MCP tools: `pc_file_list`, `pc_file_read`, `pc_file_write`, `pc_file_hash`.
- Paths are canonicalized and confined to the configured root; traversal outside it is rejected.
- Text reads limited to 2 MB; writes to 1 MB; SHA-256 hashes available.
- Writes are direct and therefore should be used only for explicit requested changes; take a VM snapshot first.

## Current boundaries
This is a scoped file bridge, not a complete autonomous coding agent. It does not yet compile/test CSS itself or connect to the current ChatGPT conversation. The MCP server is stdio and needs a compatible MCP client to invoke it. Local Ollama can support tool-calling agent loops, but that is a separate local AI—not ChatGPT. Ollama API reference: https://github.com/ollama/ollama/blob/main/docs/capabilities/tool-calling.mdx

## Setup
1. Wait for GitHub Actions workflow `PCBridge Build` to finish.
2. Download its `PCBridge-win-x64` artifact.
3. In the VM, launch `PCBridge.exe`, set Allowed root to the exact CSS directory, and Start agent.
4. Configure the MCP client with the MCP server executable and environment variables `PCBRIDGE_URL`, `PCBRIDGE_TOKEN`.
5. Keep the VM isolated and take a snapshot before file writes.
