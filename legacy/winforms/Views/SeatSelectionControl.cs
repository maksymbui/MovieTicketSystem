using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinFormsApp.Enums;
using WinFormsApp.Logic;
using WinFormsApp.Models;
using WinFormsApp.Styling;

namespace WinFormsApp.Views;

// UI for picking seats. All state feedback flows through BookingContext to keep
// behaviour deterministic for whatever host wraps this control.
internal sealed class SeatSelectionControl : UserControl
{
    readonly BookingContext _context;
    readonly ScreeningService _screenings = new();
    readonly BookingService _bookingService = new();
    readonly string _screeningId;
    readonly HashSet<string> _selectedSeats = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Button> _seatButtons = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler? BackRequested;
    public event EventHandler? ProceedRequested;

    public SeatSelectionControl(BookingContext context, string screeningId)
    {
        _context = context;
        _screeningId = screeningId;
        _selectedSeats.UnionWith(context.SelectedSeats);

        Dock = DockStyle.Fill;
        BackColor = ThemeColors.PrimaryBackground;

        BuildLayout();
    }

    void BuildLayout()
    {
        Controls.Clear();

        var topBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(18, 12, 18, 12),
            BackColor = ThemeColors.SecondaryBackground
        };

        var backButton = new Button { Text = "◀ Back" };
        backButton.ApplyPrimaryButtonTheme();
        backButton.Width = 100;
        backButton.Click += (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);

        var heading = new Label
        {
            Text = "Select Your Seats",
            Font = ThemeTypography.Heading1,
            ForeColor = ThemeColors.TextPrimary,
            AutoSize = true,
            Margin = new Padding(16, 4, 0, 0)
        };

