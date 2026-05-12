using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamTickets.Core.Models;
using ExamTickets.WPF.Helpers;
using ExamTickets.WPF.Views;
using ExamTickets.WPF.Views.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace ExamTickets.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private Page? currentPage;

    public bool IsAdmin => CurrentUser.Instance?.Role == UserRole.Admin;
    public bool IsChairman => CurrentUser.Instance?.Role == UserRole.Chairman;
    public bool IsTeacher => CurrentUser.Instance?.Role == UserRole.Teacher;
    public bool IsNotTeacher => !IsTeacher;
    public string UserFullName => CurrentUser.Instance?.FullName ?? string.Empty;

    public MainViewModel()
    {
        NavigateToTicketCreation();
    }

    [RelayCommand]
    private async Task NavigateToDashboard()
    {
        await NavigateToPageAsync<DashboardPage, DashboardViewModel>(viewModel =>
            viewModel.LoadDataCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task NavigateToTicketCreation()
    {
        await NavigateToPageAsync<TicketCreationPage, TicketCreationViewModel>(viewModel =>
            viewModel.LoadDataCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task NavigateToDatabase()
    {
        await NavigateToPageAsync<DatabaseManagementPage, DatabaseManagementViewModel>(viewModel =>
            viewModel.LoadTableDataCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task NavigateToUsers()
    {
        if (!IsAdmin)
        {
            MessageBox.Show("Доступ только для администратора.", "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await NavigateToPageAsync<UserManagementPage, UserManagementViewModel>(viewModel =>
            viewModel.LoadUsersCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task NavigateToHistory()
    {
        await NavigateToPageAsync<HistoryPage, HistoryViewModel>(viewModel =>
            viewModel.LoadEventsCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task NavigateToAuditLog()
    {
        if (!IsAdmin)
        {
            MessageBox.Show("Доступ только для администратора.", "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await NavigateToPageAsync<AuditLogPage, AuditLogViewModel>(viewModel =>
            viewModel.LoadLogsCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task NavigateToSettings()
    {
        await NavigateToPageAsync<SettingsPage, SettingsViewModel>(viewModel =>
            viewModel.LoadSettingsCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private void Logout()
    {
        try
        {
            CurrentUser.Clear();

            var loginWindow = App.Services.GetRequiredService<LoginWindow>();
            loginWindow.DataContext = App.Services.GetRequiredService<LoginViewModel>();
            loginWindow.Show();

            var mainWindow = Application.Current.Windows
                .OfType<MainWindow>()
                .FirstOrDefault();

            mainWindow?.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка выхода", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task NavigateToPageAsync<TPage, TViewModel>(
        Func<TViewModel, Task>? initializer = null)
        where TPage : Page
    {
        try
        {
            var page = App.Services.GetRequiredService<TPage>();
            var viewModel = App.Services.GetRequiredService<TViewModel>();
            page.DataContext = viewModel;
            CurrentPage = page;

            if (initializer is not null)
            {
                await initializer(viewModel);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Тип: {ex.GetType().Name}\n\nСообщение: {ex.Message}",
                "Ошибка навигации",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}