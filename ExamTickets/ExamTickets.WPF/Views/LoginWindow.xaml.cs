using System.Windows;
using ExamTickets.WPF.ViewModels;

namespace ExamTickets.WPF.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.Password = PasswordBox.Password;
            await viewModel.LoginCommand.ExecuteAsync(null);
        }
    }
}