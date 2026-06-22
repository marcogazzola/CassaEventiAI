using CassaEventiAI.Models;
using CassaEventiAI.Services;
using System.Collections.Generic;
using System.Windows;

namespace CassaEventiAI.Views.Shared;

public partial class OrderSummaryWindow : Window
{
    private readonly Sale _sale;
    private readonly PrintingService _printing;
    private readonly List<Department> _departments;

    public OrderSummaryWindow(Sale sale, PrintingService printing, List<Department> departments)
    {
        InitializeComponent();
        _sale = sale;
        _printing = printing;
        _departments = departments;

        TitleBlock.Text = $"Scontrino #{sale.Id:D4} emesso";
        SubtitleBlock.Text = $"{sale.CreatedAt:dd/MM/yyyy HH:mm}  ·  {sale.OperatorName}";
        ItemsGrid.ItemsSource = sale.Items;
        TotalBlock.Text = $"€ {sale.Total:F2}";

        var payment = $"Pagamento: {sale.PaymentMethodLabel}";
        if (sale.CashGiven > 0)
            payment += $"  ·  Dato: € {sale.CashGiven:F2}  ·  Resto: € {sale.Change:F2}";
        PaymentBlock.Text = payment;
    }

    private void Reprint_Click(object sender, RoutedEventArgs e)
        => _printing.ReprintLast(_sale, null, _departments);

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}
