using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamTickets.Core.Data;
using ExamTickets.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace ExamTickets.WPF.ViewModels;

public partial class AuditLogViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ObservableCollection<AuditLog> Logs { get; } = new();

    [ObservableProperty] private string userSearch = string.Empty;
    [ObservableProperty] private string actionFilter = string.Empty;
    [ObservableProperty] private DateTime? dateFrom;

    public IAsyncRelayCommand LoadLogsCommand { get; }
    public IAsyncRelayCommand ExportToCsvCommand { get; }

    public AuditLogViewModel(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        LoadLogsCommand = new AsyncRelayCommand(LoadLogsAsync);
        ExportToCsvCommand = new AsyncRelayCommand(ExportToCsvAsync);
    }

    public async Task LoadLogsAsync()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            IQueryable<AuditLog> query = context.AuditLogs.Include(x => x.User).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(UserSearch))
            {
                var s = UserSearch.Trim();
                query = query.Where(x => x.User != null && EF.Functions.Like(x.User.FullName, $"%{s}%"));
            }

            if (!string.IsNullOrWhiteSpace(ActionFilter))
            {
                var s = ActionFilter.Trim();
                query = query.Where(x => EF.Functions.Like(x.Action, $"%{s}%"));
            }

            if (DateFrom is not null)
            {
                query = query.Where(x => x.CreatedAt >= DateFrom.Value);
            }

            var list = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
            Logs.Clear();
            foreach (var l in list) Logs.Add(l);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "ќшибка загрузки журнала аудита", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportToCsvAsync()
    {
        var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "audit.csv" };
        if (dlg.ShowDialog() != true) return;

        try
        {
            await using var sw = new StreamWriter(dlg.FileName, false, Encoding.UTF8);
            sw.WriteLine("User;Action;Details;CreatedAt");
            foreach (var log in Logs)
            {
                var user = log.User?.FullName ?? log.UserID.ToString();
                await sw.WriteLineAsync($"{Escape(user)};{Escape(log.Action)};{Escape(log.Details)};{log.CreatedAt:O}");
            }
            MessageBox.Show("Ёкспорт завершЄн.", "Ёкспорт CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "ќшибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string Escape(string? s) => (s ?? string.Empty).Replace(";", "\\;");
}