using System.Windows;
using System.Windows.Controls;
using ExamTickets.WPF.ViewModels;

namespace ExamTickets.WPF.Views.Pages;

public partial class UserManagementPage : Page
{
    public UserManagementPage()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserManagementViewModel viewModel)
        {
            await viewModel.LoadUsersAsync();
        }
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not UserManagementViewModel viewModel)
        {
            return;
        }

        if (sender is PasswordBox passwordBox)
        {
            viewModel.EditPassword = passwordBox.Password;
        }
    }
}