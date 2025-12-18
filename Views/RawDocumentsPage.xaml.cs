using System.Collections.ObjectModel;
using System.Diagnostics;

namespace CouchbaseHackathonApp.Views
{
    public partial class RawDocumentsPage : ContentPage
    {
        private readonly DatabaseService _databaseService;
        public ObservableCollection<DocumentInfo> Documents { get; set; }

        public RawDocumentsPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            Documents = new ObservableCollection<DocumentInfo>();
            DocumentsCollectionView.ItemsSource = Documents;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDocuments();
        }

        private async Task LoadDocuments()
        {
            try
            {
                Console.WriteLine("🔍 Loading ALL documents from registry.cargo...");
                
                Documents.Clear();
                var docs = await _databaseService.GetAllDocumentsRaw();
                
                Console.WriteLine($"📊 Found {docs.Count} documents in registry.cargo");
                
                foreach (var doc in docs)
                {
                    Documents.Add(doc);
                }

                CountLabel.Text = $"📄 Total Documents: {Documents.Count}";
                EmptyState.IsVisible = Documents.Count == 0;
                DocumentsCollectionView.IsVisible = Documents.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading documents: {ex.Message}");
                await DisplayAlert("Error", $"Failed to load documents: {ex.Message}", "OK");
            }
        }
    }

    public class DocumentInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string DataJson { get; set; } = string.Empty;
        public string Sequence { get; set; } = string.Empty;
    }
}
