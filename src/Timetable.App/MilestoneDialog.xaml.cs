using System.Windows;
using Timetable.App.ViewModels;

namespace Timetable.App;

public enum MilestoneDialogResult
{
    Save,
    Delete,
}

public partial class MilestoneDialog : Window
{
    private readonly MilestoneEditorViewModel _viewModel;

    /// <summary>Null bedeutet: abgebrochen.</summary>
    public MilestoneDialogResult? Result { get; private set; }

    public MilestoneDialog(MilestoneEditorViewModel viewModel, bool canDelete)
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

        Result = MilestoneDialogResult.Save;
        DialogResult = true;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(this,
            "Diesen Meilenstein wirklich löschen?", "Meilenstein löschen",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
            return;

        Result = MilestoneDialogResult.Delete;
        DialogResult = true;
    }
}
