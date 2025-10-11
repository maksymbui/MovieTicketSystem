using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp.Entities;
using WinFormsApp.Logic;
using WinFormsApp.Styling;
using WinFormsApp.Utils;

namespace WinFormsApp.Views;

internal sealed class HomeControl : UserControl
{
    readonly MovieCatalogService _catalog = new();
    readonly FlowLayoutPanel _moviePanel = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        Padding = new Padding(16),
        BackColor = ThemeColors.PrimaryBackground
    };

    readonly TextBox _searchBox = new()
    {
        PlaceholderText = "Search movies...",
        Width = 320
    };

    readonly Button _clearButton = new()
    {
        Text = "Clear",
        AutoSize = true,
        Margin = new Padding(8, 0, 0, 0)
    };

    public event EventHandler<Movie>? MovieSelected;

    public HomeControl()
    {
        BackColor = ThemeColors.PrimaryBackground;
        Dock = DockStyle.Fill;

        var header = new Label
        {
            Text = "Now Showing",
            Dock = DockStyle.Top,
            Height = 54,
            Font = ThemeTypography.Heading1,
            ForeColor = ThemeColors.TextPrimary,
            Padding = new Padding(18, 18, 0, 0)
        };

        var searchPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 72,
            Padding = new Padding(18, 16, 18, 12),
            BackColor = ThemeColors.SecondaryBackground
        };

        _searchBox.TextChanged += (_, _) => DebouncedRefresh();
        _clearButton.Click += (_, _) =>
        {
            _searchBox.Text = "";
            RefreshMovies();
        };

        _searchBox.ApplySearchBoxTheme();
        _clearButton.ApplyPrimaryButtonTheme();

        searchPanel.Controls.Add(_searchBox);
        searchPanel.Controls.Add(_clearButton);

        Controls.Add(_moviePanel);
        Controls.Add(searchPanel);
        Controls.Add(header);

        RefreshMovies();
    }

    readonly System.Windows.Forms.Timer _debounceTimer = new() { Interval = 250 };

    void DebouncedRefresh()
    {
        _debounceTimer.Stop();
        _debounceTimer.Tick -= OnDebounce;
        _debounceTimer.Tick += OnDebounce;
        _debounceTimer.Start();
    }

    void OnDebounce(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();
        RefreshMovies();
    }

    void RefreshMovies()
    {
        var query = _searchBox.Text?.Trim() ?? "";
        var movies = string.IsNullOrWhiteSpace(query)
            ? _catalog.GetAll()
            : _catalog.Search(query);

        _moviePanel.SuspendLayout();
        _moviePanel.Controls.Clear();

        if (movies.Count == 0)
        {
            _moviePanel.Controls.Add(new Label
            {
                Text = "No movies found.",
                ForeColor = ThemeColors.TextSecondary,
                Font = ThemeTypography.Body1,
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(6)
            });
            _moviePanel.ResumeLayout();
            return;
        }

        foreach (var movie in movies)
            _moviePanel.Controls.Add(CreateMovieCard(movie));

        _moviePanel.ResumeLayout();
    }

    Control CreateMovieCard(Movie movie)
    {
        var card = new Panel
        {
            Width = 220,
            Height = 360,
            Margin = new Padding(12),
            Padding = new Padding(12),
            BackColor = ThemeColors.PanelBackground
        };

        var poster = new PictureBox
        {
            Width = 196,
            Height = 260,
            BackColor = ThemeColors.PanelElevated,
            SizeMode = PictureBoxSizeMode.StretchImage,
            Image = PosterGenerator.CreatePlaceholder(movie.Title, 196, 260)
        };

        if (!string.IsNullOrWhiteSpace(movie.PosterUrl))
            LoadPosterAsync(poster, movie.PosterUrl);

        var accentStrip = new Panel
        {
            Dock = DockStyle.Top,
            Height = 6,
            BackColor = ThemeColors.AccentSecondary
        };

        var title = new Label
        {
            Text = movie.Title,
            ForeColor = ThemeColors.TextPrimary,
            Font = ThemeTypography.Heading3,
            Dock = DockStyle.Top,
            Height = 40
        };

        var genre = new Label
        {
            Text = string.Join(", ", movie.Genres),
            ForeColor = ThemeColors.TextSecondary,
            Font = ThemeTypography.Body2,
            Dock = DockStyle.Top,
            Height = 32
        };

        var button = new Button
        {
            Text = "Times & Tickets",
            Dock = DockStyle.Bottom,
            Height = 40
        };
        button.ApplyPrimaryButtonTheme();
        button.Click += (_, _) => MovieSelected?.Invoke(this, movie);

        card.Controls.Add(button);
        card.Controls.Add(new Panel { Height = 6, Dock = DockStyle.Bottom });
        card.Controls.Add(genre);
        card.Controls.Add(title);
        card.Controls.Add(poster);
        card.Controls.Add(accentStrip);
        return card;
    }

    static readonly HttpClient HttpClient = new();

    static async void LoadPosterAsync(PictureBox box, string url)
    {
        try
        {
            using var stream = await HttpClient.GetStreamAsync(url);
            box.Image = Image.FromStream(stream);
        }
        catch
        {
            // ignore, placeholder already set
        }
    }
}
