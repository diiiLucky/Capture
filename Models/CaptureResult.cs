namespace CodexCapture.Models;

public sealed class CaptureResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string ImagePath { get; set; } = string.Empty;

    public CaptureSourceType SourceType { get; set; }

    public int WidthPx { get; set; }

    public int HeightPx { get; set; }

    public int ScreenX { get; set; }

    public int ScreenY { get; set; }

    public int ScreenWidth { get; set; }

    public int ScreenHeight { get; set; }

    public string? WindowTitle { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
}

public enum CaptureSourceType
{
    Window,
    Region
}
