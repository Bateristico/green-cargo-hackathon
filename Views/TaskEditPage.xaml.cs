using CouchbaseHackathonApp.Models;

namespace CouchbaseHackathonApp.Views;

public partial class TaskEditPage : ContentPage
{
    private DatabaseService _dbService;
    private TaskItem? _task;
    private bool _isEditMode;

    public TaskEditPage()
    {
        InitializeComponent();
        _dbService = ((App)Application.Current!).DatabaseService;
        _isEditMode = false;
        Title = "Add Task";
    }

    public TaskEditPage(TaskItem task) : this()
    {
        _task = task;
        _isEditMode = true;
        Title = "Edit Task";
        
        TitleEntry.Text = task.Title;
        DescriptionEditor.Text = task.Description;
        CompletedCheckBox.IsChecked = task.IsCompleted;
        DeleteButton.IsVisible = true;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            await DisplayAlert("Error", "Please enter a title", "OK");
            return;
        }

        if (_isEditMode && _task != null)
        {
            _task.Title = TitleEntry.Text;
            _task.Description = DescriptionEditor.Text ?? "";
            _task.IsCompleted = CompletedCheckBox.IsChecked;
            
            _dbService.UpdateTask(_task);
        }
        else
        {
            var newTask = new TaskItem
            {
                Title = TitleEntry.Text,
                Description = DescriptionEditor.Text ?? "",
                IsCompleted = CompletedCheckBox.IsChecked,
                CreatedAt = DateTime.UtcNow
            };
            
            _dbService.CreateTask(newTask);
        }

        await Navigation.PopAsync();
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (_task == null) return;

        bool confirm = await DisplayAlert(
            "Delete Task",
            $"Delete '{_task.Title}'?",
            "Delete",
            "Cancel");

        if (confirm)
        {
            _dbService.DeleteTask(_task.Id);
            await Navigation.PopAsync();
        }
    }
}