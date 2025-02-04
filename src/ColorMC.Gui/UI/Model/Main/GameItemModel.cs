﻿using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ColorMC.Core.LaunchPath;
using ColorMC.Core.Objs;
using ColorMC.Gui.UI.Flyouts;
using ColorMC.Gui.UI.Windows;
using ColorMC.Gui.UIBinding;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.IO;

namespace ColorMC.Gui.UI.Model.Main;

public partial class GameItemModel : GameModel
{
    [ObservableProperty]
    private bool _isSelect;
    [ObservableProperty]
    private bool _isLaunch;
    [ObservableProperty]
    private bool _isLoad;
    [ObservableProperty]
    private bool _isDrop;

    [ObservableProperty]
    private string _tips;

    [ObservableProperty]
    private TextWrapping _wrap = TextWrapping.NoWrap;
    [ObservableProperty]
    private TextTrimming _trim = TextTrimming.CharacterEllipsis;

    private readonly IMainTop _top;

    public string Name => Obj.Name;
    public Bitmap Pic { get; }

    public GameItemModel(IUserControl con, IMainTop top, GameSettingObj obj) : base(con, obj)
    {
        _top = top;
        Pic = GetImage();
    }

    partial void OnIsSelectChanged(bool value)
    {
        Wrap = value ? TextWrapping.Wrap : TextWrapping.NoWrap;
        Trim = value ? TextTrimming.None : TextTrimming.CharacterEllipsis;
        IsDrop = false;
    }

    public void Reload()
    {
        IsLaunch = BaseBinding.IsGameRun(Obj);

        SetTips();
    }

    public void SetTips()
    {
        Tips = string.Format(App.GetLanguage("Tips.Text1"),
            Obj.LaunchData.AddTime.Ticks == 0 ? "" : Obj.LaunchData.AddTime.ToString(),
            Obj.LaunchData.LastTime.Ticks == 0 ? "" : Obj.LaunchData.LastTime.ToString(),
            Obj.LaunchData.LastPlay.Ticks == 0 ? "" :
            $"{Obj.LaunchData.LastPlay.TotalHours:#}:{Obj.LaunchData.LastPlay.Minutes:00}:{Obj.LaunchData.LastPlay.Seconds:00}",
            Obj.LaunchData.GameTime.Ticks == 0 ? "" :
            $"{Obj.LaunchData.GameTime.TotalHours:#}:{Obj.LaunchData.GameTime.Minutes:00}:{Obj.LaunchData.GameTime.Seconds:00}");
    }

    public async void Move(PointerEventArgs e)
    {
        var dragData = new DataObject();
        dragData.Set(BaseBinding.DrapType, this);
        IsDrop = true;

        if (Window is TopLevel top)
        {
            List<IStorageFolder> files = new();
            var item = await top.StorageProvider
                   .TryGetFolderFromPathAsync(Obj.GetBasePath());
            files.Add(item!);
            dragData.Set(DataFormats.Files, files);
        }

        Dispatcher.UIThread.Post(() =>
        {
            DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move | DragDropEffects.Link | DragDropEffects.Copy);
        });
    }

    public void SetSelect()
    {
        _top.Select(this);
    }

    public void Flyout(Control con)
    {
        _ = new MainFlyout(con, this);
    }

    public void Launch()
    {
        if (IsLaunch)
        {
            return;
        }

        _top.Launch(this);
    }

    private Bitmap GetImage()
    {
        var file = Obj.GetIconFile();
        if (File.Exists(file))
        {
            return new Bitmap(file);
        }
        else
        {
            return App.GameIcon;
        }
    }

    public async void Rename()
    {
        var (Cancel, Text1, _) = await ShowEdit(App.GetLanguage("MainWindow.Info23"), Obj.Name);
        if (Cancel)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(Text1))
        {
            Show(App.GetLanguage("MainWindow.Error3"));
            return;
        }

        GameBinding.SetGameName(Obj, Text1);
    }

    public async void Copy()
    {
        var (Cancel, Text1, _) = await ShowEdit(App.GetLanguage("MainWindow.Info23"),
            Obj.Name + App.GetLanguage("MainWindow.Info24"));
        if (Cancel)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(Text1))
        {
            Show(App.GetLanguage("MainWindow.Error3"));
            return;
        }

        var res = await GameBinding.CopyGame(Obj, Text1);
        if (!res)
        {
            Show(App.GetLanguage("MainWindow.Error5"));
            return;
        }
        else
        {
            Notify(App.GetLanguage("MainWindow.Info25"));
        }
    }

    public async void DeleteGame()
    {
        var res = await ShowWait(string.Format(App.GetLanguage("MainWindow.Info19"), Obj.Name));
        if (!res)
        {
            return;
        }

        res = await GameBinding.DeleteGame(Obj);
        if (!res)
        {
            Show(App.GetLanguage("MainWindow.Info37"));
        }
    }

    public void EditGroup()
    {
        _top.EditGroup(this);
    }

    public override void Close()
    {
        Pic.Dispose();
    }
}
