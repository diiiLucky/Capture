using System.Windows;

namespace CodexCapture.Windows;

public sealed class HotkeySinkWindow : Window
{
    public HotkeySinkWindow()
    {
        // Hidden message target for global hotkey registration.
        Width = 0;
        Height = 0;
        ShowInTaskbar = false;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Opacity = 0;
    }
}
