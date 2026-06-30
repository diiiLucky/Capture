using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using CodexCapture.Models;

namespace CodexCapture.Services;

public sealed class ScreenCaptureService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public CaptureResult CaptureRegion(Int32Rect bounds, string historyDirectory, CaptureSourceType sourceType, string? windowTitle = null)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException("Capture bounds are empty.");
        }

        using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, new System.Drawing.Size(bounds.Width, bounds.Height), CopyPixelOperation.SourceCopy);
        }

        var capture = CreateCaptureResult(bounds, historyDirectory, sourceType, windowTitle);
        var dir = Path.GetDirectoryName(capture.ImagePath);
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

        bitmap.Save(capture.ImagePath, ImageFormat.Png);
        SaveMetadata(capture);
        return capture;
    }

    public void CopyToClipboard(CaptureResult capture)
    {
        if (!File.Exists(capture.ImagePath))
        {
            throw new FileNotFoundException("Capture image not found.", capture.ImagePath);
        }

        BitmapFrame image;
        using (var stream = File.OpenRead(capture.ImagePath))
        {
            image = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        }

        var dataObject = new DataObject();
        dataObject.SetImage(image);
        dataObject.SetFileDropList([capture.ImagePath]);
        Clipboard.SetDataObject(dataObject, true);
    }

    private static CaptureResult CreateCaptureResult(
        Int32Rect bounds,
        string historyDirectory,
        CaptureSourceType sourceType,
        string? windowTitle)
    {
        var now = DateTimeOffset.Now;
        var id = now.ToString("yyyyMMdd-HHmmss-fff");
        var dayDirectory = Path.Combine(historyDirectory, now.ToString("yyyy-MM-dd"));
        var path = Path.Combine(dayDirectory, $"{id}.png");
        return new CaptureResult
        {
            Id = id,
            ImagePath = path,
            SourceType = sourceType,
            WidthPx = bounds.Width,
            HeightPx = bounds.Height,
            ScreenX = bounds.X,
            ScreenY = bounds.Y,
            ScreenWidth = bounds.Width,
            ScreenHeight = bounds.Height,
            WindowTitle = windowTitle,
            CreatedAt = now
        };
    }

    private static void SaveMetadata(CaptureResult capture)
    {
        var metadataPath = Path.ChangeExtension(capture.ImagePath, ".json");
        var json = JsonSerializer.Serialize(capture, JsonOptions);
        File.WriteAllText(metadataPath, json);
    }
}
