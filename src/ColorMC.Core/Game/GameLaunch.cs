using ColorMC.Core.Downloader;
using ColorMC.Core.Game;
using ColorMC.Core.Helpers;
using ColorMC.Core.LaunchPath;
using ColorMC.Core.Net;
using ColorMC.Core.Net.Apis;
using ColorMC.Core.Objs;
using ColorMC.Core.Objs.Login;
using ColorMC.Core.Objs.Minecraft;
using ColorMC.Core.Utils;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ColorMC.Core.Game;

/// <summary>
/// 游戏启动类
/// </summary>
public static class Launch
{
    public const string JAVA_LOCAL = "%JAVA_LOCAL%";
    public const string JAVA_ARG = "%JAVA_ARG%";
    public const string LAUNCH_DIR = "%LAUNCH_DIR%";
    public const string GAME_NAME = "%GAME_NAME%";
    public const string GAME_UUID = "%GAME_UUID%";
    public const string GAME_DIR = "%GAME_DIR%";
    public const string GAME_BASE_DIR = "%GAME_BASE_DIR%";

    /// <summary>
    /// 取消启动
    /// </summary>
    private static CancellationToken s_cancel;

    private static string GetString(this List<string> arg)
    {
        var data = new StringBuilder();
        foreach (var item in arg)
        {
            data.AppendLine(item);
        }

        return data.ToString();
    }

    /// <summary>
    /// 替换参数
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="jvm">JAVA位置</param>
    /// <param name="arg">JVM参数</param>
    /// <param name="item">命令</param>
    /// <returns></returns>
    public static string ReplaceArg(this GameSettingObj obj, string jvm, List<string> arg, string item)
    {
        return item
                .Replace(GAME_NAME, obj.Name)
                .Replace(GAME_UUID, obj.UUID)
                .Replace(GAME_DIR, obj.GetGamePath())
                .Replace(GAME_BASE_DIR, obj.GetBasePath())
                .Replace(LAUNCH_DIR, ColorMCCore.BaseDir)
                .Replace(JAVA_LOCAL, jvm)
                .Replace(JAVA_ARG, arg.GetString());
    }

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="cmd">命令</param>
    public static void CmdRun(GameSettingObj obj, string cmd)
    {
        var args = cmd.Split('\n');
        var file = args[0].Trim();
        if (file.StartsWith("./") || file.StartsWith("../"))
        {
            file = Path.GetFullPath(obj.GetBasePath() + "/" + file);
        }
        var arglist = new List<string>();

        var info = new ProcessStartInfo(file)
        {
            WorkingDirectory = obj.GetGamePath(),
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        for (int a = 1; a < args.Length; a++)
        {
            info.ArgumentList.Add(args[a].Trim());
        }
        using var p = new Process()
        {
            EnableRaisingEvents = true,
            StartInfo = info
        };
        p.OutputDataReceived += (a, b) =>
        {
            ColorMCCore.GameLog?.Invoke(obj, b.Data);
        };
        p.ErrorDataReceived += (a, b) =>
        {
            ColorMCCore.GameLog?.Invoke(obj, b.Data);
        };

        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        p.WaitForExit();
    }

    /// <summary>
    /// 检查游戏文件
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="login">登录的账户</param>
    /// <exception cref="LaunchException">启动错误</exception>
    /// <returns>下载列表</returns>
    public static async Task<ConcurrentBag<DownloadItemObj>> CheckGameFile(GameSettingObj obj, LoginObj login)
    {
        var list = new ConcurrentBag<DownloadItemObj>();

        //检查游戏启动json
        ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.Check);
        var game = VersionPath.GetGame(obj.Version);
        if (game == null)
        {
            ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.LostVersion);

            var version = VersionPath.Versions?.versions.Where(a => a.id == obj.Version).FirstOrDefault();
            if (version == null)
            {
                ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.VersionError);
                throw new LaunchException(LaunchState.VersionError,
                    LanguageHelper.Get("Core.Launch.Error1"));
            }

            var res1 = await GameDownloadHelper.Download(version);
            if (res1.State != GetDownloadState.End)
            {
                throw new LaunchException(LaunchState.VersionError,
                    LanguageHelper.Get("Core.Launch.Error1"));
            }

            res1.List!.ForEach(list.Add);

