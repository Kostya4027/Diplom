using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamTickets.Core.Data;
using ExamTickets.WPF.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ExamTickets.WPF.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    [ObservableProperty]
    private int questionsCount;

    [ObservableProperty]
    private int ticketsCount;

    [ObservableProperty]
    private int eventsCount;

    public string WelcomeMessage => $"Добро пожаловать, {CurrentUser.Instance?.FullName ?? "пользователь"}!";

    public IAsyncRelayCommand LoadDataCommand { get; }

    public DashboardViewModel(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
    }

    // Пустой конструктор для дизайнера
    public DashboardViewModel() { }

    private async Task LoadDataAsync()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            QuestionsCount = await context.QuestionDocuments.CountAsync();
            TicketsCount = await context.TicketDocuments.CountAsync();
            EventsCount = await context.ExamEvents.CountAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка загрузки дашборда", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}