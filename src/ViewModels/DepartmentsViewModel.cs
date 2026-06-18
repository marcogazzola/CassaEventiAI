using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CplCassaEventi.Models;
using CplCassaEventi.Services;
using System.Collections.ObjectModel;

namespace CplCassaEventi.ViewModels;

public partial class DepartmentsViewModel(ProductService products) : BaseViewModel
{
    [ObservableProperty] private ObservableCollection<Department> _departments = [];
    [ObservableProperty] private Department? _selected;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editColor = "#378ADD";
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private bool _editPrintSeparate;
    [ObservableProperty] private int _editSortOrder;
    [ObservableProperty] private bool _isNew;

    public void Load() => Departments = new(products.GetDepartments(activeOnly: false));

    [RelayCommand] private void New()
    {
        Selected = null; EditName = ""; EditColor = "#378ADD"; EditIsActive = true;
        EditPrintSeparate = false; EditSortOrder = Departments.Count; IsNew = true; IsEditing = true;
    }

    [RelayCommand] private void Edit(Department d)
    {
        Selected = d; EditName = d.Name; EditColor = string.IsNullOrWhiteSpace(d.Color) ? "#378ADD" : d.Color;
        EditIsActive = d.IsActive; EditPrintSeparate = d.PrintSeparateReceipt;
        EditSortOrder = d.SortOrder; IsNew = false; IsEditing = true;
    }

    [RelayCommand] private void Cancel() { IsEditing = false; }

    [RelayCommand] private void Save()
    {
        if (string.IsNullOrWhiteSpace(EditName)) { StatusMessage = "Nome obbligatorio."; return; }
        var d = IsNew ? new Department() : (Selected ?? new Department());
        d.Name = EditName.Trim(); d.Color = string.IsNullOrWhiteSpace(EditColor) ? "#378ADD" : EditColor.Trim();
        d.IsActive = EditIsActive; d.PrintSeparateReceipt = EditPrintSeparate; d.SortOrder = EditSortOrder;
        products.SaveDepartment(d);
        Load(); IsEditing = false; StatusMessage = $"Reparto '{d.Name}' salvato.";
    }

    [RelayCommand] private void Delete(Department d)
    {
        if (!Confirm($"Eliminare il reparto '{d.Name}'?\nGli articoli associati non verranno eliminati.")) return;
        products.DeleteDepartment(d.Id);
        Load(); StatusMessage = $"Reparto '{d.Name}' eliminato.";
    }

    [RelayCommand] private void MoveUp(Department d)
    {
        var list = Departments.ToList();
        var idx = list.IndexOf(d);
        if (idx <= 0) return;
        list[idx].SortOrder--; list[idx - 1].SortOrder++;
        products.SaveDepartment(list[idx]); products.SaveDepartment(list[idx - 1]);
        Load();
    }

    [RelayCommand] private void MoveDown(Department d)
    {
        var list = Departments.ToList();
        var idx = list.IndexOf(d);
        if (idx < 0 || idx >= list.Count - 1) return;
        list[idx].SortOrder++; list[idx + 1].SortOrder--;
        products.SaveDepartment(list[idx]); products.SaveDepartment(list[idx + 1]);
        Load();
    }
}
