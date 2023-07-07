﻿using Avalonia.Controls;
using Avalonia.Input;
using ColorMC.Core.LaunchPath;
using ColorMC.Core.Objs;
using ColorMC.Gui.UI.Windows;
using ColorMC.Gui.UIBinding;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace ColorMC.Gui.UI.Model.GameEdit;

public partial class GameEditTab5Model : GameEditTabModel, ILoadFuntion<WorldModel>
{
    public ObservableCollection<WorldModel> WorldList { get; init; } = new();

    private WorldModel? Last;

    public GameEditTab5Model(IUserControl con, GameSettingObj obj) : base(con, obj)
    {

    }

    [RelayCommand]
    public async Task Backup()
    {
        var window = Con.Window;
        var info = new DirectoryInfo(Obj.GetWorldBackupPath());
        if (!info.Exists)
        {
            info.Create();
        }

        var list = info.GetFiles();
        var names = new List<string>();
        foreach (var item in list)
        {
            names.Add(item.Name);
        }
        await window.ComboInfo.Show(App.GetLanguage("GameEditWindow.Tab5.Info9"), names);
        if (window.ComboInfo.Cancel)
            return;
        var item1 = list[window.ComboInfo.Read().Item1];
        var res = await window.OkInfo.ShowWait(App.GetLanguage("GameEditWindow.Tab5.Info10"));
        if (!res)
            return;

        window.ProgressInfo.Show(App.GetLanguage("GameEditWindow.Tab5.Info11"));
        res = await GameBinding.BackupWorld(Obj, item1);
        window.ProgressInfo.Close();
        if (!res)
        {
            window.OkInfo.Show(App.GetLanguage("GameEditWindow.Tab5.Error4"));
        }
        else
        {
            window.NotifyInfo.Show(App.GetLanguage("GameEditWindow.Tab5.Info12"));
            await Load();
        }
    }
    [RelayCommand]
    public async Task Import()
    {
        var window = Con.Window;
        var file = await GameBinding.AddFile(window as Window, Obj, FileType.World);
        if (file == null)
            return;

        if (file == false)
        {
            window.NotifyInfo.Show(App.GetLanguage("GameEditWindow.Tab5.Error2"));
            return;
        }

        window.NotifyInfo.Show(App.GetLanguage("GameEditWindow.Tab4.Info2"));
        await Load();
    }
    [RelayCommand]
    public void Open()
    {
        BaseBinding.OpPath(Obj.GetSavesPath());
    }
    [RelayCommand]
    public void Add()
    {
        App.ShowAdd(Obj, FileType.World);
    }

    [RelayCommand]
    public void OpenBackup()
    {
        BaseBinding.OpPath(Obj.GetWorldBackupPath());
    }
    [RelayCommand]
    public async Task Load()
    {
        var window = Con.Window;
        window.ProgressInfo.Show(App.GetLanguage("GameEditWindow.Tab5.Info5"));
        WorldList.Clear();

        var res = await GameBinding.GetWorlds(Obj!);
        window.ProgressInfo.Close();
        foreach (var item in res)
        {
            WorldList.Add(new(Con, this, item));
        }
    }

    public async void Drop(IDataObject data)
    {
        var res = await GameBinding.AddFile(Obj, data, FileType.World);
        if (res)
        {
            await Load();
        }
    }

    public void SetSelect(WorldModel item)
    {
        if (Last != null)
        {
            Last.IsSelect = false;
        }
        Last = item;
        Last.IsSelect = true;
    }
}
