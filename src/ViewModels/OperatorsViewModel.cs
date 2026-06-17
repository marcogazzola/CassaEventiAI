using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CplCassaEventi.Models;
using CplCassaEventi.Services;
using System.Collections.ObjectModel;

namespace CplCassaEventi.ViewModels;

public partial class OperatorsViewModel(AuthService auth) : BaseViewModel
{
    [ObservableProperty] private ObservableCollection<Operator> _operators = [];
    [ObservableProperty] private Operator? _selectedOperator;
    [ObservableProperty] private bool _isEditing;

    // Edit form fields
    [ObservableProperty] private string _editUsername = string.Empty;
    [ObservableProperty] private string _editDisplayName = string.Empty;
    [ObservableProperty] private string _editRole = "cashier";
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private string _editNewPassword = string.Empty;
    [ObservableProperty] private string _editConfirmPassword = string.Empty;
    [ObservableProperty] private bool _isNewOperator;

    public IEnumerable<string> AvailableRoles { get; } = ["cashier", "admin"];

    public void Load() => Operators = new(auth.GetAllOperators());

    [RelayCommand]
    private void NewOperator()
    {
        SelectedOperator = null;
        EditUsername = string.Empty;
        EditDisplayName = string.Empty;
        EditRole = "cashier";
        EditIsActive = true;
        EditNewPassword = string.Empty;
        EditConfirmPassword = string.Empty;
        IsNewOperator = true;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditOperator(Operator op)
    {
        SelectedOperator = op;
        EditUsername = op.Username;
        EditDisplayName = op.DisplayName;
        EditRole = op.Role;
        EditIsActive = op.IsActive;
        EditNewPassword = string.Empty;
        EditConfirmPassword = string.Empty;
        IsNewOperator = false;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        EditNewPassword = string.Empty;
        EditConfirmPassword = string.Empty;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void SaveOperator()
    {
        if (string.IsNullOrWhiteSpace(EditUsername)) { StatusMessage = "Username obbligatorio."; return; }
        if (string.IsNullOrWhiteSpace(EditDisplayName)) { StatusMessage = "Nome visualizzato obbligatorio."; return; }

        // Validate password only if provided or new operator
        if (IsNewOperator && string.IsNullOrWhiteSpace(EditNewPassword))
            { StatusMessage = "La password è obbligatoria per i nuovi utenti."; return; }

        if (!string.IsNullOrWhiteSpace(EditNewPassword))
        {
            if (EditNewPassword.Length < 6) { StatusMessage = "La password deve avere almeno 6 caratteri."; return; }
            if (EditNewPassword != EditConfirmPassword) { StatusMessage = "Le password non coincidono."; return; }
        }

        var op = IsNewOperator
            ? new Operator { MustChangePassword = true }
            : (SelectedOperator ?? new Operator());

        op.Username = EditUsername.Trim();
        op.DisplayName = EditDisplayName.Trim();
        op.Role = EditRole;
        op.IsActive = EditIsActive;

        if (!string.IsNullOrWhiteSpace(EditNewPassword))
            op.PasswordHash = BCrypt.Net.BCrypt.HashPassword(EditNewPassword);

        try
        {
            auth.SaveOperator(op);
            Load();
            IsEditing = false;
            EditNewPassword = string.Empty; EditConfirmPassword = string.Empty;
            StatusMessage = $"Operatore '{op.DisplayName}' salvato.";
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
    }

    [RelayCommand]
    private void ResetPassword(Operator op)
    {
        if (!Confirm($"Reimpostare la password di '{op.DisplayName}'?\nAll'accesso successivo verrà richiesta una nuova password."))
            return;
        // Set a temporary hash and force change on next login
        var ops = auth.GetAllOperators();
        var target = ops.First(o => o.Id == op.Id);
        target.MustChangePassword = true;
        auth.SaveOperator(target);
        Load();
        StatusMessage = $"Password di '{op.DisplayName}' reimpostata. L'utente dovrà cambiarla al prossimo accesso.";
    }

    [RelayCommand]
    private void ToggleActive(Operator op)
    {
        try
        {
            var ops = auth.GetAllOperators();
            var target = ops.First(o => o.Id == op.Id);
            target.IsActive = !target.IsActive;
            auth.SaveOperator(target);
            Load();
            StatusMessage = $"Operatore '{op.DisplayName}' {(target.IsActive ? "abilitato" : "disabilitato")}.";
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
    }

    [RelayCommand]
    private void DeleteOperator(Operator op)
    {
        if (!Confirm($"Eliminare definitivamente l'operatore '{op.DisplayName}'?")) return;
        try
        {
            auth.DeleteOperator(op.Id);
            Load();
            StatusMessage = $"Operatore '{op.DisplayName}' eliminato.";
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
    }
}
