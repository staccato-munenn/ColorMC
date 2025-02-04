﻿using Avalonia.Controls;
using ColorMC.Core.Objs.Minecraft;
using ColorMC.Core.Utils;
using ColorMC.Gui.Objs;
using ColorMC.Gui.UI.Model;
using ColorMC.Gui.UI.Model.GameEdit;
using ColorMC.Gui.UIBinding;
using System;

namespace ColorMC.Gui.UI.Flyouts;

public class GameEditFlyout2
{
    private readonly WorldModel _model;

    public GameEditFlyout2(Control con, WorldModel model)
    {
        _model = model;

        _ = new FlyoutsControl(new()
        {
            (App.GetLanguage("Button.OpFile"), true, Button1_Click),
            (App.GetLanguage("GameEditWindow.Flyouts2.Text5"), CheckRuleUtils.IsGameLaunchVersion120(_model.World.Game.Version), Button6_Click),
            (App.GetLanguage("GameEditWindow.Flyouts2.Text1"), true, Button2_Click),
            (App.GetLanguage("GameEditWindow.Flyouts2.Text4"), true, Button5_Click),
            (App.GetLanguage("GameEditWindow.Flyouts2.Text3"), !_model.World.Broken, Button3_Click),
            (App.GetLanguage("GameEditWindow.Flyouts2.Text2"), !_model.World.Broken, Button4_Click)
        }, con);
    }

    private void Button6_Click()
    {
        _model.Top.Launch(_model);
    }

    private void Button5_Click()
    {
        App.ShowConfigEdit(_model.World);
    }

    private void Button4_Click()
    {
        _model.Top.Delete(_model);
    }

    private void Button3_Click()
    {
        _model.Top.Backup(_model);
    }

    private void Button2_Click()
    {
        _model.Top.Export(_model);
    }

    private void Button1_Click()
    {
        PathBinding.OpPath(_model.World);
    }
}
