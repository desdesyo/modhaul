# CSS Dev Controller (Ollama)

This is a separate local agent, not ChatGPT remote control. It uses Ollama's local tool-call API and the PCBridge guest agent.

## Requirements
- Windows host/guest with .NET 8 runtime (controller is framework-dependent).
- Ollama installed and a model pulled, e.g. `ollama pull qwen2.5:3b`.
- PCBridge guest agent running in the VM; configure Allowed root to the CSS v34 folder and Start agent.
- MCP server is optional for this controller.

## Start
In the host terminal:
```powershell
$env:PCBRIDGE_URL="http://<VM-IP>:8765/"
$env:PCBRIDGE_TOKEN="<token copied from guest agent>"
$env:OLLAMA_MODEL="qwen2.5:3b"
dotnet run --project PCBridge/controller/PCBridge.Controller.csproj
```
The guest agent currently binds to loopback only (127.0.0.1), so remote host-to-guest connectivity is **not enabled** by default. For host controller, run controller in the guest itself or deliberately configure a secure VM-only network listener/firewall before using a VM IP. Do not expose port 8765 to public networks.

## Direct commands
`/status`, `/ls [relative path]`, `/read relative-file`, `/hash relative-file`, `/help`, `/quit`.

Natural-language tasks are sent to Ollama; its tool loop supports status, screenshot, scoped file list/read/hash/write. Writes require a y/N confirmation. File root confinement is enforced by guest agent.

## Limits / test status
- Ollama is a local model, not ChatGPT.
- Agent loop capped at 12 tool rounds.
- Screenshot returns base64 data to the model as text (not image-vision input in this initial controller).
- No automated CSS build or in-game test is implemented yet. Those require a verified v34 SDK/compiler and an active game VM. Do not call this a complete autonomous repair/mod pipeline.
