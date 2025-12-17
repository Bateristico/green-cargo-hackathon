using CouchbaseHackathonApp.Models;

namespace CouchbaseHackathonApp.Views;

public partial class WagonDetailsPage : ContentPage
{
    private DatabaseService _dbService;
    private Wagon? _wagon;
    private bool _isEditMode;

    public WagonDetailsPage()
    {
        InitializeComponent();
        _dbService = ((App)Application.Current!).DatabaseService;
        _isEditMode = false;
        Title = "Add Wagon";
        
        StatusPicker.SelectedIndex = 0;
        LastInspectionPicker.Date = DateTime.Now;
    }

    public WagonDetailsPage(Wagon wagon) : this()
    {
        _wagon = wagon;
        _isEditMode = true;
        Title = "Edit Wagon";
        
        WagonNumberEntry.Text = wagon.WagonNumber;
        WagonTypeEntry.Text = wagon.WagonType;
        CurrentLocationEntry.Text = wagon.CurrentLocation;
        DestinationEntry.Text = wagon.Destination;
        StatusPicker.SelectedItem = wagon.Status;
        LegalCheckCheckBox.IsChecked = wagon.RequiresLegalCheck;
        LastInspectionPicker.Date = wagon.LastInspection;
        NotesEditor.Text = wagon.Notes;
        DeleteButton.IsVisible = true;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(WagonNumberEntry.Text))
        {
            await DisplayAlert("Error", "Please enter wagon number", "OK");
            return;
        }

        if (_isEditMode && _wagon != null)
        {
            _wagon.WagonNumber = WagonNumberEntry.Text;
            _wagon.WagonType = WagonTypeEntry.Text ?? "";
            _wagon.CurrentLocation = CurrentLocationEntry.Text ?? "";
            _wagon.Destination = DestinationEntry.Text ?? "";
            _wagon.Status = StatusPicker.SelectedItem?.ToString() ?? "Available";
            _wagon.RequiresLegalCheck = LegalCheckCheckBox.IsChecked;
            _wagon.LastInspection = LastInspectionPicker.Date ?? DateTime.Now;
            _wagon.Notes = NotesEditor.Text ?? "";
            
            _dbService.UpdateWagon(_wagon);
        }
        else
        {
            var newWagon = new Wagon
            {
                WagonNumber = WagonNumberEntry.Text,
                WagonType = WagonTypeEntry.Text ?? "",
                CurrentLocation = CurrentLocationEntry.Text ?? "",
                Destination = DestinationEntry.Text ?? "",
                Status = StatusPicker.SelectedItem?.ToString() ?? "Available",
                RequiresLegalCheck = LegalCheckCheckBox.IsChecked,
                LastInspection = LastInspectionPicker.Date ?? DateTime.Now,
                Notes = NotesEditor.Text ?? "",
                CreatedAt = DateTime.UtcNow
            };
            
            _dbService.CreateWagon(newWagon);
        }

        await Navigation.PopAsync();
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (_wagon == null) return;

        bool confirm = await DisplayAlert(
            "Delete Wagon",
            $"Delete wagon '{_wagon.WagonNumber}'?",
            "Delete",
            "Cancel");

        if (confirm)
        {
            _dbService.DeleteWagon(_wagon.Id);
            await Navigation.PopAsync();
        }
    }
}
