using System.Windows;
using Timetable.App.ViewModels;

namespace Timetable.App;

public partial class PlanInfoDialog : Window
{
    private readonly PlanInfoEditorViewModel _viewModel;

    public PlanInfoDialog(PlanInfoEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Validate() is { } error)
        {
            MessageBox.Show(this, error, "Eingabe unvollständig", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
