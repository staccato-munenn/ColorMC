﻿using ColorMC.Core.Net;
using ColorMC.Core.Objs;
using ColorMC.Core.Objs.Loader;
using ColorMC.Core.Objs.Minecraft;
using ColorMC.Core.Utils;
using Newtonsoft.Json;

namespace ColorMC.Core.LaunchPath;

public static class VersionPath
{
    public static VersionObj? Versions { get; private set; }

    public static string ForgeDir => BaseDir + "/" + Name1;
    public static string FabricDir => BaseDir + "/" + Name2;
    public static string QuiltDir => BaseDir + "/" + Name3;


    private const string Name = "versions";

    private const string Name1 = "forge";
    private const string Name2 = "fabric";
    private const string Name3 = "quilt";

    public static string BaseDir { get; private set; }

    public static void Init(string dir)
    {
        BaseDir = dir + "/" + Name;

        Logs.Info(LanguageHelper.GetName("Core.Path.Version.Load"));

        Directory.CreateDirectory(BaseDir);

        Directory.CreateDirectory(ForgeDir);
        Directory.CreateDirectory(FabricDir);
        Directory.CreateDirectory(QuiltDir);

        try
        {
            if (!GetFromWeb().Result || !ReadVersions())
            {
                Logs.Error(LanguageHelper.GetName("Core.Path.Version.Load.Error2"));
            }
            else
            {
                SaveVersions(Versions);
            }
        }
        catch (Exception e)
        {
            Logs.Error(LanguageHelper.GetName("Core.Path.Version.Load.Error3"), e);
        }
    }

    public static async Task<bool> GetFromWeb()
    {
        Versions = await Get.GetVersions();
        if (Versions == null)
        {
            Versions = await Get.GetVersions(SourceLocal.Offical);
            if (Versions == null)
            {
                Logs.Warn(LanguageHelper.GetName("Core.Path.Version.Load.Error4"));
                return false;
            }
        }

        return Versions != null;
    }

    public static bool Have(string version)
    {
        return Versions?.versions.Where(a => a.id == version).Any() == true;
    }

    public static bool ReadVersions()
    {
        string file = BaseDir + "/version.json";
        if (File.Exists(file))
        {
            string data = File.ReadAllText(file);
            Versions = JsonConvert.DeserializeObject<VersionObj>(data);
            return Versions != null;
        }
        return false;
    }

    public static void SaveVersions(VersionObj? obj)
    {
        if (obj == null)
            return;
        string file = BaseDir + "/version.json";
        File.WriteAllText(file, JsonConvert.SerializeObject(obj));
    }

    public static void AddGame(GameArgObj? obj)
    {
        if (obj == null)
            return;
        string file = $"{BaseDir}/{obj.id}.json";
        File.WriteAllText(file, JsonConvert.SerializeObject(obj));
    }

    public static GameArgObj? GetGame(string version)
    {
        string file = $"{BaseDir}/{version}.json";

        if (!File.Exists(file))
            return null;

        return JsonConvert.DeserializeObject<GameArgObj>(File.ReadAllText(file));
    }

    /// <summary>
    /// 更新json
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    public static async Task CheckUpdate(string version)
    {
        if (Versions == null)
            return;

        var data = Versions.versions.Where(a => a.id == version).FirstOrDefault();
        if (data != null)
        {
            AddGame(await Get.GetGame(data.url));
        }
    }

    public static ForgeInstallObj? GetForgeInstallObj(GameSettingObj obj)
    {
        return GetForgeInstallObj(obj.Version, obj.LoaderVersion);
    }

    public static ForgeInstallObj? GetForgeInstallObj(string mc, string version)
    {
        string file = $"{BaseDir}/{Name1}/forge-{mc}-{version}-install.json";

        if (!File.Exists(file))
            return null;

        return JsonConvert.DeserializeObject<ForgeInstallObj>(File.ReadAllText(file));
    }

    public static ForgeLaunchObj? GetForgeObj(GameSettingObj obj)
    {
        return GetForgeObj(obj.Version, obj.LoaderVersion);
    }

    public static ForgeLaunchObj? GetForgeObj(string mc, string version)
    {
        string file = $"{BaseDir}/{Name1}/forge-{mc}-{version}.json";

        if (!File.Exists(file))
            return null;

        return JsonConvert.DeserializeObject<ForgeLaunchObj>(File.ReadAllText(file));
    }

    public static FabricLoaderObj? GetFabricObj(GameSettingObj obj)
    {
        return GetFabricObj(obj.Version, obj.LoaderVersion);
    }

    public static FabricLoaderObj? GetFabricObj(string mc, string version)
    {
        string file = $"{BaseDir}/{Name2}/fabric-loader-{version}-{mc}.json";

        if (!File.Exists(file))
            return null;

        return JsonConvert.DeserializeObject<FabricLoaderObj>(File.ReadAllText(file));
    }

    public static QuiltLoaderObj? GetQuiltObj(GameSettingObj obj)
    {
        return GetQuiltObj(obj.Version, obj.LoaderVersion);
    }

    public static QuiltLoaderObj? GetQuiltObj(string mc, string version)
    {
        string file = $"{BaseDir}/{Name3}/quilt-loader-{version}-{mc}.json";

        if (!File.Exists(file))
            return null;

        return JsonConvert.DeserializeObject<QuiltLoaderObj>(File.ReadAllText(file));
    }
}
