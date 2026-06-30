using System.Windows.Forms;

namespace CodexCapture.Models;

public sealed class HotkeySettings
{
    public Keys Key { get; set; } = Keys.A;

    public HotkeyModifiers Modifiers { get; set; } = HotkeyModifiers.Control | HotkeyModifiers.Alt;

    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }
}

[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}
