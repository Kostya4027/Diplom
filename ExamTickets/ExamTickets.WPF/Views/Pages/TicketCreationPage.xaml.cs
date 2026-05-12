using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
// Полный путь для указания конкретного типа Group, чтобы избежать конфликта с System.Text.RegularExpressions
using CoreGroup = ExamTickets.Core.Models.Group;
using ExamTickets.Core.Models;
using ExamTickets.WPF.ViewModels;

namespace ExamTickets.WPF.Views.Pages;

public partial class TicketCreationPage : Page
{
    public TicketCreationPage()
    {
        InitializeComponent();
    }

    private void GroupsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not TicketCreationViewModel vm)
        {
            return;
        }
        
        vm.SelectedGroups.Clear();

        // Мы используем e.AddedItems и e.RemovedItems или обращаемся к sender как к ListBox,
        // вместо прямого вызова GroupsListBox (если он не найден в контексте).
        if (sender is ListBox listBox)
        {
            foreach (CoreGroup item in listBox.SelectedItems)
            {
                vm.SelectedGroups.Add(item);
            }
        }
    }

    private void TeachersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not TicketCreationViewModel vm)
        {
            return;
        }
        
        vm.SelectedTeachers.Clear();

        if (sender is ListBox listBox)
        {
            foreach (Teacher item in listBox.SelectedItems)
            {
                vm.SelectedTeachers.Add(item);
            }
        }
    }

    private void TicketCountBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Разрешаем только цифры
        e.Handled = !e.Text.All(char.IsDigit);
    }
}