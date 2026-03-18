using Avalonia.Styling;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IDataService _dataService;
    private readonly Dictionary<TodoItem, IDisposable> _subscriptions = new();

    public ObservableCollection<TodoItem> Tasks { get; } = new();

    private string _newTaskText = string.Empty;
    public string NewTaskText
    {
        get => _newTaskText;
        set => this.RaiseAndSetIfChanged(ref _newTaskText, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    private TodoItem? _selectedTask;
    public TodoItem? SelectedTask
    {
        get => _selectedTask;
        set => this.RaiseAndSetIfChanged(ref _selectedTask, value);
    }

    private string _currentFilter = "All";
    public string CurrentFilter
    {
        get => _currentFilter;
        set
        {
            this.RaiseAndSetIfChanged(ref _currentFilter, value);
            this.RaisePropertyChanged(nameof(FilteredTasks));
        }
    }

    private ThemeVariant _currentTheme = ThemeVariant.Default;
    public ThemeVariant CurrentTheme
    {
        get => _currentTheme;
        set => this.RaiseAndSetIfChanged(ref _currentTheme, value);
    }

    public IEnumerable<TodoItem> FilteredTasks =>
        CurrentFilter switch
        {
            "Active" => Tasks.Where(t => !t.IsCompleted),
            "Completed" => Tasks.Where(t => t.IsCompleted),
            _ => Tasks
        };

    public ReactiveCommand<Unit, Unit> AddTaskCommand { get; }
    public ReactiveCommand<TodoItem, Unit> DeleteTaskCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteSelectedTaskCommand { get; }
    public ReactiveCommand<string, Unit> SetFilterCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleThemeCommand { get; }

    public MainWindowViewModel()
        : this(new JsonDataService())
    {
    }

    public MainWindowViewModel(IDataService dataService)
    {
        _dataService = dataService;

        AddTaskCommand = ReactiveCommand.CreateFromTask(AddTaskAsync);
        DeleteTaskCommand = ReactiveCommand.CreateFromTask<TodoItem>(DeleteTaskAsync);
        DeleteSelectedTaskCommand = ReactiveCommand.CreateFromTask(DeleteSelectedTaskAsync);
        SetFilterCommand = ReactiveCommand.Create<string>(SetFilter);
        ToggleThemeCommand = ReactiveCommand.Create(ToggleTheme);

        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        var items = await _dataService.LoadTasksAsync();

        Tasks.Clear();

        foreach (var item in items.OrderByDescending(x => x.CreatedAt))
        {
            Tasks.Add(item);
            SubscribeToTask(item);
        }

        this.RaisePropertyChanged(nameof(FilteredTasks));
    }

    public async Task SaveAsync()
    {
        await _dataService.SaveTasksAsync(Tasks);
    }

    private async Task AddTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTaskText))
        {
            ErrorMessage = "Задача не может быть пустой";
            return;
        }

        var task = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = NewTaskText.Trim(),
            IsCompleted = false,
            CreatedAt = DateTime.Now
        };

        Tasks.Insert(0, task);
        SubscribeToTask(task);

        NewTaskText = string.Empty;
        ErrorMessage = string.Empty;

        this.RaisePropertyChanged(nameof(FilteredTasks));

        await SaveAsync();
    }

    private async Task DeleteTaskAsync(TodoItem task)
    {
        if (task == null)
            return;

        UnsubscribeFromTask(task);
        Tasks.Remove(task);

        this.RaisePropertyChanged(nameof(FilteredTasks));

        await SaveAsync();
    }

    private async Task DeleteSelectedTaskAsync()
    {
        if (SelectedTask == null)
            return;

        var task = SelectedTask;
        SelectedTask = null;

        UnsubscribeFromTask(task);
        Tasks.Remove(task);

        this.RaisePropertyChanged(nameof(FilteredTasks));

        await SaveAsync();
    }

    private void SetFilter(string filter)
    {
        CurrentFilter = filter;
    }

    private void ToggleTheme()
    {
        CurrentTheme = CurrentTheme == ThemeVariant.Dark
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
    }

    private void SubscribeToTask(TodoItem task)
    {
        var subscription = task.Changed.Subscribe(async _ =>
        {
            this.RaisePropertyChanged(nameof(FilteredTasks));
            await SaveAsync();
        });

        _subscriptions[task] = subscription;
    }

    private void UnsubscribeFromTask(TodoItem task)
    {
        if (_subscriptions.TryGetValue(task, out var subscription))
        {
            subscription.Dispose();
            _subscriptions.Remove(task);
        }
    }
}