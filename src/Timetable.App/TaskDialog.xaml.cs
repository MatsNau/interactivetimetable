using System.Windows;
using Timetable.App.ViewModels;

namespace Timetable.App;

public enum TaskDialogResult
{
    Save,
    Delete,
}

public partial class TaskDialog : Window
{
    private readonly TaskEditorViewModel _viewModel;

    /// <summary>Null bedeutet: abgebrochen.</summary>
    public TaskDialogResult? Result { get; private set; }

    public TaskDialog(TaskEditorViewModel viewModel, bool canDelete)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        DeleteButton.Visibility = canDelete ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Validate() is { } error)
        {
            MessageBox.Show(this, error, "Eingabe unvollständig", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = TaskDialogResult.Save;
        DialogResult = true;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(this,
            "Diese Aufgabe wirklich löschen?", "Aufgabe löschen",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
            return;

        Result = TaskDialogResult.Delete;
        DialogResult = true;
    }
}
