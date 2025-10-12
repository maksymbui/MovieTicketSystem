using System.Drawing;
using System.Drawing.Drawing2D;
using WinFormsApp.Styling;

namespace WinFormsApp.Utils;

internal static class PosterGenerator
{

    public static Image CreatePlaceholder(string title, int width, int height)
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.HighQuality;

        var gradient = PickGradient(title);
        using var brush = new LinearGradientBrush(new Rectangle(0, 0, width, height), gradient.Start, gradient.End, 35f);
        g.FillRectangle(brush, 0, 0, width, height);

        var initials = GetInitials(title);
        using var font = new Font("Segoe UI Semibold", Math.Max(22, width / 6f), GraphicsUnit.Pixel);
        var size = g.MeasureString(initials, font);
        var point = new PointF((width - size.Width) / 2f, (height - size.Height) / 2f);
        using var textBrush = new SolidBrush(Color.FromArgb(235, ThemeColors.TextPrimary));
        g.DrawString(initials, font, textBrush, point);

        return bmp;
    }

    static (Color Start, Color End) PickGradient(string title)
    {
        if (ThemeColors.PosterGradients.Length == 0)
            return (ThemeColors.AccentSecondary, ThemeColors.AccentHighlight);

        var hash = Math.Abs(title.GetHashCode());
        var index = hash % ThemeColors.PosterGradients.Length;
        return ThemeColors.PosterGradients[index];
    }

    static string GetInitials(string title)
    {
        var parts = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "???";

        if (parts.Length == 1)
            return parts[0].Length <= 3 ? parts[0].ToUpperInvariant() : parts[0].Substring(0, 3).ToUpperInvariant();

        return string.Concat(parts.Take(3).Select(p => char.ToUpperInvariant(p[0])));
    }
}
