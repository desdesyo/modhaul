# PCBridge MCP bridge (prototype)

## What's implemented
- `mcp-server`: MCP JSON-RPC over **stdio** (initialize, tools/list, tools/call).
- Tools: status, screenshot, click, Unicode text, allowlisted key, open URL/folder, consent-gated PowerShell.
- MCP server forwards actions to the guest agent HTTP API.

## Local test (same Windows guest)
1. Build guest agent: `dotnet publish PCBridge/guest-agent/PCBridge.csproj -c Release -r win-x64 --self-contained true`.
2. Run PCBridge.exe, keep the token displayed in its token box, click **Start agent**.
3. Build MCP server: `dotnet publish PCBridge/mcp-server/PCBridge.Mcp.csproj -c Release`.
4. Set environment variables `PCBRIDGE_URL=http://127.0.0.1:8765/` and `PCBRIDGE_TOKEN=<token>`.
5. Start `PCBridge.Mcp.exe` as an MCP stdio server from an MCP-capable client.

## Important limitations (no false claim of chat connection)
- This is **not connected to this ChatGPT conversation**. MCP stdio is a local client integration; this chat has no configured MCP transport to this machine.
- ChatGPT Free currently cannot add arbitrary user MCP servers with action tools. A compatible ChatGPT plan/client and explicit MCP configuration are needed.
- Agent currently listens on guest loopback only. Host-to-guest networking/port forwarding is intentionally NOT enabled by default. Do not expose port 8765 publicly.
- Screenshot is returned as JSON text containing base64 PNG, not an MCP image block yet.
- No persistent remote tunnel, authentication pairing UI, or VM provisioning automation yet.
- This is a prototype. PowerShell requires an on-guest confirmation dialog; do not disable it.

## Safety
Keep the guest isolated. Use a random token, do not publish it, and never forward port 8765 to the internet. Prefer a VM snapshot before testing.
