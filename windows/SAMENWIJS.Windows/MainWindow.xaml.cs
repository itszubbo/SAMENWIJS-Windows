using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using WinForms = System.Windows.Forms;

namespace SAMENWIJS.Windows;

public partial class MainWindow : Window
{
    private const string AppHost = "samenwijs.local";
    private readonly Dictionary<string, System.Threading.Timer> _reminders = new(StringComparer.Ordinal);
    private readonly object _reminderGate = new();
    private readonly SpeechSynthesizer _speech = new();
    private readonly WinForms.NotifyIcon _trayIcon;
    private bool _allowClose;
    private bool _speechNoticeShown;

    public MainWindow()
    {
        InitializeComponent();
        var exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        var icon = exe is not null ? System.Drawing.Icon.ExtractAssociatedIcon(exe) : null;
        _trayIcon = new WinForms.NotifyIcon
        {
            Icon = icon ?? System.Drawing.SystemIcons.Application,
            Text = "SAMENWIJS — seintjes",
            Visible = true,
            ContextMenuStrip = new WinForms.ContextMenuStrip()
        };
        _trayIcon.ContextMenuStrip.Items.Add("SAMENWIJS openen", null, (_, _) => OpenFromTray());
        _trayIcon.ContextMenuStrip.Items.Add("Afsluiten", null, (_, _) => Dispatcher.BeginInvoke(new Action(Close)));
        _trayIcon.DoubleClick += (_, _) => OpenFromTray();
    }

    public async Task InitializeAsync()
    {
        var userData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SAMENWIJS", "WebView2");
        Directory.CreateDirectory(userData);
        AppView.CreationProperties = new Microsoft.Web.WebView2.Wpf.CoreWebView2CreationProperties { UserDataFolder = userData };
        await AppView.EnsureCoreWebView2Async();

        var core = AppView.CoreWebView2;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.SetVirtualHostNameToFolderMapping(AppHost, Path.Combine(AppContext.BaseDirectory, "wwwroot"), CoreWebView2HostResourceAccessKind.DenyCors);
        core.WebMessageReceived += OnWebMessageReceived;
        core.NavigationStarting += OnNavigationStarting;
        await core.AddScriptToExecuteOnDocumentCreatedAsync(DesktopBridgeScript);
        core.Navigate($"https://{AppHost}/index.html");
    }

