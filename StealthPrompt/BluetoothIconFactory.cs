using System.Drawing.Drawing2D;

namespace StealthPrompt;

public static class BluetoothIconFactory
{
    public static Icon Create()
    {
        var assetPath = Path.Combine(AppContext.BaseDirectory, "assets", "250px-Bluetooth.svg.png");
        if (File.Exists(assetPath))
        {
            using var image = Image.FromFile(assetPath);
            using var assetBitmap = new Bitmap(image, new Size(256, 256));
            var assetHandle = assetBitmap.GetHicon();
            return Icon.FromHandle(assetHandle);
        }

        using var bitmap = new Bitmap(256, 256);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.Clear(Color.Transparent);

        var body = new Rectangle(47, 10, 162, 236);
        using var shape = RoundedRect(body, 78);
        using var fill = new LinearGradientBrush(
            body,
            Color.FromArgb(14, 111, 190),
            Color.FromArgb(8, 74, 145),
            LinearGradientMode.ForwardDiagonal);
        graphics.FillPath(fill, shape);

        using var highlight = new Pen(Color.FromArgb(38, 255, 255, 255), 5);
        graphics.DrawArc(highlight, body.Left + 18, body.Top + 10, body.Width - 36, 80, 200, 115);

        using var shadow = new Pen(Color.FromArgb(72, 0, 0, 0), 42)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Miter
        };
        using var white = new Pen(Color.White, 35)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Miter
        };

        using var glyph = BuildBluetoothGlyph();
        using var shadowGlyph = (GraphicsPath)glyph.Clone();
        using var shadowMatrix = new Matrix();
        shadowMatrix.Translate(5, 6);
        shadowGlyph.Transform(shadowMatrix);

        graphics.DrawPath(shadow, shadowGlyph);
        graphics.DrawPath(white, glyph);

        var handle = bitmap.GetHicon();
        return Icon.FromHandle(handle);
    }

    private static GraphicsPath BuildBluetoothGlyph()
    {
        var glyph = new GraphicsPath();
        glyph.StartFigure();
        glyph.AddLines(
        [
            new Point(126, 33),
            new Point(180, 84),
            new Point(126, 136),
            new Point(126, 33),
            new Point(83, 76),
            new Point(174, 166),
            new Point(126, 221),
            new Point(126, 136),
            new Point(82, 181)
        ]);
        return glyph;
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
