using System.ComponentModel;
using System.Windows;
using ExamTickets.WPF.Helpers;

namespace ExamTickets.WPF.Views;

public partial class MainWindow : Window
{
    public static bool IsLogoutInProgress { get; set; }

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (IsLogoutInProgress)
        {
            IsLogoutInProgress = false;
            return;
        }

        // Закрытие приложения при закрытии главного окна пользователем.
        // При Logout CurrentUser уже очищен и это условие не выполняется.
        if (CurrentUser.Instance is not null)
        {
            Application.Current.Shutdown();
        }
    }
}