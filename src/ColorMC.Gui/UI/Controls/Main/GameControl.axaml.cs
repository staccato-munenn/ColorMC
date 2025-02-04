using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ColorMC.Gui.UI.Model.Main;
using System;
using System.ComponentModel;

namespace ColorMC.Gui.UI.Controls.Main;

public partial class GameControl : UserControl
{
    public GameItemModel GameModel { get; private set; }

    public GameControl()
    {
        InitializeComponent();

        PointerEntered += GameControl_PointerEntered;
        PointerExited += GameControl_PointerExited;

        PointerPressed += GameControl_PointerPressed;
        PointerReleased += GameControl_PointerReleased;
        PointerMoved += GameControl_PointerMoved;
        DoubleTapped += GameControl_DoubleTapped;

        DataContextChanged += GameControl_DataContextChanged;
    }

    private void GameControl_DataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is GameItemModel mo)
        {
            GameModel = mo;
        }
    }

    private void GameModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "IsDrop")
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (GameModel.IsDrop == true)
                {
                    RenderTransform = new ScaleTransform(0.95, 0.95);
                }
                else
                {
                    RenderTransform = null;
                }
            });
        }
    }

    private void GameControl_PointerExited(object? sender, PointerEventArgs e)
    {
        Rectangle2.IsVisible = false;
    }

    private void GameControl_PointerEntered(object? sender, PointerEventArgs e)
    {
        Rectangle2.IsVisible = true;
        GameModel.SetTips();
    }

    private Point point;

    private void GameControl_PointerMoved(object? sender, PointerEventArgs e)
    {
        var pro = e.GetCurrentPoint(this);
        if (pro.Properties.IsLeftButtonPressed)
        {
            var pos = pro.Position;
            if (Math.Sqrt(Math.Pow(Math.Abs(pos.X - point.X), 2) + Math.Pow(Math.Abs(pos.Y - point.Y), 2)) > 30)
            {
                GameModel.Move(e);
                e.Handled = true;
            }
        }
    }

    private void GameControl_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        GameModel.IsDrop = false;
    }

    private void GameControl_DoubleTapped(object? sender, TappedEventArgs e)
    {
        e.Handled = true;
        GameModel.Launch();
    }

    private void GameControl_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;

        GameModel.SetSelect();

        var pro = e.GetCurrentPoint(this);
        point = pro.Position;

        if (pro.Properties.IsRightButtonPressed)
        {
            GameModel.Flyout(this);
        }
    }
}
