using System.Text;
using System.Windows;
using CodexCapture.Interop;
using WpfPoint = System.Windows.Point;

namespace CodexCapture.Services;

public sealed class WindowDetectionService
{
    public DetectedWindow? Detect(WpfPoint screenPoint)
    {
        var hwnd = FindWindowUnderPoint(screenPoint);
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        var root = NativeMethods.GetAncestor(hwnd, NativeMethods.GaRoot);
        if (root == IntPtr.Zero || !NativeMethods.IsWindowVisible(root))
        {
            return null;
        }

        var rect = GetWindowBounds(root);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return null;
        }

        var titleBuilder = new StringBuilder(512);
        _ = NativeMethods.GetWindowText(root, titleBuilder, titleBuilder.Capacity);
        return new DetectedWindow(root, rect, titleBuilder.ToString());
    }

    private static IntPtr FindWindowUnderPoint(WpfPoint screenPoint)
    {
        var x = (int)screenPoint.X;
        var y = (int)screenPoint.Y;
        var currentProcessId = Environment.ProcessId;
        var result = IntPtr.Zero;

        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hwnd))
            {
                return true;
            }

            NativeMethods.GetWindowThreadProcessId(hwnd, out int processId);
            if (processId == currentProcessId)
            {
                return true;
            }

            if (NativeMethods.IsIconic(hwnd) || NativeMethods.IsCloaked(hwnd))
            {
                return true;
            }

            var className = NativeMethods.GetClassName(hwnd);
            if (className is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd")
            {
                return true;
            }

            var exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GwlExStyle);
            if ((exStyle & NativeMethods.WsExToolWindow) != 0)
            {
                return true;
            }

            var bounds = GetWindowBounds(hwnd);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return true;
            }

            if (x >= bounds.X && x < bounds.X + bounds.Width &&
                y >= bounds.Y && y < bounds.Y + bounds.Height)
            {
                result = hwnd;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        return result;
    }

    private static Int32Rect GetWindowBounds(IntPtr hwnd)
    {
        if (NativeMethods.DwmGetWindowAttribute(
                hwnd,
                NativeMethods.DwmwaExtendedFrameBounds,
                out var dwmRect,
                MarshalSizeOfRect()) == 0)
        {
            return new Int32Rect(dwmRect.Left, dwmRect.Top, dwmRect.Width, dwmRect.Height);
        }

        return NativeMethods.GetWindowRect(hwnd, out var rect)
            ? new Int32Rect(rect.Left, rect.Top, rect.Width, rect.Height)
            : Int32Rect.Empty;
    }

    private static int MarshalSizeOfRect() => System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.RECT>();
}

public sealed record DetectedWindow(IntPtr Handle, Int32Rect Bounds, string? Title);
