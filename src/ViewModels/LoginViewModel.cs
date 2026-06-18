using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CassaEventiAI.Services;

namespace CassaEventiAI.ViewModels;

public partial class LoginViewModel(AuthService auth) : BaseViewModel
{
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;

    // Password change flow
    [ObservableProperty] private bool _mustChangePassword;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;

    public event Action? LoginSucceeded;

    [RelayCommand]
    private void Login()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            { ErrorMessage = "Inserisci username e password."; return; }

        var error = auth.Login(Username, Password);
        if (error != null) { ErrorMessage = error; Password = string.Empty; return; }

        Password = string.Empty;

        if (auth.CurrentOperator!.MustChangePassword)
        {
            MustChangePassword = true;
            ErrorMessage = string.Empty;
        }
        else
        {
            LoginSucceeded?.Invoke();
        }
    }

    [RelayCommand]
    private void ConfirmPasswordChange()
    {
        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
            { ErrorMessage = "La password deve contenere almeno 6 caratteri."; return; }
        if (NewPassword != ConfirmPassword)
            { ErrorMessage = "Le password non coincidono."; return; }

        auth.ChangePassword(auth.CurrentOperator!.Id, NewPassword);
        NewPassword = string.Empty; ConfirmPassword = string.Empty;
        MustChangePassword = false;
        LoginSucceeded?.Invoke();
    }

    [RelayCommand]
    private void CancelPasswordChange()
    {
        auth.Logout();
        MustChangePassword = false;
        Username = string.Empty;
    }
}
