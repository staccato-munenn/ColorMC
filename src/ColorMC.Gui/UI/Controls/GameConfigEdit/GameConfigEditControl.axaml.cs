using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Indentation.CSharp;
using ColorMC.Core.Objs;
using ColorMC.Core.Objs.Minecraft;
using ColorMC.Gui.UI.Model;
using ColorMC.Gui.UI.Model.GameConfigEdit;
using ColorMC.Gui.UI.Windows;
using ColorMC.Gui.Utils;
using System.ComponentModel;

namespace ColorMC.Gui.UI.Controls.ConfigEdit;

public partial class GameConfigEditControl : UserControl, IUserControl
{
    private readonly GameConfigEditModel _model;

    public IBaseWindow Window => App.FindRoot(VisualRoot);

    public UserControl Con => this;

    public string Title
    {
        get
        {
            if (_model.World == null)
            {
                return string.Format(App.GetLanguage("ConfigEditWindow.Title"),
                    _model.Obj?.Name);
            }
            else
            {
                return string.Format(App.GetLanguage("ConfigEditWindow.Title1"),
                    _model.Obj?.Name, _model.World?.LevelName);
            }
        }
    }

    public BaseModel Model => _model;

    public GameConfigEditControl()
    {
        InitializeComponent();

        NbtViewer.PointerPressed += NbtViewer_PointerPressed;
        NbtViewer.KeyDown += NbtViewer_KeyDown;

        TextEditor1.KeyDown += NbtViewer_KeyDown;
        TextEditor1.TextArea.Background = Brushes.Transparent;
        TextEditor1.Options.ShowBoxForControlCharacters = true;
        TextEditor1.TextArea.IndentationStrategy =
            new CSharpIndentationStrategy(TextEditor1.Options);

        DataGrid1.CellEditEnded += DataGrid1_CellEditEnded;
        DataGrid1.CellPointerPressed += DataGrid1_CellPointerPressed;
    }

    public GameConfigEditControl(WorldObj world) : this()
    {
        _model = new(this, world.Game, world);
        DataContext = _model;
        _model.PropertyChanged += Model_PropertyChanged;
    }

    public GameConfigEditControl(GameSettingObj obj) : this()
    {
        _model = new(this, obj, null);
        DataContext = _model;
        _model.PropertyChanged += Model_PropertyChanged;
    }

    private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "TurnTo")
        {
            NbtViewer.Scroll!.Offset = new(0, _model.TurnTo * 25);
        }
    }

    private void DataGrid1_CellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (e.PointerPressedEventArgs.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _model.Flyout(this);
            });
        }
    }

    private void DataGrid1_CellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit)
        {
            _model.DataEdit();
        }
    }

    private void NbtViewer_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.S && e.KeyModifiers == KeyModifiers.Control)
        {
            _model.Save();
        }
    }

    private void NbtViewer_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _model.Pressed(this);
            });
        }
    }

    public void Opened()
    {
        Window.SetTitle(Title);

        DataGrid1.SetFontColor();

        _model.Load();
    }

    public void Closed()
    {
        string key;
        if (_model.World != null)
        {
            key = _model.Obj.UUID + ":" + _model.World.LevelName;
        }
        else
        {
            key = _model.Obj.UUID;
        }
        App.ConfigEditWindows.Remove(key);
    }
}
