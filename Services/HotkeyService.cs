using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using CodexCapture.Models;

namespace CodexCapture.Services;

public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0x4343;
    private const int WmHotkey = 0x0312;
    private HwndSource? _source;
    private bool _registered;

    public event EventHandler? Pressed;

    public bool Register(Window window, HotkeySettings hotkey)
    {
        Dispose();
        var helper = new WindowInteropHelper(window);
        _source = HwndSource.FromHwnd(helper.Handle);
        if (_source is null)
        {
            return false;
        }

        _source.AddHook(WndProc);
        _registered = RegisterHotKey(helper.Handle, HotkeyId, (uint)hotkey.Modifiers, (uint)hotkey.Key);
        return _registered;
    }

    public void Dispose()
    {
        if (_source is not null)
        {
            var handle = _source.Handle;
            if (_registered)
            {
                UnregisterHotKey(handle, HotkeyId);
            }

            _source.RemoveHook(WndProc);
            _source = null;
            _registered = false;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            Pressed?.Invoke(this, EventArgs.Empty);
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
