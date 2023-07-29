using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ColorMC.Gui.UI.Flyouts;
using ColorMC.Gui.UI.Model.GameEdit;
using ColorMC.Gui.Utils;
using System.Threading;

namespace ColorMC.Gui.UI.Controls.GameEdit;

public partial class Tab10Control : UserControl
{
    public Tab10Control()
    {
        InitializeComponent();

        Button_A1.PointerExited += Button_A1_PointerLeave;
        Button_A.PointerEntered += Button_A_PointerEnter;

        Button_R1.PointerExited += Button_R1_PointerLeave;
        Button_R.PointerEntered += Button_R_PointerEnter;

        DataGrid1.CellPointerPressed += DataGrid1_CellPointerPressed;
    }

    private void DataGrid1_CellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (e.PointerPressedEventArgs.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _ = new GameEditFlyout5(this, (DataContext as GameEditTab10Model)!);
            });
        }
    }

    public void Opened()
    {
        DataGrid1.SetFontColor();
    }

    private void Button_A1_PointerLeave(object? sender, PointerEventArgs e)
    {
        App.CrossFade100.Start(Button_A1, null);
        Button_A.IsVisible = true;
    }

    private void Button_A_PointerEnter(object? sender, PointerEventArgs e)
    {
        Button_A.IsVisible = false;
        App.CrossFade100.Start(null, Button_A1);
    }
    private void Button_R1_PointerLeave(object? sender, PointerEventArgs e)
    {
        App.CrossFade100.Start(Button_R1, null);
        Button_R.IsVisible = true;
    }

    private void Button_R_PointerEnter(object? sender, PointerEventArgs e)
    {
        Button_R.IsVisible = false;
        App.CrossFade100.Start(null, Button_R1);
    }
}
