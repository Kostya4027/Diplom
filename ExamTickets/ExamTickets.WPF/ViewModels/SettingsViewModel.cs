using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamTickets.Core.Data;
using ExamTickets.Core.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;

namespace ExamTickets.WPF.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    private static string _currentTheme = "Light";

    [ObservableProperty]
    private string institutionName = string.Empty;

    [ObservableProperty]
    private string selectedTheme;

    public List<string> Themes { get; } = new() { "Light", "Dark" };

    public IAsyncRelayCommand LoadSettingsCommand { get; }
    public IAsyncRelayCommand SaveSettingsCommand { get; }

    public SettingsViewModel(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        
        var palette = new PaletteHelper();
        var theme = palette.GetTheme();
        _currentTheme = theme.GetBaseTheme() == BaseTheme.Dark ? "Dark" : "Light";
        
        selectedTheme = _currentTheme;

        LoadSettingsCommand = new AsyncRelayCommand(LoadSettingsAsync);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
    }

    public bool IsReadOnly =>
        ExamTickets.WPF.Helpers.CurrentUser.Instance?.Role == UserRole.Teacher;

    partial void OnSelectedThemeChanged(string value)
    {
        if (value == _currentTheme) return;
        _currentTheme = value;
        ToggleTheme();
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var s = await context.Settings.AsNoTracking().FirstOrDefaultAsync(x => x.SettingKey == "InstitutionName");
            InstitutionName = s?.SettingValue ?? string.Empty;

            // Применяем сохранённую тему при загрузке страницы
            ToggleTheme();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка загрузки настроек", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var s = await context.Settings.FirstOrDefaultAsync(x => x.SettingKey == "InstitutionName");
            if (s is null)
            {
                s = new Setting { SettingKey = "InstitutionName", SettingValue = InstitutionName };
                context.Settings.Add(s);
            }
            else
            {
                s.SettingValue = InstitutionName;
                context.Settings.Update(s);
            }

            await context.SaveChangesAsync();
            MessageBox.Show("Настройки сохранены.", "Сохранено", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ToggleTheme()
    {
        var palette = new PaletteHelper();
        var theme = palette.GetTheme();
        theme.SetBaseTheme(string.Equals(SelectedTheme, "Dark", StringComparison.OrdinalIgnoreCase) ? BaseTheme.Dark : BaseTheme.Light);
        palette.SetTheme(theme);
    }
}