        topBar.Controls.Add(backButton);
        topBar.Controls.Add(heading);
        Controls.Add(topBar);

        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            BackColor = ThemeColors.PrimaryBackground
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 16, 0, 16)
        };

        BuildSeatGrid(layout);
        contentPanel.Controls.Add(layout);
        contentPanel.Controls.Add(BuildLegend());

        Controls.Add(contentPanel);
        Controls.Add(BuildFooter());
    }

    void BuildSeatGrid(TableLayoutPanel layout)
    {
        var seatMap = _bookingService.GetSeatMap(_screeningId);
        var screening = DataStore.GetScreening(_screeningId);
        var auditorium = screening != null ? DataStore.GetAuditorium(screening.AuditoriumId) : null;

        if (auditorium is null || seatMap is null)
        {
            layout.Controls.Add(new Label
            {
                Text = "Seat map unavailable.",
                ForeColor = ThemeColors.TextSecondary,
                Font = ThemeTypography.Body1,
                AutoSize = true
            });
            return;
        }

        var rowCount = auditorium.RowCount;
        var colCount = auditorium.ColumnCount;

        layout.ColumnCount = colCount + 1;
        layout.RowCount = rowCount + 2;
        layout.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;

        layout.Controls.Add(new Label
        {
            Text = "Front of Cinema",
            Font = ThemeTypography.Body1,
            ForeColor = ThemeColors.TextSecondary,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        }, 0, 0);
        layout.SetColumnSpan(layout.Controls[layout.Controls.Count - 1], colCount + 1);

        for (int c = 0; c <= colCount; c++)
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, c == 0 ? 34f : 30f));
        for (int r = 0; r <= rowCount; r++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));

        for (int row = 0; row < rowCount; row++)
        {
            var rowLabel = new Label
            {
                Text = $"{(char)('A' + row)}",
                ForeColor = ThemeColors.TextSecondary,
                Font = ThemeTypography.Body2,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            layout.Controls.Add(rowLabel, 0, row + 1);

            for (int col = 0; col < colCount; col++)
            {
                var seatLabel = $"{(char)('A' + row)}{col + 1}";
                var state = seatMap.Seats.TryGetValue(seatLabel, out var seatState)
                    ? seatState
                    : SeatState.Available;

                var seatType = DataStore.GetSeatType(_screeningId, seatLabel);
                var button = CreateSeatButton(seatLabel, state, seatType);

                layout.Controls.Add(button, col + 1, row + 1);
                _seatButtons[seatLabel] = button;
            }
        }
    }

    Button CreateSeatButton(string seatLabel, SeatState state, SeatType seatType)
    {
        var button = new Button
        {
            Text = string.Empty,
            Tag = seatLabel,
            Dock = DockStyle.Fill,
            Margin = new Padding(2),
            FlatStyle = FlatStyle.Flat,
            Font = ThemeTypography.Body2,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;

        bool isSelected = _selectedSeats.Contains(seatLabel);

        if (state == SeatState.Booked)
        {
            button.BackColor = ThemeColors.AccentHighlight;
            button.Enabled = false;
        }
        else if (seatType == SeatType.Accessible)
        {
            button.Text = "♿";
            button.ForeColor = ThemeColors.TextSecondary;
            button.BackColor = ThemeColors.PanelBackground;
            button.Enabled = false;
        }
        else if (isSelected)
        {
            button.BackColor = ThemeColors.AccentSecondary;
            button.ForeColor = ThemeColors.PrimaryBackground;
        }
        else
        {
            button.BackColor = ThemeColors.PanelElevated;
            button.ForeColor = ThemeColors.TextPrimary;
        }

        button.Click += (_, _) => ToggleSeatSelection(button);
        return button;
    }

    void ToggleSeatSelection(Button button)
    {
        if (button.Tag is not string seatLabel || seatLabel.Length == 0)
            return;

        if (_selectedSeats.Contains(seatLabel))
        {
            _selectedSeats.Remove(seatLabel);
            button.BackColor = ThemeColors.PanelElevated;
            button.ForeColor = ThemeColors.TextPrimary;
            button.Text = string.Empty;
        }
        else
        {
            _selectedSeats.Add(seatLabel);
            button.BackColor = ThemeColors.AccentSecondary;
            button.ForeColor = ThemeColors.PrimaryBackground;
            button.Text = string.Empty;
        }
    }

    Control BuildLegend()
    {
        var legend = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 24, 0, 0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };

        legend.Controls.Add(CreateLegendItem(ThemeColors.PanelElevated, "Available"));
        legend.Controls.Add(CreateLegendItem(ThemeColors.AccentSecondary, "Your seat(s)"));
        legend.Controls.Add(CreateLegendItem(ThemeColors.AccentHighlight, "Reserved"));
        legend.Controls.Add(CreateLegendItem(ThemeColors.PanelBackground, "Accessible", "♿"));

        return legend;
    }

    Control CreateLegendItem(Color color, string text, string? symbol = null)
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(12, 0, 0, 0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };

        var swatch = new Label
        {
            Text = symbol ?? string.Empty,
            Width = 22,
            Height = 22,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = color,
            ForeColor = symbol is null ? Color.Transparent : ThemeColors.TextPrimary,
            Margin = new Padding(0, 0, 6, 0),
            Font = ThemeTypography.Body2
        };

        var label = new Label
        {
            Text = text,
            ForeColor = ThemeColors.TextSecondary,
            Font = ThemeTypography.Body2,
            AutoSize = true,
            Margin = new Padding(0, 2, 0, 0)
        };

        panel.Controls.Add(swatch);
        panel.Controls.Add(label);
        return panel;
    }

    Control BuildFooter()
    {
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            Padding = new Padding(18, 12, 18, 12),
            BackColor = ThemeColors.SecondaryBackground,
            FlowDirection = FlowDirection.RightToLeft
        };

        var proceedButton = new Button
        {
            Text = "Proceed",
            Width = 140
        };
        proceedButton.ApplyPrimaryButtonTheme();
        proceedButton.Click += (_, _) =>
        {
            _context.SetSelectedSeats(_selectedSeats);
            ProceedRequested?.Invoke(this, EventArgs.Empty);
        };

        footer.Controls.Add(proceedButton);
        return footer;
    }
}