    private const string DesktopBridgeScript = """
        (() => {
          const send = (action, values = {}) => window.chrome.webview.postMessage({ action, ...values });
          window.NativeApp = Object.freeze({
            shareText: text => send('shareText', { text: String(text || '') }),
            dial: phone => send('dial', { phone: String(phone || '') }),
            speakText: text => send('speakText', { text: String(text || '') }),
            scheduleReminder: (id, title, trigger, minutes) => send('scheduleReminder', {
              id: String(id || ''), title: String(title || ''), trigger: Number(trigger), minutes: Number(minutes)
            }),
            cancelReminder: id => send('cancelReminder', { id: String(id || '') }),
            restoreReminders: plans => send('restoreReminders', { plans: String(plans || '{}') })
          });
        })();
        """;

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) &&
            uri.Scheme == "https" && string.Equals(uri.Host, AppHost, StringComparison.OrdinalIgnoreCase)) return;
        e.Cancel = true;
        if (Uri.TryCreate(e.Uri, UriKind.Absolute, out uri) && uri.Scheme is "https" or "http" or "mailto" or "tel")
        {
            try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
            catch { StatusText.Text = "Windows kon deze link niet openen."; }
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var message = JsonDocument.Parse(e.WebMessageAsJson);
            var root = message.RootElement;
            var action = Text(root, "action");
            switch (action)
            {
                case "shareText": ShareText(Text(root, "text")); break;
                case "dial": OpenDialer(Text(root, "phone")); break;
                case "speakText": Speak(Text(root, "text")); break;
                case "scheduleReminder":
                    ScheduleReminder(Text(root, "id"), Text(root, "title"), Number(root, "trigger"), Int(root, "minutes"));
                    break;
                case "cancelReminder": CancelReminder(Text(root, "id")); break;
                case "restoreReminders": RestoreReminders(Text(root, "plans")); break;
            }
        }
        catch
        {
            StatusText.Text = "Deze actie kon niet worden uitgevoerd. Probeer het opnieuw.";
        }
    }

    private static string Text(JsonElement value, string key) =>
        value.TryGetProperty(key, out var item) && item.ValueKind == JsonValueKind.String ? item.GetString() ?? "" : "";

    private static long Number(JsonElement value, string key) =>
        value.TryGetProperty(key, out var item) && item.TryGetInt64(out var result) ? result : 0;

    private static int Int(JsonElement value, string key) =>
        value.TryGetProperty(key, out var item) && item.TryGetInt32(out var result) ? result : 0;

    private void ShareText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (text.Contains("\"format\": \"SAMENWIJS\"", StringComparison.Ordinal))
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Bewaar mijn SAMENWIJS-gegevens",
                FileName = "mijn-samenwijs-gegevens.json",
                Filter = "SAMENWIJS-gegevens (*.json)|*.json|Tekstbestand (*.txt)|*.txt",
                DefaultExt = ".json",
                AddExtension = true
            };
            if (dialog.ShowDialog(this) == true)
            {
                File.WriteAllText(dialog.FileName, text, new System.Text.UTF8Encoding(false));
                StatusText.Text = "Je gegevensbestand is bewaard op de gekozen plek.";
            }
            return;
        }

        if (MessageBox.Show(this, "Wil je deze hulpkaart naar het klembord kopiëren? Daarna kun je hem zelf plakken waar je wilt delen.",
                "Hulpkaart delen", MessageBoxButton.OKCancel, MessageBoxImage.Information, MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
        System.Windows.Clipboard.SetText(text);
        StatusText.Text = "De hulpkaart staat op het klembord. Jij kiest waar je hem plakt.";
        MessageBox.Show(this, "De hulpkaart is gekopieerd. Je kunt hem nu zelf plakken.", "SAMENWIJS", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenDialer(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return;
        try { Process.Start(new ProcessStartInfo("tel:" + Uri.EscapeDataString(phone)) { UseShellExecute = true }); }
        catch
        {
            System.Windows.Clipboard.SetText(phone);
            MessageBox.Show(this, "Windows heeft geen bel-app ingesteld. Het telefoonnummer staat op het klembord zodat je het zelf kunt gebruiken.",
                "Telefoonnummer", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        try
        {
            var voice = _speech.GetInstalledVoices().FirstOrDefault(v => v.Enabled &&
                v.VoiceInfo.Culture.Name.StartsWith("nl", StringComparison.OrdinalIgnoreCase));
            if (voice is null)
            {
                if (!_speechNoticeShown)
                {
                    _speechNoticeShown = true;
                    MessageBox.Show(this, "Er staat geen Nederlandse voorleesstem op deze computer. Je kunt een stem toevoegen via de Windows-instellingen voor spraak.",
                        "Voorlezen", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                return;
            }
            _speech.SelectVoice(voice.VoiceInfo.Name);
            _speech.SpeakAsyncCancelAll();
            _speech.SpeakAsync(text);
        }
        catch { StatusText.Text = "Voorlezen is op deze computer niet beschikbaar."; }
    }

    private void ScheduleReminder(string id, string title, long triggerAtMillis, int minutes)
    {
        if (string.IsNullOrWhiteSpace(id) || triggerAtMillis <= 0) return;
        CancelReminder(id);
        var trigger = DateTimeOffset.FromUnixTimeMilliseconds(triggerAtMillis);
        var due = trigger - DateTimeOffset.Now;
        if (due <= TimeSpan.Zero) return;
        if (due > TimeSpan.FromDays(49)) due = TimeSpan.FromDays(49);

        var timer = new System.Threading.Timer(_ => Dispatcher.BeginInvoke(new Action(() =>
        {
            if (DateTimeOffset.Now < trigger)
            {
                ScheduleReminder(id, title, triggerAtMillis, minutes);
                return;
            }
            CancelReminder(id);
            _trayIcon.ShowBalloonTip(10000, "SAMENWIJS",
                title + " begint over " + minutes.ToString(CultureInfo.InvariantCulture) + " minuten.", WinForms.ToolTipIcon.Info);
        })), null, due, Timeout.InfiniteTimeSpan);
        lock (_reminderGate) _reminders[id] = timer;
    }

    private void CancelReminder(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        lock (_reminderGate)
            if (_reminders.Remove(id, out var oldTimer)) oldTimer.Dispose();
    }

    private void RestoreReminders(string plansJson)
    {
        lock (_reminderGate)
        {
            foreach (var timer in _reminders.Values) timer.Dispose();
            _reminders.Clear();
        }
        using var plans = JsonDocument.Parse(string.IsNullOrWhiteSpace(plansJson) ? "{}" : plansJson);
        foreach (var day in plans.RootElement.EnumerateObject())
        {
            if (!DateTime.TryParseExact(day.Name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                || day.Value.ValueKind != JsonValueKind.Array) continue;
            foreach (var item in day.Value.EnumerateArray())
            {
                if (!item.TryGetProperty("reminder", out var reminder) || !reminder.TryGetInt32(out var minutes) || minutes <= 0
                    || item.TryGetProperty("done", out var done) && done.ValueKind == JsonValueKind.True) continue;
                if (!item.TryGetProperty("time", out var timeValue) || timeValue.ValueKind != JsonValueKind.String
                    || !TimeSpan.TryParseExact(timeValue.GetString(), "hh\\:mm", CultureInfo.InvariantCulture, out var time)) continue;
                var id = Text(item, "reminderId");
                if (string.IsNullOrWhiteSpace(id)) continue;
                var title = Text(item, "title");
                var localTrigger = DateTime.SpecifyKind(date.Date + time - TimeSpan.FromMinutes(minutes), DateTimeKind.Local);
                ScheduleReminder(id, string.IsNullOrWhiteSpace(title) ? "Je activiteit" : title,
                    new DateTimeOffset(localTrigger).ToUnixTimeMilliseconds(), minutes);
            }
        }
    }

    private void OpenFromTray()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }));
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;
        var answer = MessageBox.Show(this,
            "Wil je SAMENWIJS helemaal afsluiten? De seintjes stoppen dan. Kies Nee om het venster te minimaliseren en SAMENWIJS actief te laten.",
            "SAMENWIJS afsluiten", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (answer == MessageBoxResult.No)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            return;
        }

        _allowClose = true;
        lock (_reminderGate)
        {
            foreach (var timer in _reminders.Values) timer.Dispose();
            _reminders.Clear();
        }
        _speech.SpeakAsyncCancelAll();
        _speech.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        AppView.Dispose();
    }
}
