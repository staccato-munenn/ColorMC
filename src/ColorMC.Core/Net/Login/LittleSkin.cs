﻿using ColorMC.Core.Game.Auth;
using ColorMC.Core.Objs.Login;

namespace ColorMC.Core.Net.Login;

public static class LittleSkin
{
    private const string ServerUrl = "https://littleskin.cn";
    public static async Task<(LoginState State, LoginObj? Obj, string? Msg)> Authenticate(string clientToken,
        string user, string pass, string? server = null)
    {
        var type = AuthType.LittleSkin;
        string server1;
        if (string.IsNullOrWhiteSpace(server))
        {
            server1 = ServerUrl;
        }
        else
        {
            type = AuthType.SelfLittleSkin;
            if (server.EndsWith("/api/yggdrasil"))
            {
                server = server.Replace("/api/yggdrasil", "");
            }
            if (server.EndsWith("/user"))
            {
                server = server.Replace("/user", "");
            }
            server1 = server;
        }

        var obj = await LoginOld.Authenticate(server1 + "/api/yggdrasil", clientToken, user, pass);
        if (obj.State != LoginState.Done)
            return obj;

        obj.Obj!.AuthType = type;
        if (type == AuthType.SelfLittleSkin)
        {
            obj.Obj.Text1 = server;
        }

        return obj;
    }

    public static Task<(LoginState State, LoginObj? Obj)> Refresh(LoginObj obj)
    {
        string server;
        if (obj.AuthType == AuthType.LittleSkin)
        {
            server = ServerUrl;
        }
        else
        {
            server = obj.Text1 + "/api/yggdrasil";
        }

        return LoginOld.Refresh(server, obj);
    }
}
