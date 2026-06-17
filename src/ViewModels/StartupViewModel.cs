using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CplCassaEventi.Models;
using CplCassaEventi.Services;
using System.Collections.ObjectModel;
using System.IO;

namespace CplCassaEventi.ViewModels;

public partial class StartupViewModel : BaseViewModel
{
    private readonly EventService _events;
    private readonly ConfigService _config;
    private readonly AuthService _auth;

    public StartupViewModel(EventService events, ConfigService config, AuthService auth)
    {
        _events = events; _config = config; _auth = auth;
        HasActiveEvent = !string.IsNullOrEmpty(App.CurrentSettings.ActiveDbPath)
                         && File.Exists(App.CurrentSettings.ActiveDbPath);
        ActiveEventName = App.CurrentSettings.ActiveEventName;
        ArchivedEvents = new(_events.GetArchivedEvents());
        IsFirstRun = _auth.GetAllOperators().Count == 0;
    }

    [ObservableProperty] private bool _hasActiveEvent;
    [ObservableProperty] private bool _isFirstRun;
    [ObservableProperty] private string _activeEventName = string.Empty;
    [ObservableProperty] private string _newEventName = string.Empty;
    [ObservableProperty] private ObservableCollection<(string Name, string Path, DateTime Date)> _archivedEvents = [];

    // First admin setup (only shown when no operators exist yet)
    [ObservableProperty] private string _adminUsername = "admin";
    [ObservableProperty] private string _adminDisplayName = string.Empty;
    [ObservableProperty] private string _adminPassword = string.Empty;

    public event Action? ProceedToLogin;

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
    private void OpenArchived((string Name, string Path, DateTime Date) ev)
    {
        _events.OpenEvent(ev.Name, ev.Path);
        ProceedToLogin?.Invoke();
    }
}
