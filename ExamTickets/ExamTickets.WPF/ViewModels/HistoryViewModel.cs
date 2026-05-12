using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamTickets.Core.Data;
using ExamTickets.Core.Models;
using ExamTickets.Core.Services;
using ExamTickets.WPF.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ExamTickets.WPF.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly DocumentService _documentService;
    private readonly ExamTickets.Core.Services.AuditService _auditService;

    public ObservableCollection<Specialty> Specialties { get; } = new();
    public ObservableCollection<ExamEvent> Events { get; } = new();

    [ObservableProperty] private Specialty? selectedSpecialty;
    [ObservableProperty] private string subjectSearch = string.Empty;
    [ObservableProperty] private DateTime? dateFrom;
    [ObservableProperty] private DateTime? dateTo;

    public IAsyncRelayCommand LoadEventsCommand { get; }
    public IAsyncRelayCommand<ExamEvent> OpenDocxCommand { get; }
    public IAsyncRelayCommand<ExamEvent> OpenPdfCommand { get; }
    public IAsyncRelayCommand<ExamEvent> DeleteEventCommand { get; }

    public HistoryViewModel(
        IDbContextFactory<AppDbContext> contextFactory,
        DocumentService documentService,
        ExamTickets.Core.Services.AuditService auditService)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

        LoadEventsCommand = new AsyncRelayCommand(LoadEventsAsync);
        OpenDocxCommand = new AsyncRelayCommand<ExamEvent>(OpenDocxAsync);
        OpenPdfCommand = new AsyncRelayCommand<ExamEvent>(OpenPdfAsync);
        DeleteEventCommand = new AsyncRelayCommand<ExamEvent>(DeleteEventAsync);
    }

    public async Task LoadEventsAsync()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var specs = await context.Specialties.AsNoTracking().OrderBy(s => s.SpecialtyName).ToListAsync();
            Specialties.Clear();
            foreach (var s in specs) Specialties.Add(s);

            IQueryable<ExamEvent> query = context.ExamEvents
                .Include(e => e.Subject)
                .Include(e => e.Specialty)
                .Include(e => e.Group)
                .Include(e => e.Teacher)
                .Include(e => e.CreatedByUser)
                .AsNoTracking();

            if (SelectedSpecialty is not null)
            {
                query = query.Where(e => e.SpecialtyID == SelectedSpecialty.SpecialtyID);
            }

            if (!string.IsNullOrWhiteSpace(SubjectSearch))
            {
                var s = SubjectSearch.Trim();
                query = query.Where(e => e.Subject != null && EF.Functions.Like(e.Subject.SubjectName, $"%{s}%"));
            }

            if (DateFrom is not null)
            {
                query = query.Where(e => e.ExamDate >= DateFrom.Value);
            }

            if (DateTo is not null)
            {
                query = query.Where(e => e.ExamDate <= DateTo.Value);
            }

            if (CurrentUser.Instance?.Role == UserRole.Teacher)
            {
                var uid = CurrentUser.Instance.UserID;
                query = query.Where(e => e.CreatedBy == uid);
            }

            var list = await query.OrderByDescending(e => e.ExamDate).ToListAsync();

            Events.Clear();
            foreach (var ev in list) Events.Add(ev);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка загрузки истории", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task OpenDocxAsync(ExamEvent? ev)
    {
        if (ev is null) return;

        if (ev.TicketID is null)
        {
            MessageBox.Show("DOCX не найден для выбранного события.", "Открыть DOCX", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var context = await _contextFactory.CreateDbContextAsync();
        var doc = await context.TicketDocuments.FindAsync(ev.TicketID);
        if (doc is null || string.IsNullOrWhiteSpace(doc.FilePathDocx) || !System.IO.File.Exists(doc.FilePathDocx))
        {
            MessageBox.Show("DOCX не найден.", "Открыть DOCX", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(doc.FilePathDocx) { UseShellExecute = true });
    }

    private async Task OpenPdfAsync(ExamEvent? ev)
    {
        if (ev is null) return;

        using var context = await _contextFactory.CreateDbContextAsync();
        var doc = await context.TicketDocuments.FindAsync(ev.TicketID);
        if (doc is null || string.IsNullOrWhiteSpace(doc.FilePathPdf) || !System.IO.File.Exists(doc.FilePathPdf))
        {
            MessageBox.Show("PDF не найден.", "Открыть PDF", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(doc.FilePathPdf) { UseShellExecute = true });
    }

    private async Task DeleteEventAsync(ExamEvent? ev)
    {
        if (ev is null) return;

        var res = MessageBox.Show("Удалить выбранное экзаменационное событие?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res != MessageBoxResult.Yes) return;

        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            // Загружаем сущность из текущего контекста, чтобы корректно удалить
            var toRemove = await context.ExamEvents.FindAsync(ev.EventID);
            if (toRemove is not null)
            {
                context.ExamEvents.Remove(toRemove);
                await context.SaveChangesAsync();
            }

            await _auditService.LogAsync(CurrentUser.Instance?.UserID, "DeleteExamEvent", $"EventID={ev.EventID}");
            await LoadEventsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка удаления события", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}