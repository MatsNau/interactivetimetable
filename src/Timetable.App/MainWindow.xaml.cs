using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Timetable.App.Controls;
using Timetable.App.ViewModels;
using Timetable.Application.Timeline;

namespace Timetable.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }

    /// <summary>Hält Kopfzeile und linke Tabelle mit der Zeitachse synchron (eingefrorene Bereiche).</summary>
    private void MainScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        HeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
        LeftScroll.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void LeftRows_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: var context })
            DispatchRowAction(context);
    }

    private void TimelineRows_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || RowsControl.Model is not { } model)
            return;

        var index = (int)(e.GetPosition(RowsControl).Y / TimelineMetrics.RowHeight);
        if (index >= 0 && index < model.Rows.Count)
            DispatchRowAction(model.Rows[index]);
    }

    private void WeekMainScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        WeekHeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
        WeekLeftScroll.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void WeekRows_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || WeekRows.Model is not { } model)
            return;

        var index = (int)(e.GetPosition(WeekRows).Y / TimelineMetrics.RowHeight);
        if (index >= 0 && index < model.Rows.Count)
            DispatchRowAction(model.Rows[index]);
    }

    // Kontextmenü-Einträge erben ihren DataContext (die Zeile) vom Platzierungsziel.
    private void EditProject_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as FrameworkElement)?.DataContext)
        {
            case ProjectHeaderRow row:
                _viewModel.EditProject(row.ProjectId);
                break;
            case WeekProjectRow row:
                _viewModel.EditProject(row.ProjectId);
                break;
        }
    }

    private void AddMilestone_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as FrameworkElement)?.DataContext)
        {
            case ProjectHeaderRow row:
                _viewModel.AddMilestone(row.ProjectId);
                break;
            case WeekProjectRow row:
                _viewModel.AddMilestone(row.ProjectId);
                break;
        }
    }

    private void EditMilestone_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as FrameworkElement)?.DataContext)
        {
            case MilestoneRow row:
                _viewModel.EditMilestone(row.MilestoneId);
                break;
            case WeekMilestoneRow row:
                _viewModel.EditMilestone(row.MilestoneId);
                break;
        }
    }

    private void AddTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: WeekMilestoneRow row })
            _viewModel.AddTask(row.MilestoneId);
    }

    private void EditTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: WeekTaskRow row })
            _viewModel.EditTask(row.TaskId);
    }

    private void DispatchRowAction(object? row)
    {
        switch (row)
        {
            case MilestoneRow milestone:
                _viewModel.EditMilestone(milestone.MilestoneId);
                break;
            case ProjectHeaderRow project:
                _viewModel.AddMilestone(project.ProjectId);
                break;
            // Wochenansicht: Doppelklick auf den Meilenstein legt eine Aufgabe darunter an.
            case WeekMilestoneRow milestone:
                _viewModel.AddTask(milestone.MilestoneId);
                break;
            case WeekTaskRow task:
                _viewModel.EditTask(task.TaskId);
                break;
            case WeekProjectRow project:
                _viewModel.AddMilestone(project.ProjectId);
                break;
        }
    }
}
