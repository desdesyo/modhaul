#pragma semicolon 1
#pragma newdecls required

#include <sourcemod>

#define CSSBC_VERSION "0.1.0"

public Plugin myinfo =
{
    name = "CSS Bot Community",
    author = "CSSBC",
    description = "In-game controls for Counter-Strike: Source bots",
    version = CSSBC_VERSION,
    url = "https://github.com/desdesyo/modhaul"
};

public void OnPluginStart()
{
    RegConsoleCmd("sm_cssbc", Command_CSSBC, "Open CSS Bot Community controls");
    RegConsoleCmd("sm_cssbc_add", Command_AddBot, "Add one bot");
    RegConsoleCmd("sm_cssbc_fill", Command_Fill, "Fill server with bots");
    RegConsoleCmd("sm_cssbc_kick", Command_KickBots, "Remove all bots");
    RegAdminCmd("cssbc_add", Command_AddBot, ADMFLAG_GENERIC);
    RegAdminCmd("cssbc_fill", Command_Fill, ADMFLAG_GENERIC);
    RegAdminCmd("cssbc_kick", Command_KickBots, ADMFLAG_GENERIC);
    CreateConVar("cssbc_version", CSSBC_VERSION, "CSS Bot Community version", FCVAR_NOTIFY|FCVAR_DONTRECORD);
}

public Action Command_CSSBC(int client, int args)
{
    if (client == 0) { ReplyToCommand(client, "[CSSBC] Use cssbc_add/cssbc_fill/cssbc_kick from server console."); return Plugin_Handled; }
    Menu m = new Menu(MenuHandler_Main);
    m.SetTitle("CSS Bot Community");
    m.AddItem("add", "Добавить бота");
    m.AddItem("fill", "Заполнить сервер ботами");
    m.AddItem("kick", "Удалить всех ботов");
    m.ExitButton = true;
    m.Display(client, 20);
    return Plugin_Handled;
}

public int MenuHandler_Main(Menu menu, MenuAction action, int param1, int param2)
{
    if (action == MenuAction_End) delete menu;
    else if (action == MenuAction_Select)
    {
        if (param2 == 0) FakeClientCommand(param1, "sm_cssbc_add");
        else if (param2 == 1) FakeClientCommand(param1, "sm_cssbc_fill");
        else if (param2 == 2) FakeClientCommand(param1, "sm_cssbc_kick");
    }
    return 0;
}

public Action Command_AddBot(int client, int args)
{
    ServerCommand("bot_add");
    ReplyToCommand(client, "[CSSBC] Запрошено добавление бота.");
    return Plugin_Handled;
}

public Action Command_Fill(int client, int args)
{
    ServerCommand("bot_quota_mode fill");
    ServerCommand("bot_quota 10");
    ServerCommand("bot_join_after_player 0");
    ReplyToCommand(client, "[CSSBC] Установлен bot_quota=10 (fill).");
    return Plugin_Handled;
}

public Action Command_KickBots(int client, int args)
{
    ServerCommand("bot_kick");
    ReplyToCommand(client, "[CSSBC] Команда удаления ботов отправлена.");
    return Plugin_Handled;
}
