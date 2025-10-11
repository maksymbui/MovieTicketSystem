using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinFormsApp.Entities;
using WinFormsApp.Enums;
using WinFormsApp.Logic;
using WinFormsApp.Models;
using WinFormsApp.Styling;

namespace WinFormsApp.Views;

internal sealed class MovieDetailControl : UserControl
{
    readonly ScreeningService _screenings = new();
    readonly BookingContext _context;

    readonly Label _titleLabel = new() { Font = ThemeTypography.Heading1, ForeColor = ThemeColors.TextPrimary };
    readonly Label _metaLabel = new() { Font = ThemeTypography.Body1, ForeColor = ThemeColors.TextSecondary };
    readonly TextBox _synopsis = new()
    {
        Multiline = true,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        ForeColor = ThemeColors.TextPrimary,
        BackColor = ThemeColors.PrimaryBackground
    };

    readonly FlowLayoutPanel _cinemaFilterPanel = new()
    {
        Dock = DockStyle.Top,
        Height = 100,
        Padding = new Padding(20, 12, 20, 12),
        BackColor = ThemeColors.SecondaryBackground,
        AutoScroll = true
    };

    readonly Button _applyCinemaFilter = new()
    {
        Text = "Apply Filters",
        AutoSize = true,
        Margin = new Padding(12, 0, 0, 0)
    };

    readonly Button _clearCinemaFilter = new()
    {
        Text = "Clear",
        AutoSize = true,
        Margin = new Padding(8, 0, 0, 0)
    };

    readonly FlowLayoutPanel _dateTabs = new()
    {
        Dock = DockStyle.Top,
        Height = 50,
        Padding = new Padding(20, 8, 20, 0),
        BackColor = ThemeColors.PrimaryBackground
    };

