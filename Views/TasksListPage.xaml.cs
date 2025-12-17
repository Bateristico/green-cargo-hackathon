using CouchbaseHackathonApp.Models;
using System.Collections.ObjectModel;

namespace CouchbaseHackathonApp.Views;

public partial class TasksListPage : ContentPage
{
    private DatabaseService _dbService;
    private ObservableCollection<TaskItem> _tasks;

    public TasksListPage()
    {
        InitializeComponent();
        _dbService = ((App)Application.Current!).DatabaseService;
        _tasks = new ObservableCollection<TaskItem>();
        TasksCollection.ItemsSource = _tasks;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadTasks();
    }

    private void LoadTasks()
    {
        _tasks.Clear();
        var tasks = _dbService.GetAllTasks();
        
        foreach (var task in tasks)
        {
            _tasks.Add(task);
        }
    }

    private async void OnAddTaskClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new TaskEditPage());
    }

    private async void OnTaskTapped(object sender, EventArgs e)
    {
        var frame = (Frame)sender;
        var task = (TaskItem)frame.BindingContext;
        
        await Navigation.PushAsync(new TaskEditPage(task));
    }

    private void OnTaskCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        var checkbox = (CheckBox)sender;
        var task = (TaskItem)checkbox.BindingContext;
        
        task.IsCompleted = e.Value;
        _dbService.UpdateTask(task);
    }

    private async void OnDeleteTaskClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var task = (TaskItem)button.BindingContext;
        
        bool confirm = await DisplayAlert(
            "Delete Task", 
            $"Delete '{task.Title}'?", 
            "Delete", 
            "Cancel");
        
        if (confirm)
        {
            _dbService.DeleteTask(task.Id);
            LoadTasks();
        }
    }
}