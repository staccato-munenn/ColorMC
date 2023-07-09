using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;
using ColorMC.Gui;

namespace ColorMC.Android;

[Activity(Label = "ColorMC",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode,
    ScreenOrientation = ScreenOrientation.SensorLandscape)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        ColorMCGui.StartPhone(GetExternalFilesDir(null) + "/");

        base.OnCreate(savedInstanceState);
    }
}
