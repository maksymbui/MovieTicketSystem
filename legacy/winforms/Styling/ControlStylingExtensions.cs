using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp.Styling;

internal static class ControlStylingExtensions
{
    public static void ApplySearchBoxTheme(this TextBox textBox)
    {
        textBox.BackColor = ThemeColors.PanelElevated;
        textBox.ForeColor = ThemeColors.TextPrimary;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Font = ThemeTypography.Body1;
    }

    public static void ApplyPrimaryButtonTheme(this Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = ThemeColors.AccentPrimary;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ThemeColors.AccentSecondary;
        button.FlatAppearance.MouseDownBackColor = ThemeColors.AccentPrimary;
        button.BackColor = ThemeColors.AccentPrimary;
        button.ForeColor = ThemeColors.PrimaryBackground;
        button.Font = ThemeTypography.Button;
        button.Cursor = Cursors.Hand;
    }

    public static void ApplyPillButtonTheme(this Button button, bool isActive)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = ThemeTypography.Body2;
        button.BackColor = isActive ? ThemeColors.AccentPrimary : ThemeColors.PanelElevated;
        button.ForeColor = isActive ? ThemeColors.PrimaryBackground : ThemeColors.TextPrimary;
        button.FlatAppearance.MouseOverBackColor = isActive ? ThemeColors.AccentSecondary : ThemeColors.PanelBackground;
        button.FlatAppearance.MouseDownBackColor = isActive ? ThemeColors.AccentPrimary : ThemeColors.PanelElevated;
        button.Padding = new Padding(12, 6, 12, 6);
        button.Cursor = Cursors.Hand;
    }

    public static void ApplyPanelCardTheme(this Panel panel)
    {
        panel.BackColor = ThemeColors.PanelElevated;
        panel.Padding = new Padding(18);
        panel.Margin = new Padding(10);
    }

    public static void ApplyLabelTheme(this Label label, bool secondary = false)
    {
        label.ForeColor = secondary ? ThemeColors.TextSecondary : ThemeColors.TextPrimary;
    }

    public static void ApplySurfaceBackground(this Control control)
    {
        control.BackColor = ThemeColors.PrimaryBackground;
        control.ForeColor = ThemeColors.TextPrimary;
    }
}
