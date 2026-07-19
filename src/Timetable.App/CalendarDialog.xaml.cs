using System.Windows;
using Timetable.App.ViewModels;

namespace Timetable.App;

public partial class CalendarDialog : Window
{
    private readonly CalendarEditorViewModel _viewModel;

    public CalendarDialog(CalendarEditorViewModel viewModel)
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
