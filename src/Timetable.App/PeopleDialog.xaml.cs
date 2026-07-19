using System.Windows;
using Timetable.App.ViewModels;

namespace Timetable.App;

public partial class PeopleDialog : Window
{
    private readonly PeopleEditorViewModel _viewModel;

    public PeopleDialog(PeopleEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedPerson is not { } person)
            return;

        var label = string.IsNullOrWhiteSpace(person.ShortCode) ? "(ohne Kürzel)" : person.ShortCode.Trim();
        var references = _viewModel.CountReferences(person.Id);
        var question = references == 0
            ? $"\"{label}\" wirklich entfernen?"
            : $"\"{label}\" ist im Plan noch {references}-mal eingetragen (als Lead, Beteiligte oder in Aufgaben).\n\n" +
              "Trotzdem entfernen? Diese Einträge werden beim Bestätigen mit OK bereinigt.";

        var answer = MessageBox.Show(this, question, "Person entfernen",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (answer == MessageBoxResult.Yes)
            _viewModel.RemoveSelected();
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
