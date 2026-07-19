using System.Windows;
using Timetable.App.ViewModels;

namespace Timetable.App;

public enum ProjectDialogResult
{
    Save,
    Delete,
}

public partial class ProjectDialog : Window
{
    private readonly ProjectEditorViewModel _viewModel;
    private readonly int _milestoneCount;

    /// <summary>Null bedeutet: abgebrochen.</summary>
    public ProjectDialogResult? Result { get; private set; }

    public ProjectDialog(ProjectEditorViewModel viewModel, bool canDelete, int milestoneCount)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _milestoneCount = milestoneCount;
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

        Result = ProjectDialogResult.Save;
        DialogResult = true;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var question = _milestoneCount switch
        {
            0 => "Dieses Projekt wirklich löschen?",
            1 => "Dieses Projekt und seinen Meilenstein wirklich löschen?",
            _ => $"Dieses Projekt und seine {_milestoneCount} Meilensteine wirklich löschen?",
        };

        var answer = MessageBox.Show(this,
            question, "Projekt löschen",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
            return;

        Result = ProjectDialogResult.Delete;
        DialogResult = true;
    }
}
