using Avalonia.Controls;
using Avalonia.Input;
using ColorMC.Gui.Objs;
using ColorMC.Gui.UI.Model.Add;
using ColorMC.Gui.UI.Windows;

namespace ColorMC.Gui.UI.Controls.Add;

public partial class AddJavaControl : UserControl, IUserControl
{
    public IBaseWindow Window => App.FindRoot(VisualRoot);

    public UserControl Con => this;

    public string Title => App.GetLanguage("AddJavaWindow.Title");

    private readonly AddJavaControlModel _model;

    public AddJavaControl()
    {
        InitializeComponent();

        _model = new(this);
        DataContext = _model;

        DataGrid1.DoubleTapped += DataGrid1_DoubleTapped;
    }

    public void Opened()
    {
        Window.SetTitle(Title);

        _model.TypeIndex = 0;
    }

    public void Closed()
    {
        App.AddJavaWindow = null;
    }

    private void DataGrid1_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataGrid1.SelectedItem is not JavaDownloadDisplayObj obj)
            return;

        _model.Install(obj);
    }
}
