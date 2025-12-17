using CouchbaseHackathonApp.Models;
using CouchbaseHackathonApp.Services;

namespace CouchbaseHackathonApp.Views;

public partial class SettingsPage : ContentPage
{
    private DatabaseService _dbService;
    private SyncService _syncService;

    public SettingsPage()
    {
        InitializeComponent();
        _dbService = ((App)Application.Current!).DatabaseService;
        
        if (_dbService.Database != null)
        {
            _syncService = new SyncService(_dbService.Database);
            _syncService.StatusChanged += OnSyncStatusChanged;
            
            UpdateSyncButton();
        }
    }

    private void OnSyncStatusChanged(object? sender, string status)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = status;
            UpdateSyncButton();
        });
    }

    private void UpdateSyncButton()
    {
        if (_syncService.IsSyncing)
        {
            SyncButton.Text = "Stop Sync";
            SyncButton.BackgroundColor = Color.FromArgb("#DC3545");
            StatusFrame.BackgroundColor = Color.FromArgb("#E8F5E9");
        }
        else
        {
            SyncButton.Text = "Start Sync";
            SyncButton.BackgroundColor = Color.FromArgb("#2196F3");
            StatusFrame.BackgroundColor = Color.FromArgb("#F5F5F5");
        }
    }

    private async void OnSyncButtonClicked(object sender, EventArgs e)
    {
        if (_syncService.IsSyncing)
        {
            _syncService.StopSync();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(SyncUrlEntry.Text))
            {
                await DisplayAlert("Error", "Please enter Sync Gateway URL", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(UsernameEntry.Text))
            {
                await DisplayAlert("Error", "Please enter username", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await DisplayAlert("Error", "Please enter password", "OK");
                return;
            }

            _syncService.StartSync(
                SyncUrlEntry.Text,
                UsernameEntry.Text,
                PasswordEntry.Text
            );
        }
    }

    private async void OnGenerateTestDataClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Generate Test Data",
            "Create 20 sample wagons?",
            "Yes",
            "Cancel");

        if (!confirm) return;

        var random = new Random();
        var wagonTypes = new[] { "Freight", "Passenger", "Tank", "Hopper", "Flatcar" };
        var statuses = new[] { "Available", "In Transit", "Under Inspection", "Maintenance" };
        var locations = new[] { "Track A1", "Track A2", "Track B1", "Track B2", "Track C1", "Platform 1", "Depot" };
        var destinations = new[] { "Stockholm", "Gothenburg", "Malmö", "Uppsala", "Örebro" };

        for (int i = 1; i <= 20; i++)
        {
            var wagon = new Wagon
            {
                WagonNumber = $"WGN-{1000 + i}",
                WagonType = wagonTypes[random.Next(wagonTypes.Length)],
                Status = statuses[random.Next(statuses.Length)],
                CurrentLocation = locations[random.Next(locations.Length)],
                Destination = destinations[random.Next(destinations.Length)],
                RequiresLegalCheck = random.Next(100) < 20,
                LastInspection = DateTime.Now.AddDays(-random.Next(30)),
                Notes = $"Test wagon #{i}",
                CreatedAt = DateTime.UtcNow
            };

            _dbService.CreateWagon(wagon);
        }

        await DisplayAlert("Success", "20 test wagons created!", "OK");
    }
}
