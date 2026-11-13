CSSBC Recovery Utility

Checks bin/engine.dll, bin/client.dll and bin/server.dll; validates PE32 x86 structure and SHA-256; restores only from a user-selected clean copy of the exact same game build. Before replacement it backs up the old file, verifies backup and staged replacement hashes, then verifies the installed hash. Work runs off the UI thread and supports cancellation. CI build timeout: 10 minutes.

The repository has no verified pristine reference set. The tool will not download DLLs from untrusted mirrors. Use a clean copy from the exact same v34 distribution.

Build: GitHub Actions → Build CSSBC Recovery Utility → latest successful run → Artifacts → CSSBC_Recovery.
Run: extract EXE, choose game root, click Проверить файлы. For repair, choose clean copy root and click Восстановить из копии. Keep the .CSSBC_Backup_* folder until tested.
