using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CassaEventiAI.Models;
using CassaEventiAI.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace CassaEventiAI.ViewModels;

public partial class StartupViewModel : BaseViewModel
{
    private readonly EventService _events;
    private readonly ConfigService _config;
    private readonly AuthService _auth;
    private readonly UpdateService _update;

    public StartupViewModel(EventService events, ConfigService config, AuthService auth, UpdateService update)
    {
        _events = events; _config = config; _auth = auth; _update = update;
        HasActiveEvent = !string.IsNullOrEmpty(App.CurrentSettings.ActiveDbPath)
                         && File.Exists(App.CurrentSettings.ActiveDbPath);
        ActiveEventName = App.CurrentSettings.ActiveEventName;
        ArchivedEvents = new(_events.GetArchivedEvents());
        IsFirstRun = _auth.GetAllOperators().Count == 0;
    }

    public string AppVersion => UpdateService.CurrentVersionString;

    [ObservableProperty] private bool _hasActiveEvent;
    [ObservableProperty] private bool _isFirstRun;
    [ObservableProperty] private string _activeEventName = string.Empty;
    [ObservableProperty] private string _newEventName = string.Empty;
    [ObservableProperty] private ObservableCollection<ArchivedEventInfo> _archivedEvents = [];
    [ObservableProperty] private string _updateStatusText = string.Empty;
    [ObservableProperty] private bool _updateAvailable;

    // First admin setup (only shown when no operators exist yet)
    [ObservableProperty] private string _adminUsername = "admin";
    [ObservableProperty] private string _adminDisplayName = string.Empty;
    [ObservableProperty] private string _adminPassword = string.Empty;

    public event Action? ProceedToLogin;

    public async Task CheckForUpdatesAsync()
    {
        var release = await _update.CheckForUpdateAsync();
        if (release is null) return;

        UpdateStatusText = $"Aggiornamento disponibile: {release.TagName}";
        UpdateAvailable = true;

        var result = System.Windows.MessageBox.Show(
            $"È disponibile la versione {release.TagName}.\nVuoi scaricare e installare l'aggiornamento?",
            "Aggiornamento disponibile",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (result != MessageBoxResult.Yes) return;

        UpdateStatusText = "Download in corso...";
        await _update.DownloadAndInstallAsync(release,
            pct => System.Windows.Application.Current.Dispatcher.Invoke(() => UpdateStatusText = $"Download: {pct}%"));

        System.Windows.Application.Current.Shutdown();
    }

    [RelayCommand]
    private void ContinueWithActiveEvent()
    {
        _events.OpenEvent(App.CurrentSettings.ActiveEventName, App.CurrentSettings.ActiveDbPath);
        ProceedToLogin?.Invoke();
    }

    [RelayCommand]
    private void CreateAndStart()
    {
        if (string.IsNullOrWhiteSpace(NewEventName)) { ShowError("Inserisci il nome dell'evento."); return; }

        if (IsFirstRun)
        {
            if (string.IsNullOrWhiteSpace(AdminUsername)) { ShowError("Inserisci lo username amministratore."); return; }
            if (string.IsNullOrWhiteSpace(AdminDisplayName)) { ShowError("Inserisci il nome visualizzato."); return; }
            if (AdminPassword.Length < 6) { ShowError("La password deve contenere almeno 6 caratteri."); return; }

            _auth.CreateFirstAdmin(AdminUsername, AdminDisplayName, AdminPassword);
            AdminPassword = string.Empty;
        }

        _events.CreateNewEvent(NewEventName);
        ProceedToLogin?.Invoke();
    }

    [RelayCommand]
    private void OpenArchived(ArchivedEventInfo ev)
    {
        _events.ReopenArchivedEvent(ev);
        ProceedToLogin?.Invoke();
    }
}
