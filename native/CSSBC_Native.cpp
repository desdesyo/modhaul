#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "iserverplugin.h"

class CSSBCNative final : public IServerPluginCallbacks {
public:
    bool Load(CreateInterfaceFn, CreateInterfaceFn, char *error, int maxlen) override {
        if (error && maxlen) error[0] = '\\0';
        return true;
    }
    void Unload() override {}
    void Pause() override {}
    void UnPause() override {}
    const char *GetPluginDescription() override { return "CSS Bot Community Native (safe bootstrap)"; }
    void LevelInit(char const *) override {}
    void ServerActivate(edict_t *, int, int) override {}
    void GameFrame(bool) override {}
    void LevelShutdown() override {}
    void ClientActive(edict_t *) override {}
    void ClientDisconnect(edict_t *) override {}
    void ClientPutInServer(edict_t *, char const *) override {}
    void SetCommandClient(int) override {}
    void ClientSettingsChanged(edict_t *) override {}
    PLUGIN_RESULT ClientConnect(bool *, edict_t *, const char *, const char *, char *, int) override { return PLUGIN_CONTINUE; }
    PLUGIN_RESULT ClientCommand(edict_t *, const CCommand &) override { return PLUGIN_CONTINUE; }
    PLUGIN_RESULT NetworkIDValidated(const char *, const char *) override { return PLUGIN_CONTINUE; }
    void OnQueryCvarValueFinished(int, edict_t *, EQueryCvarValueStatus, const char *, const char *) override {}
    void OnEdictAllocated(edict_t *) override {}
    void OnEdictFreed(const edict_t *) override {}
};

static CSSBCNative g_CSSBC;
extern "C" __declspec(dllexport) void *CreateInterface(const char *name, int *ret) {
    if (ret) *ret = 1;
    if (name && !strcmp(name, ISERVERPLUGINCALLBACKS_VERSION)) {
        if (ret) *ret = 0;
        return static_cast<IServerPluginCallbacks *>(&g_CSSBC);
    }
    return nullptr;
}
