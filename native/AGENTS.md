# Native VSP source instructions

Read repository-root \`AGENTS.md\` first.
- This directory is a VSP bootstrap layer only. Keep it small and fail closed if required engine interfaces are unavailable.
- Do not implement GameDLL-owned player allocation here. The VSP cannot instantiate a valid CCS player by itself.
- Verify exact v34 ABI, Win32/x86, interface version, SDK headers and exported factory before packaging.
- Do not add engine hooks that mutate player state without a matching GameDLL integration contract and server-thread/lifetime checks.
- Do not install into \`bin\` or overwrite stock DLLs. Any install needs exact-build compatibility evidence, backup, hash verification, explicit approval and rollback.
- Keep logging bounded and never log IP addresses, auth tokens or private identifiers.
- Current \`CSSBC_Native.cpp\` is a no-op bootstrap. Empty callbacks are intentional, not missing implementation to fill with guessed APIs.
