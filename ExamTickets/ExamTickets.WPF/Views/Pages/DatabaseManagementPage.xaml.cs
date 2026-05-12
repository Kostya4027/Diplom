using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ExamTickets.WPF.Views.Pages;

public partial class DatabaseManagementPage : Page
{
    public DatabaseManagementPage()
    {
        InitializeComponent();
    }

    private void DataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
    }
}