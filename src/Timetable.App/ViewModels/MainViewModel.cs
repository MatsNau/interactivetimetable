using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Timetable.Application.Persistence;
using Timetable.Application.Timeline;
using Timetable.Application.Workspaces;
using Timetable.App.Services;
using Timetable.Domain.Planning;
using Timetable.Domain.Time;

namespace Timetable.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IAsyncDisposable, IDisposable
{
    private const string FileFilter = "Projektplan (*.json)|*.json|Alle Dateien (*.*)|*.*";

    private readonly IPlanWorkspaceFactory _workspaceFactory;
    private readonly SettingsStore _settings;
    private readonly DispatcherTimer _presenceTimer;
    private IPlanWorkspace? _workspace;
    private PlanVersionToken _version;
    private bool _disposed;

    [ObservableProperty]
    private TimelineModel _timeline;

    [ObservableProperty]
    private WeekModel _week;

    [ObservableProperty]
    private bool _isWeekView;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WeekTitle))]
    private IsoWeek _selectedWeek;

    [ObservableProperty]
    private string _statusText = "Demodaten — noch keine Datei geöffnet";

    [ObservableProperty]
    private string _presenceText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private string? _currentFilePath;

    public ProjectPlan Plan { get; private set; }

    public string Title => CurrentFilePath is null
        ? $"{Plan.Title} — Stand {Plan.AsOfDate:dd.MM.yyyy}"
        : $"{Plan.Title} — Stand {Plan.AsOfDate:dd.MM.yyyy} — {CurrentFilePath}";

    public MainViewModel(IPlanWorkspaceFactory workspaceFactory, SettingsStore settings)
    {
        _workspaceFactory = workspaceFactory;
        _settings = settings;
        Plan = DemoData.CreatePlan();
        _timeline = BuildTimeline(Plan);
        _selectedWeek = IsoWeek.FromDate(DateOnly.FromDateTime(DateTime.Today));
        _week = BuildWeek(Plan, _selectedWeek);

        _presenceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _presenceTimer.Tick += async (_, _) => await RefreshPresenceAsync();
    }

    /// <summary>Beim Start: zuletzt geöffnete Datei wieder öffnen, falls vorhanden.</summary>
    public async Task InitializeAsync()
    {
        if (_settings.LastPlanPath is { } lastPath && File.Exists(lastPath))
            await OpenFileAsync(lastPath);
    }

    [RelayCommand]
    private async Task NewPlanAsync()
    {
        await CloseWorkspaceAsync();
        Plan = new ProjectPlan
        {
            Title = "Neuer Projektzeitplan",
            AsOfDate = DateOnly.FromDateTime(DateTime.Today),
        };
        _version = default;
        CurrentFilePath = null;
        RefreshTimeline();
        StatusText = "Neuer Plan — noch nicht gespeichert";
    }

    [RelayCommand]
    private async Task OpenPlanAsync()
    {
        var dialog = new OpenFileDialog { Filter = FileFilter, Title = "Projektplan öffnen" };
        if (dialog.ShowDialog() == true)
            await OpenFileAsync(dialog.FileName);
    }

    [RelayCommand]
    private async Task SavePlanAsync()
    {
        if (_workspace is null)
        {
            await SavePlanAsAsync();
            return;
        }

        await SaveCoreAsync(_workspace, overwrite: false);
    }

    [RelayCommand]
    private async Task SavePlanAsAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = FileFilter,
            Title = "Projektplan speichern",
            FileName = "plan.json",
        };
        if (dialog.ShowDialog() != true)
            return;

        await CloseWorkspaceAsync();
        _workspace = _workspaceFactory.Create(dialog.FileName);
        _version = default;
        CurrentFilePath = dialog.FileName;

        // Der Speichern-Dialog hat ein Überschreiben bereits bestätigt.
        if (await SaveCoreAsync(_workspace, overwrite: true))
            await StartCollaborationAsync(dialog.FileName);
    }

    private async Task OpenFileAsync(string path)
    {
        try
        {
            var workspace = _workspaceFactory.Create(path);
            var document = await workspace.Repository.LoadAsync();

            await CloseWorkspaceAsync();
            _workspace = workspace;
            Plan = document.Plan;
            _version = document.Version;
            CurrentFilePath = path;
            RefreshTimeline();
            StatusText = $"Geladen: {Path.GetFileName(path)}";
            await StartCollaborationAsync(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Die Datei konnte nicht geladen werden:\n{ex.Message}",
                "Fehler beim Öffnen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<bool> SaveCoreAsync(IPlanWorkspace workspace, bool overwrite)
    {
        try
        {
            _version = await workspace.Repository.SaveAsync(Plan, _version, overwrite);
            StatusText = $"Gespeichert um {DateTime.Now:HH:mm:ss}";
            return true;
        }
        catch (PlanConflictException)
        {
            var answer = MessageBox.Show(
                "Jemand anderes hat den Plan zwischenzeitlich gespeichert.\n\n" +
                "Trotzdem überschreiben? Die Änderungen der anderen Person gehen dann verloren " +
                "(ein Backup der Datei wird automatisch angelegt).",
                "Speicherkonflikt", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

            if (answer == MessageBoxResult.Yes)
                return await SaveCoreAsync(workspace, overwrite: true);

            StatusText = "Speichern abgebrochen — die Datei wurde nicht überschrieben";
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Speichern fehlgeschlagen:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private async Task StartCollaborationAsync(string path)
    {
        if (_workspace is null)
            return;

        await _workspace.Presence.AnnounceAsync();
        _settings.LastPlanPath = path;
        _settings.Save();
        _presenceTimer.Start();
        await RefreshPresenceAsync();
    }

    /// <summary>Bearbeitet Titel und "Stand"-Datum des Plans (Werkzeugleiste).</summary>
    [RelayCommand]
    private void EditPlanInfo()
    {
        var editor = new PlanInfoEditorViewModel(Plan);
        var dialog = new PlanInfoDialog(editor) { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true)
            return;

        editor.ApplyTo(Plan);
        StatusText = "Plandaten geändert — noch nicht gespeichert";
        RefreshTimeline();
    }

    /// <summary>Öffnet die Ferien- und Event-Verwaltung (Werkzeugleiste).</summary>
    [RelayCommand]
    private void EditCalendar()
    {
        var editor = new CalendarEditorViewModel(Plan);
        var dialog = new CalendarDialog(editor) { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true)
            return;

        editor.ApplyTo(Plan);
        StatusText = "Ferien/Events geändert — noch nicht gespeichert";
        RefreshTimeline();
    }

    /// <summary>Öffnet die Personenverwaltung (Werkzeugleiste).</summary>
    [RelayCommand]
    private void EditPeople()
    {
        var editor = new PeopleEditorViewModel(Plan);
        var dialog = new PeopleDialog(editor) { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true)
            return;

        editor.ApplyTo(Plan);
        StatusText = "Personen geändert — noch nicht gespeichert";
        RefreshTimeline();
    }

    /// <summary>Legt ein neues Projekt an (Werkzeugleiste).</summary>
    [RelayCommand]
    private void AddProject()
    {
        var editor = new ProjectEditorViewModel(Plan, existing: null);
        var dialog = new ProjectDialog(editor, canDelete: false, milestoneCount: 0) { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true || dialog.Result != ProjectDialogResult.Save)
            return;

        var project = new Project { Name = string.Empty };
        editor.ApplyTo(project);
        Plan.Projects.Add(project);
        StatusText = "Projekt angelegt — noch nicht gespeichert";
        RefreshTimeline();
    }

    /// <summary>Öffnet den Bearbeitungsdialog für ein bestehendes Projekt (Kontextmenü der Projektzeile).</summary>
    public void EditProject(Guid projectId)
    {
        var project = Plan.Projects.FirstOrDefault(p => p.Id == projectId);
        if (project is null)
            return;

        var editor = new ProjectEditorViewModel(Plan, project);
        var dialog = new ProjectDialog(editor, canDelete: true, milestoneCount: project.Milestones.Count) { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true)
            return;

        if (dialog.Result == ProjectDialogResult.Delete)
        {
            Plan.Projects.Remove(project);
            StatusText = "Projekt gelöscht — noch nicht gespeichert";
        }
        else
        {
            editor.ApplyTo(project);
            StatusText = "Projekt geändert — noch nicht gespeichert";
        }

        RefreshTimeline();
    }

    /// <summary>Öffnet den Bearbeitungsdialog für einen bestehenden Meilenstein (Doppelklick auf die Zeile).</summary>
    public void EditMilestone(Guid milestoneId)
    {
        var (project, milestone) = FindMilestone(milestoneId);
        if (project is null || milestone is null)
            return;

        var editor = new MilestoneEditorViewModel(Plan, milestone, project.Name);
        var dialog = new MilestoneDialog(editor, canDelete: true) { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true)
            return;

        if (dialog.Result == MilestoneDialogResult.Delete)
        {
            project.Milestones.Remove(milestone);
            StatusText = "Meilenstein gelöscht — noch nicht gespeichert";
        }
        else
        {
            editor.ApplyTo(milestone);
            StatusText = "Meilenstein geändert — noch nicht gespeichert";
        }

        RefreshTimeline();
    }

    /// <summary>Legt einen neuen Meilenstein im Projekt an (Doppelklick auf die Projektzeile).</summary>
    public void AddMilestone(Guid projectId)
    {
        var project = Plan.Projects.FirstOrDefault(p => p.Id == projectId);
        if (project is null)
            return;

        var editor = new MilestoneEditorViewModel(Plan, existing: null, project.Name);
        var dialog = new MilestoneDialog(editor, canDelete: false) { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true || dialog.Result != MilestoneDialogResult.Save)
            return;

        var milestone = new Milestone { Title = string.Empty };
        editor.ApplyTo(milestone);
        project.Milestones.Add(milestone);
        StatusText = "Meilenstein angelegt — noch nicht gespeichert";
        RefreshTimeline();
    }

    /// <summary>Legt eine neue Aufgabe unter dem Meilenstein an (Wochenansicht, Doppelklick auf die Meilensteinzeile).</summary>
    public void AddTask(Guid milestoneId)
    {
        var (_, milestone) = FindMilestone(milestoneId);
        if (milestone is null)
            return;

        // Vorbelegung: Montag bis Freitag der angezeigten Woche.
        var monday = SelectedWeek.Monday;
        var editor = new TaskEditorViewModel(Plan, existing: null, milestone.Title, monday, monday.AddDays(4));
        var dialog = new TaskDialog(editor, canDelete: false) { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true || dialog.Result != TaskDialogResult.Save)
            return;

        var task = new TaskItem { Title = string.Empty };
        editor.ApplyTo(task);
        milestone.Tasks.Add(task);
        StatusText = "Aufgabe angelegt — noch nicht gespeichert";
        RefreshTimeline();
    }

    /// <summary>Öffnet den Bearbeitungsdialog für eine bestehende Aufgabe (Doppelklick auf die Zeile).</summary>
    public void EditTask(Guid taskId)
    {
        var (milestone, task) = FindTask(taskId);
        if (milestone is null || task is null)
            return;

        var editor = new TaskEditorViewModel(Plan, task, milestone.Title, task.Start, task.End);
        var dialog = new TaskDialog(editor, canDelete: true) { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true)
            return;

        if (dialog.Result == TaskDialogResult.Delete)
        {
            milestone.Tasks.Remove(task);
            StatusText = "Aufgabe gelöscht — noch nicht gespeichert";
        }
        else
        {
            editor.ApplyTo(task);
            StatusText = "Aufgabe geändert — noch nicht gespeichert";
        }

        RefreshTimeline();
    }

    private (Milestone? Milestone, TaskItem? Task) FindTask(Guid taskId)
    {
        foreach (var milestone in Plan.Projects.SelectMany(p => p.Milestones))
        {
            if (milestone.Tasks.FirstOrDefault(t => t.Id == taskId) is { } task)
                return (milestone, task);
        }

        return (null, null);
    }

    private (Project? Project, Milestone? Milestone) FindMilestone(Guid milestoneId)
    {
        foreach (var project in Plan.Projects)
        {
            if (project.Milestones.FirstOrDefault(m => m.Id == milestoneId) is { } milestone)
                return (project, milestone);
        }

        return (null, null);
    }

    private async Task RefreshPresenceAsync()
    {
        if (_workspace is null)
        {
            PresenceText = string.Empty;
            return;
        }

        try
        {
            var others = await _workspace.Presence.GetOtherUsersAsync();
            PresenceText = others.Count == 0
                ? string.Empty
                : "Ebenfalls geöffnet: " + string.Join(", ", others.Select(o => o.UserName));
        }
        catch (Exception)
        {
            // Netzlaufwerk kurz nicht erreichbar — der nächste Tick versucht es erneut.
        }
    }

    private void RefreshTimeline()
    {
        Timeline = BuildTimeline(Plan);
        Week = BuildWeek(Plan, SelectedWeek);
        OnPropertyChanged(nameof(Title));
    }

    private static TimelineModel BuildTimeline(ProjectPlan plan)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return TimelineBuilder.Build(plan, TimelineRange.Covering(plan, today), today);
    }

    private static WeekModel BuildWeek(ProjectPlan plan, IsoWeek week) =>
        WeekBuilder.Build(plan, week, DateOnly.FromDateTime(DateTime.Today));

    public string WeekTitle =>
        $"{SelectedWeek} · {SelectedWeek.Monday:dd.MM.}–{SelectedWeek.Sunday:dd.MM.yyyy}";

    partial void OnSelectedWeekChanged(IsoWeek value) => Week = BuildWeek(Plan, value);

    [RelayCommand]
    private void PreviousWeek() => SelectedWeek = SelectedWeek.Previous();

    [RelayCommand]
    private void NextWeek() => SelectedWeek = SelectedWeek.Next();

    [RelayCommand]
    private void GoToCurrentWeek() => SelectedWeek = IsoWeek.FromDate(DateOnly.FromDateTime(DateTime.Today));

    private async Task CloseWorkspaceAsync()
    {
        _presenceTimer.Stop();
        PresenceText = string.Empty;

        if (_workspace is not null)
        {
            await _workspace.DisposeAsync();
            _workspace = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;
        await CloseWorkspaceAsync();
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}
