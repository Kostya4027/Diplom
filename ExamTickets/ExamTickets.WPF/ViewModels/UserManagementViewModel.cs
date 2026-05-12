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
using Microsoft.EntityFrameworkCore;

namespace ExamTickets.WPF.ViewModels;

public partial class UserManagementViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly AuthService _authService;

    public ObservableCollection<User> Users { get; } = new();

    [ObservableProperty] private User? selectedUser;
    [ObservableProperty] private string selectedRoleFilter = "Все";

    [ObservableProperty] private string editLogin = string.Empty;
    [ObservableProperty] private string editFullName = string.Empty;
    [ObservableProperty] private UserRole editRole = UserRole.Teacher;
    [ObservableProperty] private string editPassword = string.Empty;
    [ObservableProperty] private bool editIsActive = true;
    [ObservableProperty] private bool isEditMode;

    public IAsyncRelayCommand LoadUsersCommand { get; }
    public IRelayCommand StartAddCommand { get; }
    public IRelayCommand<User?> StartEditCommand { get; }
    public IAsyncRelayCommand SaveUserCommand { get; }
    public IAsyncRelayCommand<User?> ToggleActiveCommand { get; }
    public IAsyncRelayCommand<User?> ResetPasswordCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public UserManagementViewModel(IDbContextFactory<AppDbContext> contextFactory, AuthService authService)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        LoadUsersCommand = new AsyncRelayCommand(LoadUsersAsync);
        StartAddCommand = new RelayCommand(StartAdd);
        StartEditCommand = new RelayCommand<User?>(StartEdit);
        SaveUserCommand = new AsyncRelayCommand(SaveUserAsync);
        ToggleActiveCommand = new AsyncRelayCommand<User?>(ToggleActiveAsync);
        ResetPasswordCommand = new AsyncRelayCommand<User?>(ResetPasswordAsync);
        CancelCommand = new RelayCommand(Cancel);
    }

    partial void OnSelectedRoleFilterChanged(string value)
    {
        _ = LoadUsersAsync();
    }

    public async Task LoadUsersAsync()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            IQueryable<User> query = context.Users.AsNoTracking();

            query = SelectedRoleFilter switch
            {
                "Администраторы" => query.Where(u => u.Role == UserRole.Admin),
                "Председатели" => query.Where(u => u.Role == UserRole.Chairman),
                "Учителя" => query.Where(u => u.Role == UserRole.Teacher),
                _ => query
            };

            var list = await query.OrderBy(u => u.Login).ToListAsync();
            Users.Clear();
            foreach (var u in list)
            {
                Users.Add(u);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка загрузки пользователей", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void StartAdd()
    {
        SelectedUser = null;
        EditLogin = string.Empty;
        EditFullName = string.Empty;
        EditRole = UserRole.Teacher;
        EditPassword = string.Empty;
        EditIsActive = true;
        IsEditMode = true;
    }

    public void StartEdit(User? user)
    {
        if (user is null)
        {
            return;
        }

        SelectedUser = user;
        EditLogin = user.Login;
        EditFullName = user.FullName;
        EditRole = user.Role;
        EditPassword = string.Empty;
        EditIsActive = user.IsActive;
        IsEditMode = true;
    }

    public async Task SaveUserAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EditLogin) || string.IsNullOrWhiteSpace(EditFullName))
            {
                throw new InvalidOperationException("Логин и ФИО обязательны.");
            }

            if (SelectedUser is null)
            {
                if (string.IsNullOrWhiteSpace(EditPassword))
                {
                    throw new InvalidOperationException("Введите пароль для нового пользователя.");
                }

                var created = await _authService.CreateUserAsync(EditLogin.Trim(), EditPassword, EditFullName.Trim(), EditRole);
                if (!created)
                {
                    throw new InvalidOperationException("Пользователь с таким логином уже существует.");
                }
            }
            else
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var user = await context.Users.FirstOrDefaultAsync(x => x.UserID == SelectedUser.UserID);
                if (user is null)
                {
                    throw new InvalidOperationException("Пользователь не найден.");
                }

                var duplicate = await context.Users.AnyAsync(x => x.Login == EditLogin.Trim() && x.UserID != user.UserID);
                if (duplicate)
                {
                    throw new InvalidOperationException("Пользователь с таким логином уже существует.");
                }

                user.Login = EditLogin.Trim();
                user.FullName = EditFullName.Trim();
                user.Role = EditRole;
                user.IsActive = EditIsActive;

                context.Users.Update(user);
                await context.SaveChangesAsync();
            }

            IsEditMode = false;
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка сохранения пользователя", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ToggleActiveAsync(User? user)
    {
        if (user is null)
        {
            return;
        }

        try
        {
            var ok = await _authService.ToggleUserActiveAsync(user.UserID);
            if (!ok)
            {
                throw new InvalidOperationException("Не удалось изменить статус пользователя.");
            }

            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ResetPasswordAsync(User? user)
    {
        if (user is null)
        {
            return;
        }

        var newPassword = Microsoft.VisualBasic.Interaction.InputBox($"Введите новый пароль для '{user.Login}':", "Сброс пароля");
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return;
        }

        var ok = await _authService.ChangePasswordAsync(user.UserID, newPassword);
        if (!ok)
        {
            MessageBox.Show("Не удалось сменить пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void Cancel()
    {
        IsEditMode = false;
    }
}