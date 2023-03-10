using Avalonia.Controls;
using Avalonia.Interactivity;
using ColorMC.Core.Objs;
using ColorMC.Core.Objs.ServerPack;
using ColorMC.Core.Utils;
using ColorMC.Gui.Objs;
using ColorMC.Gui.UIBinding;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ColorMC.Gui.UI.Controls.Server;

public partial class Tab2Control : UserControl
{
    private bool load = false;
    private ServerPackObj Obj1;
    private ObservableCollection<ServerPackModDisplayObj> List = new();

    public Tab2Control()
    {
        InitializeComponent();

        DataGrid1.Items = List;
        DataGrid1.CellEditEnded += DataGrid1_CellEditEnded;

        Button1.Click += Button1_Click;
        Button2.Click += Button2_Click;
    }

    private void Button2_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var item in List)
        {
            item.Check = false;
            item.NotifyPropertyChanged("Check");
            ItemEdit(item);
        }
    }

    private void Button1_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var item in List)
        {
            item.Check = true;
            item.NotifyPropertyChanged("Check");
            ItemEdit(item);
        }
    }

    private string GetUrl(ServerPackModDisplayObj item)
    {
        if (string.IsNullOrWhiteSpace(item.PID) || string.IsNullOrWhiteSpace(item.FID))
        {
            if (!string.IsNullOrWhiteSpace(item.Url))
            {
                return item.Url;
            }
            else if (!string.IsNullOrWhiteSpace(Obj1.Url))
            {
                return Obj1.Url + "mods/" + item.FileName;
            }
            else
            {
                return "";
            }
        }
        else if (UIUtils.CheckNotNumber(item.PID) || UIUtils.CheckNotNumber(item.FID))
        {
            return $"https://cdn.modrinth.com/data/{item.PID}/versions/{item.FID}/{item.FileName}";
        }
        else
        {
            var fid = long.Parse(item.FID);
            return $"https://edge.forgecdn.net/files/{fid / 1000}/{fid % 1000}/{item.FileName}";
        }
    }

    private void ItemEdit(ServerPackModDisplayObj obj)
    {
        var item = Obj1.Mod?.FirstOrDefault(a => a.Sha1 == obj.Sha1
                        && a.File == obj.FileName);
        if (obj.Check)
        {
            SourceType? source = null;
            if (obj.Source == SourceType.CurseForge.GetName())
            {
                source = SourceType.CurseForge;
            }
            else if (obj.Source == SourceType.Modrinth.GetName())
            {
                source = SourceType.Modrinth;
            }

            if (item != null)
            {
                item.Projcet = obj.PID;
                item.FileId = obj.FID;
                item.Source = source;
                item.Sha1 = obj.Sha1;
                item.Url = GetUrl(obj);
                item.File = obj.FileName;
            }
            else
            {
                Obj1.Mod ??= new();
                item = new()
                {
                    Projcet = obj.PID,
                    FileId = obj.FID,
                    Source = source,
                    Sha1 = obj.Sha1,
                    Url = GetUrl(obj),
                    File = obj.FileName
                };
                Obj1.Mod.Add(item);
            }

            obj.Url = item.Url;
        }
        else
        {
            if (item != null)
            {
                obj.Url = "";
                Obj1.Mod?.Remove(item);
            }
        }

        GameBinding.SaveServerPack(Obj1);
    }

    private void DataGrid1_CellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (e.Row.DataContext is ServerPackModDisplayObj obj)
        {
            ItemEdit(obj);
        }
    }

    public async void Load()
    {
        if (Obj1 == null)
            return;

        load = true;
        List.Clear();
        var mods = await GameBinding.GetGameMods(Obj1.Game);

        Obj1.Mod?.RemoveAll(a => mods.Find(b => a.Sha1 == b.Obj.Sha1) == null);

        mods.ForEach(item =>
        {
            if (item.Obj.Broken)
                return;

            string file = Path.GetFileName(item.Obj.Local);

            var item1 = Obj1.Mod?.FirstOrDefault(a => a.Sha1 == item.Obj.Sha1
                        && a.File == file);

            var item2 = new ServerPackModDisplayObj()
            {
                FileName = file,
                Check = item1 != null,
                PID = item.PID,
                FID = item.FID,
                Sha1 = item.Obj.Sha1,
                Obj = item
            };
            if (item2.Check)
            {
                if (item1 != null && !string.IsNullOrWhiteSpace(item1.Url))
                {
                    item2.Url = item1.Url;
                }
                else
                {
                    item2.Url = GetUrl(item2);
                }
            }

            List.Add(item2);
        });

        GameBinding.SaveServerPack(Obj1);

        load = false;
    }

    public void SetObj(ServerPackObj obj1)
    {
        Obj1 = obj1;
    }
}
