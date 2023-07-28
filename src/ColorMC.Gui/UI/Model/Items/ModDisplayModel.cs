using ColorMC.Core.Helpers;
using ColorMC.Core.Objs;
using ColorMC.Core.Objs.Minecraft;
using ColorMC.Core.Utils;
using ColorMC.Gui.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace ColorMC.Gui.UI.Model;

/// <summary>
/// Mod项目
/// </summary>
public partial class ModExportModel : ObservableObject
{
    [ObservableProperty]
    private bool _export;
    [ObservableProperty]
    private string? _pID;
    [ObservableProperty]
    private string? _fID;

    public string Name => Obj.name;
    public string Modid => Obj.modid;
    public string Local => Obj.Local;
    public string Loader => Obj.Loader.GetName();
    public string Source
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PID) || string.IsNullOrWhiteSpace(FID))
                return "";
            return Funtcions.CheckNotNumber(PID) || Funtcions.CheckNotNumber(FID) ?
                SourceType.Modrinth.GetName() : SourceType.CurseForge.GetName();
        }
    }

    partial void OnPIDChanged(string? value)
    {
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Source)));
    }

    partial void OnFIDChanged(string? value)
    {
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Source)));
    }

    public ModInfoObj? Obj1;
    public ModObj Obj;
}
