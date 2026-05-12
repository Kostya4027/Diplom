using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamTickets.Core.Services;
using ExamTickets.WPF.Helpers;
using ExamTickets.WPF.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ExamTickets.WPF.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;

    [ObservableProperty]
    private string login = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    // passwordFromUi позволяет передать PasswordBox.Password напрямую (если вызываете ExecuteAsync(password))
    [RelayCommand]
    private async Task LoginAsync(string? passwordFromUi = null)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var rawPassword = string.IsNullOrWhiteSpace(passwordFromUi) ? Password : passwordFromUi;
            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(rawPassword))
            {
                ErrorMessage = "Введите логин и пароль.";
                MessageBox.Show(ErrorMessage, "Вход", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = await _authService.LoginAsync(Login.Trim(), rawPassword);
            if (user is null)
            {
                ErrorMessage = "Неверный логин или пароль.";
                MessageBox.Show(ErrorMessage, "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!user.IsActive)
            {
                ErrorMessage = "Учётная запись отключена.";
                MessageBox.Show(ErrorMessage, "Доступ закрыт", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CurrentUser.SetUser(user);

            var mainWindow = App.Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = App.Services.GetRequiredService<MainViewModel>();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();

            foreach (Window w in Application.Current.Windows)
            {
                if (w is LoginWindow)
                {
                    w.Close();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            MessageBox.Show(ex.Message, "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}