            game = VersionPath.GetGame(obj.Version);
        }

        if (game == null)
        {
            throw new LaunchException(LaunchState.VersionError, LanguageHelper.Get("Core.Launch.Error1"));
        }

        var list1 = new List<Task>();

        //检查游戏核心文件
        if (ConfigUtils.Config.GameCheck.CheckCore)
        {
            list1.Add(Task.Run(() =>
            {
                ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.CheckVersion);
                string file = LibrariesPath.GetGameFile(game.id);
                if (!File.Exists(file))
                {
                    list.Add(new()
                    {
                        Url = game.downloads.client.url,
                        SHA1 = game.downloads.client.sha1,
                        Local = file,
                        Name = $"{obj.Version}.jar"
                    });
                }
                else if (ConfigUtils.Config.GameCheck.CheckCoreSha1)
                {
                    using FileStream stream2 = new(file, FileMode.Open, FileAccess.ReadWrite,
                        FileShare.ReadWrite);
                    stream2.Seek(0, SeekOrigin.Begin);
                    string sha1 = FuntionUtils.GenSha1(stream2);
                    if (sha1 != game.downloads.client.sha1)
                    {
                        list.Add(new()
                        {
                            Url = game.downloads.client.url,
                            SHA1 = game.downloads.client.sha1,
                            Local = file,
                            Name = $"{obj.Version}.jar"
                        });
                    }
                }
            }, s_cancel));
        }

        //检查游戏资源文件
        if (ConfigUtils.Config.GameCheck.CheckAssets)
        {
            list1.Add(Task.Run(async () =>
            {
                ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.CheckAssets);
                var assets = game.GetIndex();
                if (assets == null)
                {
                    assets = await GameAPI.GetAssets(game.assetIndex.url);
                    if (assets == null)
                    {
                        ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.AssetsError);
                        throw new LaunchException(LaunchState.AssetsError,
                            LanguageHelper.Get("Core.Launch.Error2"));
                    }
                    game.AddIndex(assets);
                }

                var list1 = await assets.Check(s_cancel);
                foreach (var (Name, Hash) in list1)
                {
                    if (s_cancel.IsCancellationRequested)
                    {
                        return;
                    }
                    list.Add(new()
                    {
                        Overwrite = true,
                        Url = UrlHelper.DownloadAssets(Hash, BaseClient.Source),
                        SHA1 = Hash,
                        Local = $"{AssetsPath.ObjectsDir}/{Hash[..2]}/{Hash}",
                        Name = Name
                    });
                }
            }, s_cancel));
        }

        //检查运行库
        if (ConfigUtils.Config.GameCheck.CheckLib)
        {
            list1.Add(Task.Run(async () =>
            {
                ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.CheckLib);
                var list2 = await game.CheckGameLib(s_cancel);
                if (list2.Count != 0)
                {
                    ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.LostLib);
                    foreach (var item in list2)
                    {
                        list.Add(item);
                    }
                }

                //检查加载器运行库
                ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.CheckLoader);
                if (obj.Loader == Loaders.Forge || obj.Loader == Loaders.NeoForge)
                {
                    bool neo = obj.Loader == Loaders.NeoForge;
                    var list3 = await obj.CheckForgeLib(neo, s_cancel);
                    if (s_cancel.IsCancellationRequested)
                    {
                        return;
                    }
                    if (list3 == null)
                    {
                        ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.LostLoader);

                        var list4 = await GameDownloadHelper.DownloadForge(obj, neo);
                        if (list4.State != GetDownloadState.End)
                            throw new LaunchException(LaunchState.LostLoader,
                            LanguageHelper.Get("Core.Launch.Error3"));

                        list4.List!.ForEach(list.Add);
                    }
                    else
                    {
                        foreach (var item in list3)
                        {
                            list.Add(item);
                        }
                    }
                }
                else if (obj.Loader == Loaders.Fabric)
                {
                    var list3 = obj.CheckFabricLib(s_cancel);
                    if (s_cancel.IsCancellationRequested)
                    {
                        return;
                    }
                    if (list3 == null)
                    {
                        ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.LostLoader);

                        var list4 = await GameDownloadHelper.DownloadFabric(obj);
                        if (list4.State != GetDownloadState.End)
                            throw new LaunchException(LaunchState.LostLoader,
                            LanguageHelper.Get("Core.Launch.Error3"));

                        list4.List!.ForEach(list.Add);
                    }
                    else
                    {
                        foreach (var item in list3)
                        {
                            list.Add(item);
                        }
                    }
                }
                else if (obj.Loader == Loaders.Quilt)
                {
                    var list3 = obj.CheckQuiltLib(s_cancel);
                    if (s_cancel.IsCancellationRequested)
                    {
                        return;
                    }
                    if (list3 == null)
                    {
                        ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.LostLoader);

                        var list4 = await GameDownloadHelper.DownloadQuilt(obj);
                        if (list4.State != GetDownloadState.End)
                            throw new LaunchException(LaunchState.LostLoader,
                            LanguageHelper.Get("Core.Launch.Error3"));

                        list4.List!.ForEach(list.Add);
                    }
                    else
                    {
                        foreach (var item in list3)
                        {
                            list.Add(item);
                        }
                    }
                }

                //检查外置登录器
                ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.CheckLoginCore);

                if (login.AuthType == AuthType.Nide8)
                {
                    var item = await AuthlibHelper.ReadyNide8();
                    if (item != null)
                    {
                        list.Add(item);
                    }
                }
                else if (login.AuthType is AuthType.AuthlibInjector
                    or AuthType.LittleSkin or AuthType.SelfLittleSkin)
                {
                    var item = await AuthlibHelper.ReadyAuthlibInjector();
                    if (item != null)
                    {
                        list.Add(item);
                    }
                }
            }, s_cancel));
        }

        //检查整合包mod
        if (obj.ModPack && ConfigUtils.Config.GameCheck.CheckMod)
        {
            list1.Add(Task.Run(async () =>
            {
                ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.CheckMods);

                var mods = await obj.GetMods();
                ModObj? mod = null;
                int find = 0;
                ModInfoObj?[] array = obj.Mods.Values.ToArray();
                for (int a = 0; a < array.Length; a++)
                {
                    if (s_cancel.IsCancellationRequested)
                    {
                        return;
                    }

                    var item = array[a];
                    foreach (var item1 in mods)
                    {
                        if (item == null)
                            continue;
                        if (!ConfigUtils.Config.GameCheck.CheckModSha1)
                            continue;
                        if (item1.Sha1 == item.SHA1 ||
                            item1.Local.ToLower().EndsWith(item.File.ToLower()))
                        {
                            mod = item1;
                            break;
                        }
                    }
                    if (mod != null)
                    {
                        mods.Remove(mod);
                        find++;
                        mod = null;
                        array[a] = null;
                    }
                }
                if (find != array.Length)
                {
                    foreach (var item in array)
                    {
                        if (s_cancel.IsCancellationRequested)
                        {
                            return;
                        }
                        if (item == null)
                        {
                            continue;
                        }

                        list.Add(new()
                        {
                            Url = item.Url,
                            Name = item.File,
                            Local = obj.GetModsPath() + item.File,
                            SHA1 = item.SHA1
                        });
                    }
                }
            }, s_cancel));
        }

        await Task.WhenAll(list1.ToArray());

        return list;
    }



    /// <summary>
    /// V1版Jvm参数
    /// </summary>
    private readonly static List<string> V1JvmArg = new()
    {
        "-Djava.library.path=${natives_directory}", "-cp", "${classpath}"
    };

    /// <summary>
    /// 创建V2版Jvm参数
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns></returns>
    private static List<string> MakeV2JvmArg(GameSettingObj obj)
    {
        var game = VersionPath.GetGame(obj.Version)!;
        if (game.arguments == null)
            return V1JvmArg;

        List<string> arg = new();
        foreach (var item in game.arguments.jvm)
        {
            if (item is string)
            {
                arg.Add(item);
            }
            else if (item is JObject obj1)
            {
                var obj2 = obj1.ToObject<GameArgObj.Arguments.Jvm>();
                if (obj2 == null)
                {
                    continue;
                }
                if (!CheckRuleUtils.CheckAllow(obj2.rules))
                {
                    continue;
                }

                if (obj2.value is string item2)
                {
                    arg.Add(item2!);
                }
                else if (obj2.value is JArray)
                {
                    foreach (string item1 in obj2.value)
                    {
                        arg.Add(item1);
                    }
                }
            }
        }

        if (obj.Loader == Loaders.Forge || obj.Loader == Loaders.NeoForge)
        {
            var forge = obj.Loader == Loaders.NeoForge ?
                obj.GetNeoForgeObj()! : obj.GetForgeObj()!;
            if (forge.arguments.jvm != null)
            {
                foreach (var item in forge.arguments.jvm)
                {
                    arg.Add(item);
                }
            }
        }
        else if (obj.Loader == Loaders.Fabric)
        {
            var fabric = obj.GetFabricObj()!;
            foreach (var item in fabric.arguments.jvm)
            {
                arg.Add(item);
            }
        }

        return arg;
    }

    /// <summary>
    /// 创建V1版游戏参数
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>启动参数</returns>
    private static List<string> MakeV1GameArg(GameSettingObj obj)
    {
        if (obj.Loader == Loaders.Forge || obj.Loader == Loaders.NeoForge)
        {
            var forge = obj.Loader == Loaders.NeoForge ?
                obj.GetNeoForgeObj()! : obj.GetForgeObj()!;
            return new(forge.minecraftArguments.Split(" "));
        }

        var version = VersionPath.GetGame(obj.Version)!;
        return new(version.minecraftArguments.Split(" "));
    }

    /// <summary>
    /// 创建V2版游戏参数
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns></returns>
    private static List<string> MakeV2GameArg(GameSettingObj obj)
    {
        var game = VersionPath.GetGame(obj.Version)!;
        if (game.arguments == null)
            return MakeV1GameArg(obj);

        List<string> arg = new();
        foreach (var item in game.arguments.game)
        {
            if (item is string)
            {
                arg.Add(item);
            }
            //else if (item is JObject)
            //{
            //    JObject obj1 = item as JObject;
            //    var obj2 = obj1.ToObject<GameArgObj.Arguments.Jvm>();
            //    bool use = CheckRule.CheckAllow(obj2.rules);
            //    if (!use)
            //        continue;

            //    if (obj2.value is string)
            //    {
            //        string? item2 = obj2.value as string;
            //        arg.Append(item2).Append(' ');
            //    }
            //    else if (obj2.value is JArray)
            //    {
            //        foreach (string item1 in obj2.value)
            //        {
            //            arg.Append(item1).Append(' ');
            //        }
            //    }
            //}
        }

        //Mod加载器参数
        if (obj.Loader == Loaders.Forge || obj.Loader == Loaders.NeoForge)
        {
            var forge = obj.Loader == Loaders.NeoForge ? obj.GetNeoForgeObj()! : obj.GetForgeObj()!;
            foreach (var item in forge.arguments.game)
            {
                arg.Add(item);
            }
        }
        else if (obj.Loader == Loaders.Fabric)
        {
            var fabric = obj.GetFabricObj()!;
            foreach (var item in fabric.arguments.game)
            {
                arg.Add(item);
            }
        }
        else if (obj.Loader == Loaders.Quilt)
        {
            var quilt = obj.GetQuiltObj()!;
            foreach (var item in quilt.arguments.game)
            {
                arg.Add(item);
            }
        }

        return arg;
    }

    /// <summary>
    /// 创建Jvm参数
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="v2">V2模式</param>
    /// <param name="login">登录的账户</param>
    /// <returns>Jvm参数</returns>
    private static async Task<List<string>> JvmArg(GameSettingObj obj, bool v2, LoginObj login)
    {
        JvmArgObj args;

        if (obj.JvmArg == null)
        {
            args = ConfigUtils.Config.DefaultJvmArg;
        }
        else
        {
            args = new()
            {
                JvmArgs = obj.JvmArg.JvmArgs
                    ?? ConfigUtils.Config.DefaultJvmArg.JvmArgs,
                GCArgument = obj.JvmArg.GCArgument
                    ?? ConfigUtils.Config.DefaultJvmArg.GCArgument,
                GC = obj.JvmArg.GC
                    ?? ConfigUtils.Config.DefaultJvmArg.GC,
                JavaAgent = obj.JvmArg.JavaAgent
                    ?? ConfigUtils.Config.DefaultJvmArg.JavaAgent,
                MaxMemory = obj.JvmArg.MaxMemory
                    ?? ConfigUtils.Config.DefaultJvmArg.MaxMemory,
                MinMemory = obj.JvmArg.MinMemory
                    ?? ConfigUtils.Config.DefaultJvmArg.MinMemory
            };
        }

        List<string> jvmHead = new();

        //javaagent
        if (!string.IsNullOrWhiteSpace(args.JavaAgent))
        {
            jvmHead.Add($"-javaagent:{args.JavaAgent.Trim()}");
        }

        //gc
        switch (args.GC)
        {
            case GCType.G1GC:
                jvmHead.Add("-XX:+UseG1GC");
                break;
            case GCType.SerialGC:
                jvmHead.Add("-XX:+UseSerialGC");
                break;
            case GCType.ParallelGC:
                jvmHead.Add("-XX:+UseParallelGC");
                break;
            case GCType.CMSGC:
                jvmHead.Add("-XX:+UseConcMarkSweepGC");
                break;
            case GCType.User:
                if (!string.IsNullOrWhiteSpace(args.GCArgument))
                    jvmHead.Add(args.GCArgument.Trim());
                break;
            default:
                break;
        }

        //mem
        if (args.MinMemory != 0)
        {
            jvmHead.Add($"-Xms{args.MinMemory}m");
        }
        if (args.MaxMemory != 0)
        {
            jvmHead.Add($"-Xmx{args.MaxMemory}m");
        }
        if (!string.IsNullOrWhiteSpace(args.JvmArgs))
        {
            jvmHead.AddRange(args.JvmArgs.Split(";"));
        }

        //loader
        if (v2 && obj.Loader == Loaders.Forge || obj.Loader == Loaders.NeoForge)
        {
            jvmHead.Add($"-Dforgewrapper.librariesDir={LibrariesPath.BaseDir}");
            jvmHead.Add($"-Dforgewrapper.installer={(obj.Loader == Loaders.NeoForge ?
                GameHelper.BuildNeoForgeInster(obj.Version, obj.LoaderVersion!).Local :
                GameHelper.BuildForgeInster(obj.Version, obj.LoaderVersion!).Local)}");
            jvmHead.Add($"-Dforgewrapper.minecraft={LibrariesPath.GetGameFile(obj.Version)}");
        }

        //jvmHead.Add("-Djava.rmi.server.useCodebaseOnly=true");
        //jvmHead.Add("-XX:+UnlockExperimentalVMOptions");
        jvmHead.Add("-Dfml.ignoreInvalidMinecraftCertificates=true");
        jvmHead.Add("-Dfml.ignorePatchDiscrepancies=true");
        jvmHead.Add("-Dlog4j2.formatMsgNoLookups=true");
        //jvmHead.Add("-Dcom.sun.jndi.rmi.object.trustURLCodebase=false");
        //jvmHead.Add("-Dcom.sun.jndi.cosnaming.object.trustURLCodebase=false");
        //jvmHead.Add($"-Dminecraft.client.jar={VersionPath.BaseDir}/{obj.Version}.jar");

        jvmHead.AddRange(v2 ? MakeV2JvmArg(obj) : V1JvmArg);

        //auth
        if (login.AuthType == AuthType.Nide8)
        {
            jvmHead.Add($"-javaagent:{AuthlibHelper.NowNide8Injector}={login.Text1}");
            jvmHead.Add("-Dnide8auth.client=true");
        }
        else if (login.AuthType == AuthType.AuthlibInjector)
        {
            var res = await BaseClient.GetString(login.Text1);
            jvmHead.Add($"-javaagent:{AuthlibHelper.NowAuthlibInjector}={login.Text1}");
            jvmHead.Add($"-Dauthlibinjector.yggdrasil.prefetched={FuntionUtils.GenBase64(res.Item2!)}");
        }
        else if (login.AuthType == AuthType.LittleSkin)
        {
            var res = await BaseClient.GetString($"{UrlHelper.LittleSkin}api/yggdrasil");
            jvmHead.Add($"-javaagent:{AuthlibHelper.NowAuthlibInjector}={UrlHelper.LittleSkin}api/yggdrasil");
            jvmHead.Add($"-Dauthlibinjector.yggdrasil.prefetched={FuntionUtils.GenBase64(res.Item2!)}");
        }
        else if (login.AuthType == AuthType.SelfLittleSkin)
        {
            var res = await BaseClient.GetString($"{login.Text1}/api/yggdrasil");
            jvmHead.Add($"-javaagent:{AuthlibHelper.NowAuthlibInjector}={login.Text1}/api/yggdrasil");
            jvmHead.Add($"-Dauthlibinjector.yggdrasil.prefetched={FuntionUtils.GenBase64(res.Item2!)}");
        }

        return jvmHead;
    }

    /// <summary>
    /// 创建游戏启动参数
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="v2">V2模式</param>
    /// <returns>游戏启动参数</returns>
    private static List<string> GameArg(GameSettingObj obj, bool v2, WorldObj? world)
    {
        List<string> gameArg = new();
        gameArg.AddRange(v2 ? MakeV2GameArg(obj) : MakeV1GameArg(obj));

        //window
        WindowSettingObj window;
        if (obj.Window == null)
        {
            window = ConfigUtils.Config.Window;
        }
        else
        {
            window = new()
            {
                FullScreen = obj.Window.FullScreen
                    ?? ConfigUtils.Config.Window.FullScreen,
                Width = obj.Window.Width
                    ?? ConfigUtils.Config.Window.Width,
                Height = obj.Window.Height
                    ?? ConfigUtils.Config.Window.Height,
            };
        }
        if (window.FullScreen == true)
        {
            gameArg.Add("--fullscreen");
        }
        else
        {
            if (window.Width > 0)
            {
                gameArg.Add($"--width");
                gameArg.Add($"{window.Width}");
            }
            if (window.Height > 0)
            {
                gameArg.Add($"--height");
                gameArg.Add($"{window.Height}");
            }
        }

        //--quickPlayMultiplayer

        if (world != null)
        {
            gameArg.Add($"--quickPlaySingleplayer");
            gameArg.Add($"{world.LevelName}");
        }
        else
        {
            if (obj.StartServer != null && !string.IsNullOrWhiteSpace(obj.StartServer.IP)
                && obj.StartServer.Port != null)
            {
                if (CheckRuleUtils.IsGameLaunchVersion120(obj.Version))
                {
                    gameArg.Add($"--quickPlayMultiplayer");
                    if (obj.StartServer.Port > 0)
                    {
                        gameArg.Add($"{obj.StartServer.IP}:{obj.StartServer.Port}");
                    }
                    else
                    {
                        gameArg.Add($"{obj.StartServer.IP}:25565");
                    }
                }
                else
                {
                    gameArg.Add($"--server");
                    gameArg.Add(obj.StartServer.IP);
                    if (obj.StartServer.Port > 0)
                    {
                        gameArg.Add($"--port");
                        gameArg.Add(obj.StartServer.Port.ToString()!);
                    }
                }
            }
        }

        //proxy
        if (obj.ProxyHost != null)
        {
            if (!string.IsNullOrWhiteSpace(obj.ProxyHost.IP))
            {
                gameArg.Add($"--proxyHost");
                gameArg.Add(obj.ProxyHost.IP);
            }
            if (obj.ProxyHost.Port != null && obj.ProxyHost.Port != 0)
            {
                gameArg.Add($"--proxyPort");
                gameArg.Add(obj.ProxyHost.Port.ToString()!);
            }
            if (!string.IsNullOrWhiteSpace(obj.ProxyHost.User))
            {
                gameArg.Add($"--proxyUser");
                gameArg.Add(obj.ProxyHost.User);
            }
            if (!string.IsNullOrWhiteSpace(obj.ProxyHost.Password))
            {
                gameArg.Add($"--proxyPass");
                gameArg.Add(obj.ProxyHost.Password);
            }
        }
        else if (ConfigUtils.Config.Http.GameProxy)
        {
            if (!string.IsNullOrWhiteSpace(ConfigUtils.Config.Http.ProxyIP))
            {
                gameArg.Add($"--proxyHost");
                gameArg.Add(ConfigUtils.Config.Http.ProxyIP);
            }
            if (ConfigUtils.Config.Http.ProxyPort != 0)
            {
                gameArg.Add($"--proxyPort");
                gameArg.Add(ConfigUtils.Config.Http.ProxyPort.ToString());
            }
            if (!string.IsNullOrWhiteSpace(ConfigUtils.Config.Http.ProxyUser))
            {
                gameArg.Add($"--proxyUser");
                gameArg.Add(ConfigUtils.Config.Http.ProxyUser);
            }
            if (!string.IsNullOrWhiteSpace(ConfigUtils.Config.Http.ProxyPassword))
            {
                gameArg.Add($"--proxyPass");
                gameArg.Add(ConfigUtils.Config.Http.ProxyPassword);
            }
        }

        if (!string.IsNullOrWhiteSpace(obj.JvmArg?.GameArgs))
        {
            gameArg.AddRange(obj.JvmArg.GameArgs.Split("\n"));
        }

        return gameArg;
    }

    /// <summary>
    /// 添加获取更新
    /// </summary>
    /// <param name="dic">源字典</param>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    private static void AddOrUpdate(this Dictionary<LibVersionObj, string> dic,
        LibVersionObj key, string value)
    {
        foreach (var item in dic)
        {
            if (item.Key.Equals(key))
            {
                dic.Remove(item.Key);
                break;
            }
        }

        dic.Add(key, value);
    }

    /// <summary>
    /// 获取所有Lib
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="v2">V2模式</param>
    /// <returns>Lib地址列表</returns>
    private static async Task<List<string>> GetLibs(GameSettingObj obj, bool v2)
    {
        Dictionary<LibVersionObj, string> list = new();
        var version = VersionPath.GetGame(obj.Version)!;

        //GameLib
        var list1 = await GameHelper.MakeGameLibs(version);
        foreach (var item in list1)
        {
            if (item.Later == null)
            {
                list.AddOrUpdate(PathCUtils.MakeVersionObj(item.Name), item.Local);
            }
        }

        //LoaderLib
        if (obj.Loader == Loaders.Forge || obj.Loader == Loaders.NeoForge)
        {
            var forge = obj.Loader == Loaders.NeoForge ?
                obj.GetNeoForgeObj()! : obj.GetForgeObj()!;

            var list2 = GameHelper.MakeForgeLibs(forge, obj.Version, obj.LoaderVersion!,
                obj.Loader == Loaders.NeoForge);

            list2.ForEach(a => list.AddOrUpdate(PathCUtils.MakeVersionObj(a.Name), a.Local));

            if (v2)
            {
                list.AddOrUpdate(new(), GameHelper.ForgeWrapper);
            }
        }
        else if (obj.Loader == Loaders.Fabric)
        {
            var fabric = obj.GetFabricObj()!;
            foreach (var item in fabric.libraries)
            {
                var name = PathCUtils.ToName(item.name);
                list.AddOrUpdate(PathCUtils.MakeVersionObj(name.Name),
                    Path.GetFullPath($"{LibrariesPath.BaseDir}/{name.Path}"));
            }
        }
        else if (obj.Loader == Loaders.Quilt)
        {
            var quilt = obj.GetQuiltObj()!;
            foreach (var item in quilt.libraries)
            {
                var name = PathCUtils.ToName(item.name);
                list.AddOrUpdate(PathCUtils.MakeVersionObj(name.Name),
                    Path.GetFullPath($"{LibrariesPath.BaseDir}/{name.Path}"));
            }
        }

        //游戏核心
        var list3 = new List<string>(list.Values);
        if (obj.Loader != Loaders.NeoForge)
        {
            list3.Add(LibrariesPath.GetGameFile(obj.Version));
        }

        return list3;
    }

    /// <summary>
    /// 替换参数
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="login">登录的账户</param>
    /// <param name="all_arg">参数</param>
    /// <param name="v2">V2模式</param>
    private static async Task ReplaceAll(GameSettingObj obj, LoginObj login, List<string> all_arg, bool v2)
    {
        var version = VersionPath.GetGame(obj.Version)!;
        string assetsPath = AssetsPath.BaseDir;
        string gameDir = InstancesPath.GetGamePath(obj);
        string assetsIndexName;
        if (version.assets != null)
        {
            assetsIndexName = version.assets;
        }
        else
        {
            assetsIndexName = "legacy";
        }

        string version_name = obj.Loader switch
        {
            Loaders.Forge => $"forge-{obj.Version}-{obj.LoaderVersion}",
            Loaders.Fabric => $"fabric-{obj.Version}-{obj.LoaderVersion}",
            Loaders.Quilt => $"quilt-{obj.Version}-{obj.LoaderVersion}",
            _ => obj.Version
        };
        var libraries = await GetLibs(obj, v2);
        StringBuilder classpath = new();
        string sep = SystemInfo.Os == OsType.Windows ? ";" : ":";
        ColorMCCore.GameLog?.Invoke(obj, LanguageHelper.Get("Core.Launch.Info2"));

        //去除重复的classpath
        if (!string.IsNullOrWhiteSpace(obj.AdvanceJvm?.ClassPath))
        {
            var list = obj.AdvanceJvm.ClassPath.Split(";");
            foreach (var item1 in list)
            {
                var path = Path.GetFullPath(item1);
                if (File.Exists(path))
                {
                    libraries.Add(item1);
                }
            }
        }

        //添加lib路径到classpath
        foreach (var item in libraries)
        {
            classpath.Append($"{item}{sep}");
            ColorMCCore.GameLog?.Invoke(obj, $"    {item}");
        }
        classpath.Remove(classpath.Length - 1, 1);

        Dictionary<string, string> argDic = new()
        {
            {"${auth_player_name}", login.UserName },
            {"${version_name}",version_name },
            {"${game_directory}",gameDir },
            {"${assets_root}",assetsPath },
            {"${assets_index_name}",assetsIndexName },
            {"${auth_uuid}",login.UUID },
            {"${auth_access_token}",login.AccessToken },
            {"${game_assets}",assetsPath },
            {"${user_properties}", "{}" },
            {"${user_type}", login.AuthType == AuthType.OAuth ? "msa" : "legacy" },
            {"${version_type}", "ColorMC" },
            {"${natives_directory}", LibrariesPath.GetNativeDir(obj.Version) },
            {"${library_directory}",LibrariesPath.BaseDir },
            {"${classpath_separator}", sep },
            {"${launcher_name}","ColorMC" },
            {"${launcher_version}", ColorMCCore.Version  },
            {"${classpath}",  classpath.ToString().Trim() },
        };

        for (int a = 0; a < all_arg.Count; a++)
        {
            all_arg[a] = argDic.Aggregate(all_arg[a], (a, b) => a.Replace(b.Key, b.Value));
        }
    }

    /// <summary>
    /// 创建所有启动参数
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="login">登录的账户</param>
    /// <returns></returns>
    private static async Task<List<string>> MakeArg(GameSettingObj obj, LoginObj login, WorldObj? world)
    {
        var list = new List<string>();
        var version = VersionPath.GetGame(obj.Version)!;
        var v2 = CheckRuleUtils.GameLaunchVersion(version);

        list.AddRange(await JvmArg(obj, v2, login));

        if (string.IsNullOrWhiteSpace(obj.AdvanceJvm?.MainClass))
        {
            if (obj.Loader == Loaders.Normal)
            {
                list.Add(version.mainClass);
            }
            //forgewrapper
            else if (obj.Loader == Loaders.Forge || obj.Loader == Loaders.NeoForge)
            {
                if (v2)
                {
                    list.Add("io.github.zekerzhayard.forgewrapper.installer.Main");
                }
                else
                {
                    var forge = obj.Loader == Loaders.NeoForge ? obj.GetNeoForgeObj()! : obj.GetForgeObj()!;
                    list.Add(forge.mainClass);
                }
            }
            else if (obj.Loader == Loaders.Fabric)
            {
                var fabric = obj.GetFabricObj()!;
                list.Add(fabric.mainClass);
            }
            else if (obj.Loader == Loaders.Quilt)
            {
                var quilt = obj.GetQuiltObj()!;
                list.Add(quilt.mainClass);
            }
        }
        else
        {
            list.Add(obj.AdvanceJvm.MainClass);
        }
        list.AddRange(GameArg(obj, v2, world));

        await ReplaceAll(obj, login, list, v2);

        return list;
    }

    /// <summary>
    /// 进程日志
    /// </summary>
    private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        ColorMCCore.ProcessLog?.Invoke(sender as Process, e.Data);
    }

    /// <summary>
    /// 进程日志
    /// </summary>
    private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        ColorMCCore.ProcessLog?.Invoke(sender as Process, e.Data);
    }

    /// <summary>
    /// 启动游戏
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="login">登录的账户</param>
    /// <param name="jvmCfg">使用的Java</param>
    /// <exception cref="LaunchException">启动错误</exception>
    /// <returns></returns>
    public static async Task<Process?> StartGame(this GameSettingObj obj, LoginObj login, WorldObj? world, CancellationToken token)
    {
        s_cancel = token;
        Stopwatch stopwatch = new();

        if (string.IsNullOrWhiteSpace(obj.Version) ||
            (obj.Loader != Loaders.Normal && string.IsNullOrWhiteSpace(obj.LoaderVersion)))
        {
            throw new LaunchException(LaunchState.VersionError, LanguageHelper.Get("Core.Launch.Error7"));
        }

        //登录账户
        stopwatch.Restart();
        stopwatch.Start();
        ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.Login);
        var (State, State1, Obj, Message, Ex) = await login.RefreshToken();
        if (State1 != LoginState.Done)
        {
            if (login.AuthType == AuthType.OAuth
                && !string.IsNullOrWhiteSpace(login.UUID)
                && ColorMCCore.OfflineLaunch != null
                && await ColorMCCore.OfflineLaunch(login) == true)
            {
                login = new()
                {
                    UserName = login.UserName,
                    UUID = login.UUID,
                    AuthType = AuthType.Offline
                };
            }
            else
            {
                ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.LoginFail);
                if (Ex != null)
                    throw new LaunchException(LaunchState.LoginFail, Message!, Ex);

                throw new LaunchException(LaunchState.LoginFail, Message!);
            }
        }
        else
        {
            login = Obj!;
            login.Save();
        }

        if (s_cancel.IsCancellationRequested)
        {
            return null;
        }

        stopwatch.Stop();
        string temp = string.Format(LanguageHelper.Get("Core.Launch.Info4"),
            obj.Name, stopwatch.Elapsed.ToString());
        ColorMCCore.GameLog?.Invoke(obj, temp);
        Logs.Info(temp);

        stopwatch.Restart();
        stopwatch.Start();
        //检查游戏文件
        var res = await CheckGameFile(obj, login);
        stopwatch.Stop();
        temp = string.Format(LanguageHelper.Get("Core.Launch.Info5"),
            obj.Name, stopwatch.Elapsed.ToString());
        ColorMCCore.GameLog?.Invoke(obj, temp);
        Logs.Info(temp);

        //下载缺失的文件
        if (!res.IsEmpty)
        {
            bool download = true;
            if (!ConfigUtils.Config.Http.AutoDownload)
            {
                if (ColorMCCore.GameDownload == null)
                {
                    throw new LaunchException(LaunchState.LostGame,
                        LanguageHelper.Get("Core.Launch.Error4"));
                }

                download = await ColorMCCore.GameDownload.Invoke(LaunchState.LostFile, obj);
            }

            if (download)
            {
                ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.Download);

                stopwatch.Restart();
                stopwatch.Start();

                var ok = await DownloadManager.Start(res.ToList());
                if (!ok)
                {
                    throw new LaunchException(LaunchState.LostFile,
                        LanguageHelper.Get("Core.Launch.Error5"));
                }
                stopwatch.Stop();
                temp = string.Format(LanguageHelper.Get("Core.Launch.Info7"),
                    obj.Name, stopwatch.Elapsed.ToString());
                ColorMCCore.GameLog?.Invoke(obj, temp);
                Logs.Info(temp);
            }
        }

        stopwatch.Restart();
        stopwatch.Start();

        var path = obj.JvmLocal;
        JavaInfo? jvm = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            var game = VersionPath.GetGame(obj.Version)!;
            var jv = game.javaVersion.majorVersion;
            jvm = JvmPath.GetInfo(obj.JvmName) ?? JvmPath.FindJava(jv);
            if (jvm == null)
            {
                ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.JavaError);
                ColorMCCore.NoJava?.Invoke();
                throw new LaunchException(LaunchState.JavaError,
                        LanguageHelper.Get("Core.Launch.Error6"));
            }

            path = jvm.GetPath();
        }

        if (s_cancel.IsCancellationRequested)
        {
            return null;
        }

        //准备Jvm参数
        ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.JvmPrepare);

        var arg = await MakeArg(obj, login, world);
        ColorMCCore.GameLog?.Invoke(obj, LanguageHelper.Get("Core.Launch.Info1"));
        bool hidenext = false;
        foreach (var item in arg)
        {
            if (hidenext)
            {
                hidenext = false;
                ColorMCCore.GameLog?.Invoke(obj, "****************");
            }
            else
            {
                ColorMCCore.GameLog?.Invoke(obj, item);
            }
            var low = item.ToLower();
            if (low.StartsWith("--uuid") || low.StartsWith("--accesstoken"))
            {
                hidenext = true;
            }
        }

        ColorMCCore.GameLog?.Invoke(obj, LanguageHelper.Get("Core.Launch.Info3"));
        ColorMCCore.GameLog?.Invoke(obj, path);

        if (s_cancel.IsCancellationRequested)
        {
            return null;
        }

        //启动前运行
        if (ColorMCCore.LaunchP != null && (obj.JvmArg?.LaunchPre == true
            || ConfigUtils.Config.DefaultJvmArg.LaunchPre))
        {
            var start = obj.JvmArg?.LaunchPreData;
            if (string.IsNullOrWhiteSpace(start))
            {
                start = ConfigUtils.Config.DefaultJvmArg.LaunchPreData;
            }
            if (!string.IsNullOrWhiteSpace(start))
            {
                var res1 = await ColorMCCore.LaunchP.Invoke(true);
                if (res1)
                {
                    stopwatch.Start();
                    ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.LaunchPre);
                    start = ReplaceArg(obj, path, arg, start);
                    CmdRun(obj, start);
                    stopwatch.Stop();
                    string temp1 = string.Format(LanguageHelper.Get("Core.Launch.Info8"),
                        obj.Name, stopwatch.Elapsed.ToString());
                    ColorMCCore.GameLog?.Invoke(obj, temp1);
                    Logs.Info(temp1);
                }
            }
            else
            {
                string temp2 = string.Format(LanguageHelper.Get("Core.Launch.Info10"),
                obj.Name);
                ColorMCCore.GameLog?.Invoke(obj, temp2);
                Logs.Info(temp2);
            }
        }

        if (s_cancel.IsCancellationRequested)
        {
            return null;
        }

        if (SystemInfo.Os == OsType.Android)
        {
            NativeLaunch(jvm!, arg);
            return null;
        }

        //启动进程
        Process process = new()
        {
            EnableRaisingEvents = true
        };
        process.StartInfo.FileName = path;
        process.StartInfo.WorkingDirectory = obj.GetGamePath();
        Directory.CreateDirectory(process.StartInfo.WorkingDirectory);
        foreach (var item in arg)
        {
            process.StartInfo.ArgumentList.Add(item);
        }

        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.OutputDataReceived += Process_OutputDataReceived;
        process.ErrorDataReceived += Process_ErrorDataReceived;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        stopwatch.Stop();
        temp = string.Format(LanguageHelper.Get("Core.Launch.Info6"),
            obj.Name, stopwatch.Elapsed.ToString());
        ColorMCCore.GameLog?.Invoke(obj, temp);
        Logs.Info(temp);

        //启动后执行
        if (ColorMCCore.LaunchP != null && (obj.JvmArg?.LaunchPost == true
            || ConfigUtils.Config.DefaultJvmArg.LaunchPost))
        {
            var start = obj.JvmArg?.LaunchPostData;
            if (string.IsNullOrWhiteSpace(start))
            {
                start = ConfigUtils.Config.DefaultJvmArg.LaunchPostData;
            }
            if (!string.IsNullOrWhiteSpace(start))
            {
                var res1 = await ColorMCCore.LaunchP.Invoke(false);
                if (res1)
                {
                    stopwatch.Start();
                    ColorMCCore.GameLaunch?.Invoke(obj, LaunchState.LaunchPost);
                    start = ReplaceArg(obj, path, arg, start);
                    CmdRun(obj, start);
                    stopwatch.Stop();
                    string temp1 = string.Format(LanguageHelper.Get("Core.Launch.Info9"),
                        obj.Name, stopwatch.Elapsed.ToString());
                    ColorMCCore.GameLog?.Invoke(obj, temp1);
                    Logs.Info(temp1);
                }
            }
            else
            {
                string temp2 = string.Format(LanguageHelper.Get("Core.Launch.Info11"),
                obj.Name);
                ColorMCCore.GameLog?.Invoke(obj, temp2);
                Logs.Info(temp2);
            }
        }

        return process;
    }

    public delegate int DLaunch(int argc,
        string[] argv, /* main argc, argc */
        int jargc, string[] jargv,          /* java args */
        int appclassc, string[] appclassv,  /* app classpath */
        string fullversion,                 /* full version defined */
        string dotversion,                  /* dot version defined */
        string pname,                       /* program name */
        string lname,                       /* launcher name */
        bool javaargs,                      /* JAVA_ARGS */
        bool cpwildcard,                    /* classpath wildcard*/
        bool javaw,                         /* windows-only javaw */
        int ergo                            /* ergonomics class policy */
    );

    private static int s_argLength;
    private static string[] s_args;

    /// <summary>
    /// native启动
    /// </summary>
    /// <param name="info">Java信息</param>
    /// <param name="args">启动参数</param>
    public static void NativeLaunch(JavaInfo info, List<string> args)
    {
        var info1 = new FileInfo(info.Path);
        var path = info1.Directory?.Parent?.FullName;

        var local = path + SystemInfo.Os switch
        {
            OsType.Windows => "/bin/jli.dll",
            OsType.Linux => "/lib/libjli.so",
            OsType.MacOS => "/lib/libjli.dylib",
            OsType.Android => "/lib/libjli.so",
            _ => throw new NotImplementedException(),
        };
        if (File.Exists(local))
        {
            local = Path.GetFullPath(local);
        }

        //加载运行库
        var temp = NativeLoader.Loader.LoadLibrary(local);
        var temp1 = NativeLoader.Loader.GetProcAddress(temp, "JLI_Launch", false);
        var inv = Marshal.GetDelegateForFunctionPointer<DLaunch>(temp1);

        //Environment.SetEnvironmentVariable("JAVA_HOME", path);
        //Environment.SetEnvironmentVariable("PATH", path + "/bin:" + path);
        //Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", path + "/lib:" + path + "/bin");

        var args1 = new string[args.Count + 1];
        args1[0] = "java";

        for (int i = 1; i < args1.Length; i++)
        {
            args1[i] = args[i - 1];
        }

        s_argLength = args1.Length;
        s_args = args1;

        //启动游戏
        new Thread(() =>
        {
            try
            {
                var res = inv(s_argLength, s_args, 0, null, 0, null,
                    "", "", "", "", false, false, true, 0);

                var res1 = NativeLoader.Loader.CloseLibrary(temp);
            }
            catch (Exception e)
            {
                ColorMCCore.OnError?.Invoke("Error", e, false);
            }
        })
        {
            Name = "ColorMC_Game"
        }.Start();
    }
}
