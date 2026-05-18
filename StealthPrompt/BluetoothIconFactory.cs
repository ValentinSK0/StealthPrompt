using System.Drawing.Drawing2D;

namespace StealthPrompt;

public static class BluetoothIconFactory
{
    public static Icon Create()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);

        using var blue = new SolidBrush(Color.FromArgb(0, 72, 255));
        using var white = new Pen(Color.White, 3.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Miter
        };

        graphics.FillEllipse(blue, 2, 2, 28, 28);

        var points = new[]
        {
            new Point(15, 5),
            new Point(23, 12),
            new Point(15, 19),
            new Point(15, 5),
            new Point(9, 10),
            new Point(23, 24),
            new Point(15, 29),
            new Point(15, 19),
            new Point(9, 23)
        };

        graphics.DrawLines(white, points);

        var handle = bitmap.GetHicon();
        return Icon.FromHandle(handle);
    }

}
