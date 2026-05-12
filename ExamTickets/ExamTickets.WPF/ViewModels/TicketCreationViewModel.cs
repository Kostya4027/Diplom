using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamTickets.Core.Data;
using ExamTickets.Core.DTOs;
using ExamTickets.Core.Models;
using ExamTickets.Core.Services;
using ExamTickets.WPF.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace ExamTickets.WPF.ViewModels;

public partial class TicketCreationViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly TicketGeneratorService _ticketGeneratorService;
    private readonly DocumentService _documentService;
    private readonly AuditService _auditService;

    public ObservableCollection<Specialty> Specialties { get; } = new();
    public ObservableCollection<Subject> Subjects { get; } = new();
    public ObservableCollection<Group> AllGroups { get; } = new();
    public ObservableCollection<Teacher> Teachers { get; } = new();

    public ObservableCollection<Group> SelectedGroups { get; } = new();
    public ObservableCollection<Teacher> SelectedTeachers { get; } = new();

    public List<string> ExamTypes { get; } = new() { "Дисциплина", "Квалификационный экзамен по ПМ" };
    public List<string> SaveFormats { get; } = new() { "docx", "pdf" };
    public List<string> QuestionModes { get; } = new()
    {
        "1 вопрос", "2 вопроса", "3 вопроса", "4 вопроса", "2 вопроса + задача", "задача"
    };

    [ObservableProperty] private Specialty? selectedSpecialty;
    [ObservableProperty] private Subject? selectedSubject;
    
    [ObservableProperty] private string commission = string.Empty;
    [ObservableProperty] private string protocolNumber = string.Empty;
    [ObservableProperty] private DateTime date = DateTime.Today;
    [ObservableProperty] private string chairman = string.Empty;
    [ObservableProperty] private string specialtyNumber = string.Empty;
    [ObservableProperty] private string examType = "Дисциплина";
    [ObservableProperty] private string exam = string.Empty;
    [ObservableProperty] private string groupsNumber = string.Empty;
    [ObservableProperty] private string semester = string.Empty;
    [ObservableProperty] private string affirmer = string.Empty;
    [ObservableProperty] private string affirmerLastName = string.Empty;
    [ObservableProperty] private DateTime dateOfStatement = DateTime.Today;
    [ObservableProperty] private int ticketCount = 1;
    [ObservableProperty] private string selectedQuestionMode = "3 вопроса";
    [ObservableProperty] private string questionFilePath = string.Empty;
    [ObservableProperty] private int maxTickets;
    [ObservableProperty] private byte[]? generatedDocument;
    [ObservableProperty] private bool isGenerated;
    [ObservableProperty] private string saveFormat = "docx";

    [ObservableProperty] private bool isFileValid;
    [ObservableProperty] private string fileValidationMessage = string.Empty;

    [ObservableProperty] private string previewInstitution = string.Empty;
    [ObservableProperty] private string previewCommission = string.Empty;
    [ObservableProperty] private string previewProtocol = string.Empty;
    [ObservableProperty] private string previewDate = string.Empty;
    [ObservableProperty] private string previewChairman = string.Empty;
    [ObservableProperty] private string previewSpecialtyNumber = string.Empty;
    [ObservableProperty] private string previewExamType = string.Empty;
    [ObservableProperty] private string previewExam = string.Empty;
    [ObservableProperty] private string previewGroups = string.Empty;
    [ObservableProperty] private string previewSemester = string.Empty;
    [ObservableProperty] private string previewAffirmer = string.Empty;
    [ObservableProperty] private string previewAffirmerLastName = string.Empty;
    [ObservableProperty] private string previewDateOfStatement = string.Empty;
    [ObservableProperty] private string previewTeachers = string.Empty;
    [ObservableProperty] private string previewQuestion1 = "Вопрос 1";
    [ObservableProperty] private string previewQuestion2 = "Вопрос 2";
    [ObservableProperty] private string previewQuestion3 = "Вопрос 3";
    [ObservableProperty] private string previewQuestion4 = "Вопрос 4";

    private int QuestionsPerTicket => SelectedQuestionMode switch
    {
        "1 вопрос" => 1,
        "2 вопроса" => 2,
        "3 вопроса" => 3,
        "4 вопроса" => 4,
        "2 вопроса + задача" => 3,
        "задача" => 1,
        _ => 3
    };

    public bool CanGenerate =>
        SelectedSpecialty != null &&
        SelectedSubject != null &&
        SelectedGroups.Count > 0 &&
        SelectedTeachers.Count > 0 &&
        !string.IsNullOrWhiteSpace(QuestionFilePath) &&
        !string.IsNullOrWhiteSpace(ProtocolNumber) &&
        !string.IsNullOrWhiteSpace(ExamType) &&
        !string.IsNullOrWhiteSpace(Chairman);

    public IAsyncRelayCommand LoadDataCommand { get; }
    public IRelayCommand BrowseFileCommand { get; }
    public IAsyncRelayCommand GenerateCommand { get; }
    public IAsyncRelayCommand PreviewCommand { get; }
    public IAsyncRelayCommand PrintCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }

    public TicketCreationViewModel(IDbContextFactory<AppDbContext> contextFactory, TicketGeneratorService ticketGeneratorService, DocumentService documentService, AuditService auditService)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _ticketGeneratorService = ticketGeneratorService ?? throw new ArgumentNullException(nameof(ticketGeneratorService));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        BrowseFileCommand = new RelayCommand(BrowseFile);
        GenerateCommand = new AsyncRelayCommand(GenerateAsync);
        PreviewCommand = new AsyncRelayCommand(PreviewAsync);
        PrintCommand = new AsyncRelayCommand(PrintAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);

        SelectedGroups.CollectionChanged += (_, _) => { UpdateGroupsNumber(); TriggerCanGenerate(); UpdatePreview(); };
        SelectedTeachers.CollectionChanged += (_, _) => { TriggerCanGenerate(); UpdatePreview(); };
    }

    public TicketCreationViewModel() { }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(SelectedSpecialty) or nameof(SelectedSubject) or
            nameof(QuestionFilePath) or nameof(ProtocolNumber) or nameof(ExamType) or nameof(Chairman))
        {
            TriggerCanGenerate();
        }
    }

    private void TriggerCanGenerate() => OnPropertyChanged(nameof(CanGenerate));

    partial void OnCommissionChanged(string value) => UpdatePreview();
    partial void OnProtocolNumberChanged(string value) => UpdatePreview();
    partial void OnDateChanged(DateTime value) => UpdatePreview();
    partial void OnChairmanChanged(string value) => UpdatePreview();

    partial void OnSelectedSpecialtyChanged(Specialty? value)
    {
        _ = LoadSubjectsAndGroupsAsync(value);
        UpdatePreview();
    }

    partial void OnSelectedSubjectChanged(Subject? value)
    {
        if (value is not null)
        {
            Semester = value.Semester.ToString();
            Exam = value.SubjectName;
            _ = LoadTeachersForSubjectAsync(value.SubjectID);
        }
        else
        {
            Semester = string.Empty;
            Exam = string.Empty;
            Teachers.Clear();
            SelectedTeachers.Clear();
        }

        UpdatePreview();
    }

    partial void OnExamTypeChanged(string value) => UpdatePreview();
    partial void OnAffirmerChanged(string value) => UpdatePreview();
    partial void OnAffirmerLastNameChanged(string value) => UpdatePreview();
    partial void OnDateOfStatementChanged(DateTime value) => UpdatePreview();

    partial void OnSelectedQuestionModeChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(QuestionFilePath))
        {
            CalculateMaxTickets();
        }
        
        UpdatePreview();
    }

    private void UpdateGroupsNumber()
    {
        GroupsNumber = string.Join(", ", SelectedGroups.Select(g => g.GroupNumber));
    }

    private void UpdatePreview()
    {
        PreviewCommission = string.IsNullOrWhiteSpace(Commission) 
            ? string.Empty : Commission;

        PreviewProtocol = string.IsNullOrWhiteSpace(ProtocolNumber) 
            ? string.Empty : ProtocolNumber;

        PreviewDate = Date == DateTime.Today 
            ? string.Empty : Date.ToString("dd.MM.yyyy");

        PreviewChairman = string.IsNullOrWhiteSpace(Chairman) 
            ? string.Empty : Chairman;

        PreviewSpecialtyNumber = SelectedSpecialty?.SpecialtyNumber ?? string.Empty;

        PreviewExamType = string.IsNullOrWhiteSpace(ExamType) 
            ? string.Empty : ExamType;

        PreviewExam = string.IsNullOrWhiteSpace(Exam) 
            ? string.Empty : Exam;

        PreviewGroups = SelectedGroups.Count > 0
            ? string.Join(", ", SelectedGroups.Select(g => g.GroupNumber))
            : string.Empty;

        PreviewSemester = string.IsNullOrWhiteSpace(Semester) 
            ? string.Empty : Semester;

        PreviewAffirmer = string.IsNullOrWhiteSpace(Affirmer) 
            ? string.Empty : Affirmer;

        PreviewAffirmerLastName = string.IsNullOrWhiteSpace(AffirmerLastName) 
            ? string.Empty : AffirmerLastName;

        PreviewDateOfStatement = DateOfStatement == DateTime.Today 
            ? string.Empty : DateOfStatement.ToString("dd.MM.yyyy");

        PreviewTeachers = SelectedTeachers.Count > 0
            ? string.Join(", ", SelectedTeachers.Select(t => t.FullName))
            : string.Empty;

        var mode = SelectedQuestionMode ?? "3 вопроса";
        switch (mode)
        {
            case "1 вопрос":
                PreviewQuestion1 = "Вопрос 1";
                PreviewQuestion2 = string.Empty;
                PreviewQuestion3 = string.Empty;
                PreviewQuestion4 = string.Empty;
                break;
            case "2 вопроса":
                PreviewQuestion1 = "Вопрос 1";
                PreviewQuestion2 = "Вопрос 2";
                PreviewQuestion3 = string.Empty;
                PreviewQuestion4 = string.Empty;
                break;
            case "3 вопроса":
                PreviewQuestion1 = "Вопрос 1";
                PreviewQuestion2 = "Вопрос 2";
                PreviewQuestion3 = "Вопрос 3";
                PreviewQuestion4 = string.Empty;
                break;
            case "4 вопроса":
                PreviewQuestion1 = "Вопрос 1";
                PreviewQuestion2 = "Вопрос 2";
                PreviewQuestion3 = "Вопрос 3";
                PreviewQuestion4 = "Вопрос 4";
                break;
            case "2 вопроса + задача":
                PreviewQuestion1 = "Вопрос 1";
                PreviewQuestion2 = "Вопрос 2";
                PreviewQuestion3 = "Задача";
                PreviewQuestion4 = string.Empty;
                break;
            case "задача":
                PreviewQuestion1 = "Задача";
                PreviewQuestion2 = string.Empty;
                PreviewQuestion3 = string.Empty;
                PreviewQuestion4 = string.Empty;
                break;
            default:
                PreviewQuestion1 = "Вопрос 1";
                PreviewQuestion2 = "Вопрос 2";
                PreviewQuestion3 = "Вопрос 3";
                PreviewQuestion4 = string.Empty;
                break;
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var specs = await context.Specialties.AsNoTracking().OrderBy(s => s.SpecialtyName).ToListAsync();

            Specialties.Clear();
            foreach (var s in specs) Specialties.Add(s);

            var setting = await context.Settings
                .FirstOrDefaultAsync(s => s.SettingKey == "InstitutionName");
            PreviewInstitution = setting?.SettingValue ?? string.Empty;
            UpdatePreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка загрузки данных", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadSubjectsAndGroupsAsync(Specialty? specialty)
    {
        try
        {
            Subjects.Clear();
            AllGroups.Clear();
            SelectedGroups.Clear();
            Teachers.Clear();
            SelectedTeachers.Clear();

            if (specialty is null) return;

            using var context = await _contextFactory.CreateDbContextAsync();
            var subs = await context.Subjects.AsNoTracking().Where(x => x.SpecialtyID == specialty.SpecialtyID).OrderBy(x => x.SubjectName).ToListAsync();
            var grps = await context.Groups.AsNoTracking().Where(x => x.SpecialtyID == specialty.SpecialtyID).OrderBy(x => x.GroupNumber).ToListAsync();

            foreach (var s in subs) Subjects.Add(s);
            foreach (var g in grps) AllGroups.Add(g);

            SpecialtyNumber = specialty.SpecialtyNumber ?? string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка загрузки предметов/групп", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadTeachersForSubjectAsync(int subjectId)
    {
        try
        {
            Teachers.Clear();
            SelectedTeachers.Clear();
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var teachers = await context.TeacherSubjects
                .AsNoTracking()
                .Where(ts => ts.SubjectID == subjectId)
                .Include(ts => ts.Teacher)
                .Select(ts => ts.Teacher)
                .OrderBy(t => t.FullName)
                .ToListAsync();
                
            foreach (var t in teachers)
            {
                if (t != null) Teachers.Add(t);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка загрузки преподавателей", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BrowseFile()
    {
        var dlg = new OpenFileDialog { Filter = "Документы (*.docx;*.txt)|*.docx;*.txt|Все файлы (*.*)|*.*" };
        if (dlg.ShowDialog() != true) return;

        QuestionFilePath = dlg.FileName;
        ValidateQuestionFile();
        CalculateMaxTickets();
    }

    private async Task GenerateAsync()
    {
        if (!CanGenerate) return;

        try
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();
            var setting = await ctx.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.SettingKey == "InstitutionName");

            var formData = new TicketFormData
            {
                EducationalInstitution = setting?.SettingValue ?? string.Empty,
                Commission = Commission,
                ProtocolNumber = ProtocolNumber,
                Date = Date,
                Chairman = Chairman,
                SpecialtyNumber = SpecialtyNumber,
                ExamType = ExamType,
                Exam = Exam,
                GroupsNumber = GroupsNumber,
                Semester = int.TryParse(Semester, out var sem) ? sem : 0,
                Affirmer = Affirmer,
                AffirmerLastName = AffirmerLastName,
                DateOfStatement = DateOfStatement,
                Teachers = string.Join(", ", SelectedTeachers.Select(t => t.FullName)),
                TicketCount = TicketCount,
                QuestionsPerTicket = QuestionsPerTicket,
                QuestionFilePath = QuestionFilePath
            };

            GeneratedDocument = await _ticketGeneratorService.GenerateTicketsDocumentAsync(formData);
            IsGenerated = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка генерации", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task PreviewAsync()
    {
        if (GeneratedDocument is null || GeneratedDocument.Length == 0) return;
        var temp = Path.Combine(Path.GetTempPath(), $"ticket_preview_{Guid.NewGuid():N}.docx");
        await File.WriteAllBytesAsync(temp, GeneratedDocument);
        Process.Start(new ProcessStartInfo(temp) { UseShellExecute = true });
    }

    private async Task PrintAsync()
    {
        if (GeneratedDocument is null || GeneratedDocument.Length == 0) return;
        await _documentService.PrintDocumentAsync(GeneratedDocument);
    }

    private async Task SaveAsync()
    {
        if (GeneratedDocument is null || GeneratedDocument.Length == 0) return;

        if (string.Equals(SaveFormat, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            var dlg = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", DefaultExt = ".pdf", AddExtension = true };
            if (dlg.ShowDialog() == true)
            {
                var pdfPath = dlg.FileName;
                var docxPath = Path.ChangeExtension(pdfPath, ".docx");
                await _documentService.SaveAsDocxAsync(GeneratedDocument, docxPath);
                await _documentService.SaveAsPdfAsync(GeneratedDocument, pdfPath);
                await SaveEntitiesAsync(docxPath, pdfPath);
            }
        }
        else
        {
            var dlg = new SaveFileDialog { Filter = "Word Document (*.docx)|*.docx", DefaultExt = ".docx", AddExtension = true };
            if (dlg.ShowDialog() == true)
            {
                await _documentService.SaveAsDocxAsync(GeneratedDocument, dlg.FileName);
                await SaveEntitiesAsync(dlg.FileName, null);
            }
        }
    }

    private void ValidateQuestionFile()
    {
        try
        {
            var markers = Enumerable.Range(1, QuestionsPerTicket).Select(i => $"Вопрос {i}").ToList();
            _ticketGeneratorService.ValidateQuestionFile(QuestionFilePath, markers);
            
            IsFileValid = true;
            FileValidationMessage = "Файл корректен.";
        }
        catch (Exception ex)
        {
            IsFileValid = false;
            FileValidationMessage = ex.Message;
            MessageBox.Show(ex.Message, "Проверка файла вопросов", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CalculateMaxTickets()
    {
        try
        {
            var markers = Enumerable.Range(1, QuestionsPerTicket).Select(i => $"Вопрос {i}").ToList();
            var questions = _ticketGeneratorService.ExtractQuestions(QuestionFilePath, markers);
            MaxTickets = _ticketGeneratorService.CalculateMaxTickets(questions, markers);
        }
        catch { MaxTickets = 0; }
    }

    private async Task SaveEntitiesAsync(string docxPath, string? pdfPath)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var userId = CurrentUser.Instance?.UserID ?? throw new InvalidOperationException("Пользователь не определён.");
            var now = DateTime.UtcNow;

            var qdoc = new QuestionDocument
            {
                FilePath = QuestionFilePath,
                SubjectID = SelectedSubject!.SubjectID,
                UploadedBy = userId,
                UploadedAt = now
            };
            context.QuestionDocuments.Add(qdoc);
            await context.SaveChangesAsync();

            var tdoc = new TicketDocument
            {
                FilePathDocx = docxPath,
                FilePathPdf = pdfPath,
                GeneratedBy = userId,
                GeneratedAt = now
            };
            context.TicketDocuments.Add(tdoc);
            await context.SaveChangesAsync();

            foreach (var group in SelectedGroups)
            {
                foreach (var teacher in SelectedTeachers)
                {
                    var ev = new ExamEvent
                    {
                        SubjectID = SelectedSubject.SubjectID,
                        SpecialtyID = SelectedSpecialty!.SpecialtyID,
                        GroupID = group.GroupID,
                        TeacherID = teacher.TeacherID,
                        QuestionID = qdoc.QuestionID,
                        TicketID = tdoc.TicketID,
                        ExamDate = Date,
                        ProtocolNumber = ProtocolNumber,
                        CommissionName = Commission,
                        Chairman = Chairman,
                        Affirmer = Affirmer,
                        AffirmerLastName = AffirmerLastName,
                        DateOfStatement = DateOfStatement,
                        Semester = int.TryParse(Semester, out var sem) ? sem : 0,
                        ExamType = ExamType,
                        TicketCount = TicketCount,
                        QuestionsPerTicket = QuestionsPerTicket,
                        CreatedBy = userId,
                        CreatedAt = now
                    };
                    context.ExamEvents.Add(ev);
                }
            }

            await context.SaveChangesAsync();
            
            await _auditService.LogAsync(
                userId,
                "CreateTickets",
                $"Предмет: {SelectedSubject?.SubjectName}, " +
                $"Специальность: {SelectedSpecialty?.SpecialtyName}, " +
                $"Билетов: {TicketCount}");

            MessageBox.Show("Документ и записи сохранены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка сохранения данных в БД", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}