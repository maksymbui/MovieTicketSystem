namespace WinFormsApp
{
    public partial class Form1 : Form
    {
        readonly Models.BookingContext _context = new();
        Views.HomeControl? _home;
        Views.MovieDetailControl? _detail;
        Views.SeatSelectionControl? _seatSelection;

        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ShowHome();
        }

        void ShowHome()
        {
            _home ??= new Views.HomeControl();
            _home.MovieSelected -= OnMovieSelected;
            _home.MovieSelected += OnMovieSelected;
            ShowControl(_home);
        }

        void ShowMovieDetail(Entities.Movie movie)
        {
            _detail ??= new Views.MovieDetailControl(_context);
            _detail.BackRequested -= OnBackRequested;
            _detail.BackRequested += OnBackRequested;
            _detail.ScreeningSelected -= OnScreeningSelected;
            _detail.ScreeningSelected += OnScreeningSelected;
            _detail.BindMovie(movie);
            ShowControl(_detail);
        }

        void ShowControl(Control control)
        {
            contentPanel.SuspendLayout();
            contentPanel.Controls.Clear();
            control.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(control);
            contentPanel.ResumeLayout();
        }

        void OnMovieSelected(object? sender, Entities.Movie movie) => ShowMovieDetail(movie);

        void OnBackRequested(object? sender, EventArgs e) => ShowHome();

        void OnScreeningSelected(object? sender, string screeningId)
        {
            _context.SetScreening(screeningId);
            ShowSeatSelection(screeningId);
        }

        void ShowSeatSelection(string screeningId)
        {
            _seatSelection = new Views.SeatSelectionControl(_context, screeningId);
            _seatSelection.BackRequested += (_, _) =>
            {
                if (_context.SelectedMovie is not null)
                    ShowMovieDetail(_context.SelectedMovie);
                else
                    ShowHome();
            };
            _seatSelection.ProceedRequested += (_, _) =>
            {
                var seats = string.Join(", ", _context.SelectedSeats);
                MessageBox.Show(seats.Length == 0
                        ? "No seats selected yet."
                        : $"Seats selected: {seats}\n(Proceed to checkout coming soon.)",
                    "Movie Ticket System",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };
            ShowControl(_seatSelection);
        }
    }
}
