using System.Windows;

namespace SoundMixer.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show(
                $"Error:\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                "SoundMixer Pro Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        bool startMinimized = false;
        foreach (var arg in e.Args)
        {
            if (arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase))
            {
                startMinimized = true;
                break;
            }
        }

        var window = new MainWindow();

        if (startMinimized)
        {
            window.WindowState = WindowState.Minimized;
            window.ShowActivated = false;
            window.Show();
            window.Hide();
        }
        else
        {
            window.Show();
        }
    }
}