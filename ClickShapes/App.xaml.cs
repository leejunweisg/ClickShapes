using Serilog;
using System.Windows;

namespace ClickShapes
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Initialise logger
            Log.Logger = new LoggerConfiguration()
                        .WriteTo.Debug()
                        .MinimumLevel.Debug()
                        .CreateLogger();
        }

    }
}
