# CSSBC Native bootstrap (CSS v34)

This is an intentionally minimal **Metamod:Source server-plugin bootstrap**. It does not implement bot AI yet. Its purpose is to establish a clean, no-SourceMod native loading baseline before features are added.

## Compatibility warning
The target is the old Counter-Strike: Source v34 (likely an old engine build). Current Metamod/HL2SDK CSS headers and binaries are not guaranteed compatible with every v34 non-Steam distribution. This bootstrap is not a release-ready DLL until it is compiled against a matching SDK/interface and tested on that exact engine.

## Build
Use Visual Studio C++ (x86) and the AlliedModders HL2SDK branch `css`. Include `hl2sdk-css/public` and its required public include directories. Build as a 32-bit DLL. The exported factory must return `IServerPluginCallbacks` for `ISERVERPLUGINCALLBACKS_VERSION`.

## Install only after a confirmed matching MetaMod build
Do not place this DLL into `bin` or `addons` yet. No automatic install is included. A wrong ABI can crash the game/server.
