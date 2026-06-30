using System.Windows;

namespace CodexCapture.Windows;

internal enum CaptureMenuPlacementKind
{
    Above,
    Below,
    Right,
    Left,
    Inside
}

internal readonly record struct CaptureMenuPlacement(Point Location, CaptureMenuPlacementKind Kind);

internal static class CaptureMenuPlacementCalculator
{
    public static Size ToPhysicalPixels(Size dipSize, double dpiScaleX, double dpiScaleY) => new(
        Math.Ceiling(dipSize.Width * dpiScaleX),
        Math.Ceiling(dipSize.Height * dpiScaleY));

    public static CaptureMenuPlacement Calculate(
        Rect capture,
        Size menu,
        Rect workingArea,
        double gap,
        double screenMargin,
        double insideMargin)
    {
        var safeArea = DeflateOrOriginal(workingArea, screenMargin);
        var menuWidth = Math.Min(menu.Width, safeArea.Width);
        var menuHeight = Math.Min(menu.Height, safeArea.Height);
        var menuSize = new Size(menuWidth, menuHeight);

        if (TryPlaceAbove(capture, menuSize, safeArea, gap, out var above))
        {
            return new CaptureMenuPlacement(above, CaptureMenuPlacementKind.Above);
        }

        if (TryPlaceBelow(capture, menuSize, safeArea, gap, out var below))
        {
            return new CaptureMenuPlacement(below, CaptureMenuPlacementKind.Below);
        }

        if (TryPlaceRight(capture, menuSize, safeArea, gap, out var right))
        {
            return new CaptureMenuPlacement(right, CaptureMenuPlacementKind.Right);
        }

        if (TryPlaceLeft(capture, menuSize, safeArea, gap, out var left))
        {
            return new CaptureMenuPlacement(left, CaptureMenuPlacementKind.Left);
        }

        return new CaptureMenuPlacement(
            PlaceInside(capture, menuSize, safeArea, insideMargin),
            CaptureMenuPlacementKind.Inside);
    }

    private static bool TryPlaceAbove(Rect capture, Size menu, Rect safeArea, double gap, out Point location)
    {
        var top = capture.Top - menu.Height - gap;
        if (top < safeArea.Top)
        {
            location = default;
            return false;
        }

        location = new Point(
            Clamp(capture.Left + capture.Width / 2 - menu.Width / 2, safeArea.Left, safeArea.Right - menu.Width),
            top);
        return true;
    }

    private static bool TryPlaceBelow(Rect capture, Size menu, Rect safeArea, double gap, out Point location)
    {
        var top = capture.Bottom + gap;
        if (top + menu.Height > safeArea.Bottom)
        {
            location = default;
            return false;
        }

        location = new Point(
            Clamp(capture.Left + capture.Width / 2 - menu.Width / 2, safeArea.Left, safeArea.Right - menu.Width),
            top);
        return true;
    }

    private static bool TryPlaceRight(Rect capture, Size menu, Rect safeArea, double gap, out Point location)
    {
        var left = capture.Right + gap;
        if (left + menu.Width > safeArea.Right)
        {
            location = default;
            return false;
        }

        location = new Point(
            left,
            Clamp(capture.Top + capture.Height / 2 - menu.Height / 2, safeArea.Top, safeArea.Bottom - menu.Height));
        return true;
    }

    private static bool TryPlaceLeft(Rect capture, Size menu, Rect safeArea, double gap, out Point location)
    {
        var left = capture.Left - menu.Width - gap;
        if (left < safeArea.Left)
        {
            location = default;
            return false;
        }

        location = new Point(
            left,
            Clamp(capture.Top + capture.Height / 2 - menu.Height / 2, safeArea.Top, safeArea.Bottom - menu.Height));
        return true;
    }

    private static Point PlaceInside(Rect capture, Size menu, Rect safeArea, double insideMargin)
    {
        var insideLeftMin = Math.Max(capture.Left + insideMargin, safeArea.Left);
        var insideLeftMax = Math.Min(capture.Right - menu.Width - insideMargin, safeArea.Right - menu.Width);
        var insideTopMin = Math.Max(capture.Top + insideMargin, safeArea.Top);
        var insideTopMax = Math.Min(capture.Bottom - menu.Height - insideMargin, safeArea.Bottom - menu.Height);

        var left = capture.Right - menu.Width - insideMargin;
        var top = capture.Bottom - menu.Height - insideMargin;

        if (insideLeftMin <= insideLeftMax)
        {
            left = Clamp(left, insideLeftMin, insideLeftMax);
        }
        else
        {
            left = Clamp(left, safeArea.Left, safeArea.Right - menu.Width);
        }

        if (insideTopMin <= insideTopMax)
        {
            top = Clamp(top, insideTopMin, insideTopMax);
        }
        else
        {
            top = Clamp(top, safeArea.Top, safeArea.Bottom - menu.Height);
        }

        return new Point(left, top);
    }

    private static Rect DeflateOrOriginal(Rect rect, double margin)
    {
        if (rect.Width <= margin * 2 || rect.Height <= margin * 2)
        {
            return rect;
        }

        return new Rect(
            rect.Left + margin,
            rect.Top + margin,
            rect.Width - margin * 2,
            rect.Height - margin * 2);
    }

    private static double Clamp(double value, double min, double max) =>
        max < min ? min : Math.Min(Math.Max(value, min), max);
}
