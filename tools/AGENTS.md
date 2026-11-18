# CSSBC recovery tools instructions

Read repository-root \`AGENTS.md\` first.
- Recovery utility is only for PE32 x86 structural inspection and restoring from a user-selected clean copy of the exact same v34 build.
- A valid MZ/PE signature and x86 machine type do not prove a DLL is clean, compatible or safe.
- There is no verified pristine reference set in this repository. Never download replacements from untrusted mirrors or treat repository DLLs as clean baselines.
- Before replacement, require exact build provenance, independent source/destination hashes, a verified backup, staged-file validation, atomic replacement and post-write hash verification.
- Keep the game closed. Abort on ambiguous source, same source/destination, missing backup, hash mismatch, unexpected architecture or failed post-check.
- Preserve backup until the user has tested the game. Never silently delete recovery data.
- Keep long-running filesystem work off the UI thread and honor cancellation without leaving partial files.
