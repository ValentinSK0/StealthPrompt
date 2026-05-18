using System.Drawing.Drawing2D;

namespace StealthPrompt;

public static class BluetoothIconFactory
{
    public static Icon Create()
    {
        using var bitmap = new Bitmap(64, 64);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var blue = new SolidBrush(Color.FromArgb(0, 15, 245));
        using var white = new Pen(Color.White, 9)
        {
            StartCap = LineCap.Square,
            EndCap = LineCap.Square,
            LineJoin = LineJoin.Miter
        };

        using var shape = RoundedRect(new Rectangle(4, 2, 56, 60), 22);
        graphics.FillPath(blue, shape);

        var points = new[]
        {
            new Point(31, 8),
            new Point(49, 25),
            new Point(31, 42),
            new Point(31, 8),
            new Point(17, 20),
            new Point(47, 50),
            new Point(31, 62),
            new Point(31, 42),
            new Point(17, 54)
        };

        graphics.DrawLines(white, points);

        var handle = bitmap.GetHicon();
        return Icon.FromHandle(handle);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
