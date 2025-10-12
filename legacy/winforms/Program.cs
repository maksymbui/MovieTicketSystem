using System.Windows.Forms;

namespace WinFormsApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Logic.DataStore.Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load data.\n{ex.Message}", "Movie Tickets", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}
