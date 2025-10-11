using System.Drawing;

namespace WinFormsApp.Styling;

internal static class ThemeColors
{
    public static readonly Color PrimaryBackground = FromHex("#19181B");
    public static readonly Color SecondaryBackground = FromHex("#0A192F");
    public static readonly Color PanelBackground = FromHex("#252525");
    public static readonly Color PanelElevated = FromHex("#343A3F");
    public static readonly Color Sidebar = FromHex("#3B3A3C");

    public static readonly Color AccentPrimary = FromHex("#EF9654");
    public static readonly Color AccentSecondary = FromHex("#3AC6F0");
    public static readonly Color AccentHighlight = FromHex("#F76C6C");

    public static readonly Color TextPrimary = FromHex("#FAF6F6");
    public static readonly Color TextSecondary = FromHex("#C6CBD2");
    public static readonly Color TextMuted = Color.FromArgb(140, TextPrimary);

    public static readonly Color Divider = Color.FromArgb(35, TextPrimary);

    public static readonly (Color Start, Color End)[] PosterGradients =
    {
        (FromHex("#3AC6F0"), FromHex("#F76C6C")),
        (FromHex("#76C66A"), FromHex("#EF9654")),
        (FromHex("#8A5AF7"), FromHex("#FEC669")),
        (FromHex("#F76C6C"), FromHex("#73333E")),
        (FromHex("#FEC669"), FromHex("#3AC6F0")),
        (FromHex("#B36CE3"), FromHex("#F76C6C"))
    };

    public static Color FromHex(string hex)
    {
        hex = hex.Replace("#", string.Empty);
        if (hex.Length != 6)
            throw new ArgumentException("Hex value must be 6 characters.", nameof(hex));

        return Color.FromArgb(
            255,
            Convert.ToInt32(hex.Substring(0, 2), 16),
            Convert.ToInt32(hex.Substring(2, 2), 16),
            Convert.ToInt32(hex.Substring(4, 2), 16));
    }
}
