using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamTickets.Core.Data;
using ExamTickets.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamTickets.WPF.ViewModels;

public partial class DatabaseManagementViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public ObservableCollection<string> Tables { get; } = new()
    {
        "Специальности",
        "Предметы",
        "Группы",
        "Преподаватели",
        "Привязки преподавателей"
    };

    public ObservableCollection<Specialty> Specialties { get; } = new();
    public ObservableCollection<Subject> Subjects { get; } = new();
    public ObservableCollection<Group> Groups { get; } = new();
    public ObservableCollection<Teacher> Teachers { get; } = new();
    public ObservableCollection<TeacherSubject> TeacherSubjects { get; } = new();

    public ObservableCollection<object> CurrentItems { get; } = new();

    [ObservableProperty] private string selectedTable = "Специальности";
    [ObservableProperty] private object? selectedItem;
    [ObservableProperty] private bool hasChanges;

    public IAsyncRelayCommand LoadTableDataCommand { get; }
    public IRelayCommand AddRowCommand { get; }
    public IAsyncRelayCommand<object?> DeleteRowCommand { get; }
    public IAsyncRelayCommand SaveChangesCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public DatabaseManagementViewModel(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        LoadTableDataCommand = new AsyncRelayCommand(LoadTableData);
        AddRowCommand = new RelayCommand(AddRow);
        DeleteRowCommand = new AsyncRelayCommand<object?>(DeleteRow);
        SaveChangesCommand = new AsyncRelayCommand(SaveChangesAsync);
        RefreshCommand = new AsyncRelayCommand(LoadTableData);
    }

    partial void OnSelectedTableChanged(string value)
    {
        _ = LoadTableData();
    }

    public async Task LoadTableData()
    {
        if (!await _loadLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var listS = await context.Specialties
                .AsNoTracking()
                .OrderBy(x => x.SpecialtyName)
                .ToListAsync();

            Specialties.Clear();
            foreach (var s in listS)
            {
                Specialties.Add(s);
            }

            var listSubFull = await context.Subjects.AsNoTracking().OrderBy(x => x.SubjectName).ToListAsync();
            Subjects.Clear();
            foreach (var s in listSubFull)
            {
                Subjects.Add(s);
            }

            var listTFull = await context.Teachers.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
            Teachers.Clear();
            foreach (var t in listTFull)
            {
                Teachers.Add(t);
            }

            CurrentItems.Clear();
            Groups.Clear();
            TeacherSubjects.Clear();

            switch (SelectedTable)
            {
                case "Специальности":
                    foreach (var item in listS)
                    {
                        CurrentItems.Add(item);
                    }

                    break;

                case "Предметы":
                {
                    var listSub = await context.Subjects
                        .Include(x => x.Specialty)
                        .AsNoTracking()
                        .OrderBy(x => x.SubjectName)
                        .ToListAsync();

                    Subjects.Clear();
                    foreach (var item in listSub)
                    {
                        Subjects.Add(item);
                        CurrentItems.Add(item);
                    }

                    break;
                }

                case "Группы":
                {
                    var listG = await context.Groups
                        .Include(x => x.Specialty)
                        .AsNoTracking()
                        .OrderBy(x => x.GroupNumber)
                        .ToListAsync();

                    foreach (var item in listG)
                    {
                        Groups.Add(item);
                        CurrentItems.Add(item);
                    }

                    break;
                }

                case "Преподаватели":
                {
                    foreach (var item in listTFull)
                    {
                        CurrentItems.Add(item);
                    }

                    break;
                }

                case "Привязки преподавателей":
                {
                    var listTS = await context.TeacherSubjects
                        .Include(ts => ts.Teacher)
                        .Include(ts => ts.Subject)
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var item in listTS)
                    {
                        TeacherSubjects.Add(item);
                        CurrentItems.Add(item);
                    }

                    break;
                }

                default:
                    throw new InvalidOperationException($"Неизвестная таблица: {SelectedTable}");
            }

            HasChanges = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private void AddRow()
    {
        switch (SelectedTable)
        {
            case "Специальности":
            {
                var item = new Specialty { SpecialtyName = string.Empty, SpecialtyNumber = string.Empty };
                Specialties.Add(item);
                CurrentItems.Add(item);
                SelectedItem = item;
                break;
            }
            case "Предметы":
            {
                var item = new Subject { SubjectName = string.Empty, Semester = 1, SpecialtyID = 0 };
                Subjects.Add(item);
                CurrentItems.Add(item);
                SelectedItem = item;
                break;
            }
            case "Группы":
            {
                var item = new Group { GroupNumber = string.Empty, SpecialtyID = 0 };
                Groups.Add(item);
                CurrentItems.Add(item);
                SelectedItem = item;
                break;
            }
            case "Преподаватели":
            {
                var item = new Teacher { FullName = string.Empty, UserID = null };
                Teachers.Add(item);
                CurrentItems.Add(item);
                SelectedItem = item;
                break;
            }
            case "Привязки преподавателей":
            {
                var item = new TeacherSubject { TeacherID = 0, SubjectID = 0 };
                TeacherSubjects.Add(item);
                CurrentItems.Add(item);
                SelectedItem = item;
                break;
            }
        }

        HasChanges = true;
    }

    private async Task DeleteRow(object? item)
    {
        if (item is null) return;

        var res = MessageBox.Show("Удалить выбранную запись?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res != MessageBoxResult.Yes) return;

        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            switch (item)
            {
                case Specialty s:
                {
                    var hasSubjects = await context.Subjects.AnyAsync(x => x.SpecialtyID == s.SpecialtyID);
                    var hasGroups = await context.Groups.AnyAsync(x => x.SpecialtyID == s.SpecialtyID);
                    var hasEvents = await context.ExamEvents.AnyAsync(x => x.SpecialtyID == s.SpecialtyID);
                    if (hasSubjects || hasGroups || hasEvents)
                    {
                        MessageBox.Show("Нельзя удалить специальность: имеются связанные записи (предметы, группы или события).", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    Specialties.Remove(s);
                    CurrentItems.Remove(s);
                    if (s.SpecialtyID > 0)
                    {
                        var dbItem = new Specialty { SpecialtyID = s.SpecialtyID };
                        context.Specialties.Attach(dbItem);
                        context.Specialties.Remove(dbItem);
                        await context.SaveChangesAsync();
                    }
                    break;
                }

                case Subject sub:
                {
                    var hasTeacherSubjects = await context.TeacherSubjects.AnyAsync(ts => ts.SubjectID == sub.SubjectID);
                    var hasQuestionDocs = await context.QuestionDocuments.AnyAsync(q => q.SubjectID == sub.SubjectID);
                    var hasEvents = await context.ExamEvents.AnyAsync(e => e.SubjectID == sub.SubjectID);
                    if (hasTeacherSubjects || hasQuestionDocs || hasEvents)
                    {
                        MessageBox.Show("Нельзя удалить предмет: имеются связанные записи (преподаватели/вопросы/события).", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    Subjects.Remove(sub);
                    CurrentItems.Remove(sub);
                    if (sub.SubjectID > 0)
                    {
                        var dbItem = new Subject { SubjectID = sub.SubjectID };
                        context.Subjects.Attach(dbItem);
                        context.Subjects.Remove(dbItem);
                        await context.SaveChangesAsync();
                    }
                    break;
                }

                case Group g:
                {
                    var hasEvents = await context.ExamEvents.AnyAsync(e => e.GroupID == g.GroupID);
                    if (hasEvents)
                    {
                        MessageBox.Show("Нельзя удалить группу: имеются связанные экзаменационные события.", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    Groups.Remove(g);
                    CurrentItems.Remove(g);
                    if (g.GroupID > 0)
                    {
                        var dbItem = new Group { GroupID = g.GroupID };
                        context.Groups.Attach(dbItem);
                        context.Groups.Remove(dbItem);
                        await context.SaveChangesAsync();
                    }
                    break;
                }

                case Teacher t:
                {
                    var hasTeacherSubjects = await context.TeacherSubjects.AnyAsync(ts => ts.TeacherID == t.TeacherID);
                    var hasEvents = await context.ExamEvents.AnyAsync(e => e.TeacherID == t.TeacherID);
                    if (hasTeacherSubjects || hasEvents)
                    {
                        MessageBox.Show("Нельзя удалить преподавателя: имеются связанные записи (предметы/события).", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    Teachers.Remove(t);
                    CurrentItems.Remove(t);
                    if (t.TeacherID > 0)
                    {
                        var dbItem = new Teacher { TeacherID = t.TeacherID };
                        context.Teachers.Attach(dbItem);
                        context.Teachers.Remove(dbItem);
                        await context.SaveChangesAsync();
                    }
                    break;
                }

                case TeacherSubject ts:
                {
                    TeacherSubjects.Remove(ts);
                    CurrentItems.Remove(ts);
                    if (ts.TeacherID > 0 && ts.SubjectID > 0)
                    {
                        var dbItem = new TeacherSubject { TeacherID = ts.TeacherID, SubjectID = ts.SubjectID };
                        context.TeacherSubjects.Attach(dbItem);
                        context.TeacherSubjects.Remove(dbItem);
                        await context.SaveChangesAsync();
                    }
                    break;
                }
            }

            HasChanges = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task SaveChangesAsync()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            switch (SelectedTable)
            {
                case "Специальности":
                {
                    foreach (var item in Specialties.ToList())
                    {
                        if (string.IsNullOrWhiteSpace(item.SpecialtyName))
                        {
                            MessageBox.Show("Название специальности не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (item.SpecialtyID == 0) context.Specialties.Add(item);
                        else context.Specialties.Update(item);
                    }
                    break;
                }

                case "Предметы":
                {
                    foreach (var item in Subjects.ToList())
                    {
                        if (string.IsNullOrWhiteSpace(item.SubjectName))
                        {
                            MessageBox.Show("Название предмета не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (item.SpecialtyID == 0)
                        {
                            MessageBox.Show($"Выберите специальность для предмета: {item.SubjectName}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (item.SubjectID == 0) context.Subjects.Add(item);
                        else context.Subjects.Update(item);
                    }
                    break;
                }

                case "Группы":
                {
                    foreach (var item in Groups.ToList())
                    {
                        if (string.IsNullOrWhiteSpace(item.GroupNumber))
                        {
                            MessageBox.Show("Номер группы не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (item.SpecialtyID == 0)
                        {
                            MessageBox.Show($"Выберите специальность для группы: {item.GroupNumber}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (item.GroupID == 0) context.Groups.Add(item);
                        else context.Groups.Update(item);
                    }
                    break;
                }

                case "Преподаватели":
                {
                    foreach (var item in Teachers.ToList())
                    {
                        if (string.IsNullOrWhiteSpace(item.FullName))
                        {
                            MessageBox.Show("ФИО преподавателя не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (item.TeacherID == 0) context.Teachers.Add(item);
                        else context.Teachers.Update(item);
                    }
                    break;
                }

                case "Привязки преподавателей":
                {
                    foreach (var item in TeacherSubjects.ToList())
                    {
                        if (item.TeacherID == 0 || item.SubjectID == 0)
                        {
                            MessageBox.Show("Выберите преподавателя и предмет.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var exists = await context.TeacherSubjects
                            .AnyAsync(ts => ts.TeacherID == item.TeacherID && ts.SubjectID == item.SubjectID);
                        
                        if (!exists)
                        {
                            context.TeacherSubjects.Add(item);
                        }
                    }
                    break;
                }
            }

            await context.SaveChangesAsync();
            await LoadTableData();
            HasChanges = false;
            MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

}