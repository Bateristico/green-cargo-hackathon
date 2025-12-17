using CouchbaseHackathonApp.Models;
using System.Collections.ObjectModel;

namespace CouchbaseHackathonApp.Views;

public partial class WagonsListPage : ContentPage
{
    private DatabaseService _dbService;
    private ObservableCollection<Wagon> _wagons;

    public WagonsListPage()
    {
        InitializeComponent();
        _dbService = ((App)Application.Current!).DatabaseService;
        _wagons = new ObservableCollection<Wagon>();
        WagonsCollection.ItemsSource = _wagons;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadWagons();
    }

    private void LoadWagons()
    {
        _wagons.Clear();
        var wagons = _dbService.GetAllWagons();
        
        foreach (var wagon in wagons)
        {
            _wagons.Add(wagon);
        }
    }

    private async void OnAddWagonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new WagonDetailsPage());
    }

    private async void OnWagonTapped(object sender, EventArgs e)
    {
        var frame = (Frame)sender;
        var wagon = (Wagon)frame.BindingContext;
        
        await Navigation.PushAsync(new WagonDetailsPage(wagon));
    }

    private async void OnDeleteWagonClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var wagon = (Wagon)button.BindingContext;
        
        bool confirm = await DisplayAlert(
            "Delete Wagon", 
            $"Delete wagon '{wagon.WagonNumber}'?", 
            "Delete", 
            "Cancel");
        
        if (confirm)
        {
            _dbService.DeleteWagon(wagon.Id);
            LoadWagons();
        }
    }
}
