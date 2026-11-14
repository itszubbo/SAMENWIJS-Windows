using System.Windows;
using Microsoft.Web.WebView2.Core;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using StartupEventArgs = System.Windows.StartupEventArgs;

namespace SAMENWIJS.Windows;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var splash = new SplashWindow();
        MainWindow = splash;
        splash.Show();

        try
        {
            _ = CoreWebView2Environment.GetAvailableBrowserVersionString();
            var window = new MainWindow { Opacity = 0, ShowInTaskbar = false };
            window.Show();
            await window.InitializeAsync();

            MainWindow = window;
            ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;
            window.ShowInTaskbar = true;
            window.Opacity = 1;
            window.Activate();
            splash.Close();
        }
        catch (WebView2RuntimeNotFoundException)
        {
            splash.Close();
            MessageBox.Show(
                "SAMENWIJS heeft Microsoft Edge WebView2 nodig. Installeer de gratis WebView2 Runtime en start SAMENWIJS opnieuw.\n\nhttps://developer.microsoft.com/microsoft-edge/webview2/",
                "SAMENWIJS kan niet starten",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
        }
        catch (Exception exception)
        {
            splash.Close();
            MessageBox.Show(
                "SAMENWIJS kon niet goed starten. Probeer de app opnieuw te openen.\n\n" + exception.Message,
                "SAMENWIJS kan niet starten",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }
}
