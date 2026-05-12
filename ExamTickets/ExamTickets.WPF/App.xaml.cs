using System;
using System.Windows;
using ExamTickets.Core.Data;
using ExamTickets.Core.Services;
using ExamTickets.WPF.ViewModels;
using ExamTickets.WPF.Views;
using ExamTickets.WPF.Views.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamTickets.WPF;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        var services = new ServiceCollection();

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlServer("Data Source=.\\SQLEXPRESS;Initial Catalog=ExamTicketsDB;Integrated Security=True;TrustServerCertificate=True"),
            ServiceLifetime.Scoped);

        services.AddScoped<AppDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

        services.AddScoped<AuthService>();
        services.AddScoped<TicketGeneratorService>();
        services.AddScoped<DocumentService>();
        
        services.AddTransient<AuditService>();

        services.AddTransient<LoginWindow>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainWindow>();
        services.AddTransient<MainViewModel>();

        services.AddTransient<DashboardPage>();
        services.AddTransient<DashboardViewModel>();

        services.AddTransient<TicketCreationPage>();
        services.AddTransient<TicketCreationViewModel>();

        services.AddTransient<HistoryPage>();
        services.AddTransient<HistoryViewModel>();

        services.AddTransient<AuditLogPage>();
        services.AddTransient<AuditLogViewModel>();

        services.AddTransient<SettingsPage>();
        services.AddTransient<SettingsViewModel>();

        services.AddTransient<UserManagementPage>();
        services.AddTransient<UserManagementViewModel>();

        services.AddTransient<DatabaseManagementPage>();
        services.AddTransient<DatabaseManagementViewModel>();

        Services = services.BuildServiceProvider();

        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            try
            {
                // Применяем только pending миграции, не пересоздаём БД
                var pending = dbContext.Database.GetPendingMigrations();
                if (pending.Any())
                {
                    dbContext.Database.Migrate();
                }
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
                when (ex.Message.Contains("уже существует") ||
                      ex.Message.Contains("already exists"))
            {
                // БД уже есть и актуальна — продолжаем
            }
        }

        var loginWindow = Services.GetRequiredService<LoginWindow>();
        loginWindow.DataContext = Services.GetRequiredService<LoginViewModel>();
        loginWindow.Show();
    }
}