    readonly FlowLayoutPanel _sessionPanel = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        Padding = new Padding(18),
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false
    };

    DateOnly? _selectedDate;
    readonly HashSet<string> _selectedCinemaIds = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler? BackRequested;
    public event EventHandler<string>? ScreeningSelected;

    public MovieDetailControl(BookingContext context)
    {
        _context = context;
        Dock = DockStyle.Fill;
        BackColor = ThemeColors.PrimaryBackground;

        var backButton = new Button
        {
            Text = "◀ Back",
            Width = 100,
            Height = 36,
            Margin = new Padding(0, 0, 0, 12)
        };
        backButton.ApplyPrimaryButtonTheme();
        backButton.Click += (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);

        var headerPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(12, 6, 12, 6),
            BackColor = ThemeColors.SecondaryBackground
        };
        headerPanel.Controls.Add(backButton);

        _applyCinemaFilter.ApplyPrimaryButtonTheme();
        _clearCinemaFilter.ApplyPillButtonTheme(false);
        _applyCinemaFilter.Click += (_, _) =>
        {
            RefreshDateTabs();
            RefreshSessions();
        };
        _clearCinemaFilter.Click += (_, _) =>
        {
            _selectedCinemaIds.Clear();
            foreach (var check in _cinemaFilterPanel.Controls.OfType<CheckBox>())
                check.Checked = false;
            _selectedDate = null;
            RefreshDateTabs();
            RefreshSessions();
        };

        var infoPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 230,
            Padding = new Padding(24),
            BackColor = ThemeColors.PanelBackground
        };

        _synopsis.Dock = DockStyle.Fill;
        _synopsis.ScrollBars = ScrollBars.Vertical;

        infoPanel.Controls.Add(_synopsis);
        infoPanel.Controls.Add(new Panel { Height = 12, Dock = DockStyle.Top });
        infoPanel.Controls.Add(_metaLabel);
        infoPanel.Controls.Add(new Panel { Height = 6, Dock = DockStyle.Top });
        infoPanel.Controls.Add(_titleLabel);

        Controls.Add(_sessionPanel);
        Controls.Add(_dateTabs);
        Controls.Add(_cinemaFilterPanel);
        Controls.Add(headerPanel);
        Controls.Add(infoPanel);
    }

    public void BindMovie(Movie movie)
    {
        _context.SetMovie(movie);

        _titleLabel.Text = movie.Title;
        _metaLabel.Text = $"{movie.Rating} • {movie.RuntimeMinutes} mins • {string.Join(", ", movie.Genres)}";
        _synopsis.Text = movie.Synopsis;

        var cinemas = DataStore.Cinemas
            .OrderBy(c => c.Name)
            .ToList();

        _cinemaFilterPanel.Controls.Clear();
        _cinemaFilterPanel.Controls.Add(new Label
        {
            Text = "Choose cinemas:",
            ForeColor = ThemeColors.TextSecondary,
            Font = ThemeTypography.Body1,
            AutoSize = true,
            Padding = new Padding(0, 4, 12, 0)
        });

        foreach (var cinema in cinemas)
        {
            var checkBox = new CheckBox
            {
                Text = cinema.Name,
                ForeColor = ThemeColors.TextPrimary,
                AutoSize = true,
                Tag = cinema.Id,
                Margin = new Padding(0, 6, 12, 0)
            };
            checkBox.CheckedChanged += (_, _) =>
            {
                var id = (string)checkBox.Tag;
                if (checkBox.Checked) _selectedCinemaIds.Add(id);
                else _selectedCinemaIds.Remove(id);
            };
            _cinemaFilterPanel.Controls.Add(checkBox);
        }

        _cinemaFilterPanel.Controls.Add(_applyCinemaFilter);
        _cinemaFilterPanel.Controls.Add(_clearCinemaFilter);

        _selectedCinemaIds.Clear();
        _selectedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        RefreshDateTabs();
        RefreshSessions();
    }

    void RefreshSessions()
    {
        _sessionPanel.SuspendLayout();
        _sessionPanel.Controls.Clear();

        var movie = _context.SelectedMovie;
        if (movie is null)
        {
            _sessionPanel.ResumeLayout();
            return;
        }

        int containerWidth = Math.Max(360, _sessionPanel.ClientSize.Width - 36);

        var cinemas = DataStore.Cinemas.OrderBy(c => c.Name).ToList();

        foreach (var cinema in cinemas)
        {
            if (_selectedCinemaIds.Count > 0 && !_selectedCinemaIds.Contains(cinema.Id))
                continue;

            var sessions = _screenings.GetByMovie(movie.Id)
                .Where(s => s.CinemaId.Equals(cinema.Id, StringComparison.OrdinalIgnoreCase))
                .Where(s => !_selectedDate.HasValue || DateOnly.FromDateTime(s.StartUtc) == _selectedDate)
                .OrderBy(s => s.StartUtc)
                .ToList();

            if (sessions.Any())
            {
                var block = CreateCinemaBlock(cinema.Name, sessions, containerWidth);
                _sessionPanel.Controls.Add(block);
            }
        }

        if (_sessionPanel.Controls.Count == 0)
        {
            _sessionPanel.Controls.Add(new Label
            {
                Text = "No sessions scheduled.",
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 12f),
                AutoSize = true,
                Margin = new Padding(0, 12, 0, 0)
            });
        }

        _sessionPanel.ResumeLayout();
    }

    Form? _parentForm;
    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (_parentForm != null)
            _parentForm.ResizeEnd -= ParentFormOnResizeEnd;

        _parentForm = FindForm();
        if (_parentForm != null)
            _parentForm.ResizeEnd += ParentFormOnResizeEnd;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _parentForm != null)
            _parentForm.ResizeEnd -= ParentFormOnResizeEnd;
        base.Dispose(disposing);
    }

    void ParentFormOnResizeEnd(object? sender, EventArgs e)
    {
        if (_context.SelectedMovie != null)
            RefreshSessions();
    }

    Control CreateCinemaBlock(string cinemaName, IReadOnlyList<ScreeningSummary> sessions, int width)
    {
        var container = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 18),
            Padding = new Padding(16, 14, 16, 14),
            BackColor = ThemeColors.PanelBackground,
            Width = width
        };
        container.MinimumSize = new Size(width, 0);
        container.MaximumSize = new Size(width, int.MaxValue);

        var title = new Label
        {
            Text = cinemaName,
            Font = ThemeTypography.Heading2,
            ForeColor = ThemeColors.TextPrimary,
            AutoSize = true
        };

        var sessionsPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 12, 0, 0),
            Padding = new Padding(0),
            BackColor = Color.Transparent,
            Width = width - container.Padding.Horizontal
        };
        sessionsPanel.MaximumSize = new Size(width - container.Padding.Horizontal, int.MaxValue);

        foreach (var session in sessions.OrderBy(s => s.StartUtc))
            sessionsPanel.Controls.Add(CreateSessionCard(session));

        container.Controls.Add(title);
        container.Controls.Add(sessionsPanel);

        return container;
    }

    Control CreateSessionCard(ScreeningSummary summary)
    {
        var card = new Panel
        {
            Width = 185,
            Height = 78,
            Margin = new Padding(6),
            BackColor = ThemeColors.PanelElevated
        };

        var indicator = new Panel
        {
            BackColor = GetClassColor(summary.Class),
            Width = 5,
            Dock = DockStyle.Left
        };

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 10, 12, 6)
        };

        var timeLabel = new Label
        {
            Text = summary.StartUtc.ToLocalTime().ToString("h:mm tt"),
            ForeColor = ThemeColors.TextPrimary,
            Font = ThemeTypography.Heading3,
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 24
        };

        var classLabel = new Label
        {
            Text = FormatClass(summary.Class),
            ForeColor = ThemeColors.TextSecondary,
            Font = ThemeTypography.Body2,
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 20
        };

        content.Controls.Add(new Panel { Height = 4, Dock = DockStyle.Top });
        content.Controls.Add(classLabel);
        content.Controls.Add(new Panel { Height = 4, Dock = DockStyle.Top });
        content.Controls.Add(timeLabel);

        card.Controls.Add(content);
        card.Controls.Add(indicator);

        AttachClickHandler(card, () => ScreeningSelected?.Invoke(this, summary.ScreeningId));

        return card;
    }

    void RefreshDateTabs()
    {
        _dateTabs.Controls.Clear();
        var movie = _context.SelectedMovie;
        if (movie is null)
        {
            _dateTabs.Visible = false;
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tomorrow = today.AddDays(1);

        var dates = _screenings.GetByMovie(movie.Id)
            .Select(s => DateOnly.FromDateTime(s.StartUtc))
            .Distinct()
            .Select(d => new
            {
                Date = d,
                Order = d == today ? 0 :
                        d == tomorrow ? 1 :
                        d < today ? 3 : 2
            })
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Date)
            .Select(x => x.Date)
            .ToList();

        if (!dates.Any())
        {
            _dateTabs.Visible = false;
            return;
        }

        _dateTabs.Visible = true;

        if (!_selectedDate.HasValue && dates.Contains(today))
            _selectedDate = today;
        else if (_selectedDate.HasValue && !dates.Contains(_selectedDate.Value))
            _selectedDate = dates.First();

        foreach (var date in dates)
        {
            var dayText = FormatDateTabLabel(date);
            var button = new Button
            {
                Text = dayText,
                Tag = date,
                AutoSize = true,
                Margin = new Padding(4, 0, 4, 0),
                BackColor = _selectedDate == date ? Color.FromArgb(252, 163, 17) : Color.FromArgb(35, 47, 71),
                ForeColor = _selectedDate == date ? Color.Black : Color.White,
                FlatStyle = FlatStyle.Flat
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (_, _) =>
            {
                _selectedDate = _selectedDate == date ? null : date;
                RefreshDateTabs();
                RefreshSessions();
            };
            _dateTabs.Controls.Add(button);
        }
    }

    static void AttachClickHandler(Control control, Action action)
    {
        void Handler(object? _, EventArgs __) => action();
        control.Cursor = Cursors.Hand;
        control.Click += Handler;
        foreach (Control child in control.Controls)
            AttachClickHandler(child, action);
    }

    static string FormatClass(ScreeningClass @class) => @class switch
    {
        ScreeningClass.GoldClass => "Gold Class",
        ScreeningClass.VMax => "V-Max",
        ScreeningClass.Deluxe => "Deluxe",
        _ => "Original"
    };

    static Color GetClassColor(ScreeningClass @class) => @class switch
    {
        ScreeningClass.GoldClass => Color.Goldenrod,
        ScreeningClass.VMax => Color.SlateGray,
        ScreeningClass.Deluxe => Color.MediumOrchid,
        _ => Color.FromArgb(223, 83, 73)
    };

    static string FormatDateTabLabel(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (date == today) return "Today";
        if (date == today.AddDays(1)) return "Tomorrow";
        return date.ToString("ddd dd MMM");
    }
}
