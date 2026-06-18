using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace CassaEventiAI.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;

    protected static void ShowError(string msg)
        => System.Windows.MessageBox.Show(msg, "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);

    protected static bool Confirm(string msg, string title = "Conferma")
        => System.Windows.MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
