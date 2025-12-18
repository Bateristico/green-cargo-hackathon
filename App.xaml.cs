using Microsoft.Extensions.DependencyInjection;
using CouchbaseHackathonApp.Services;

namespace CouchbaseHackathonApp
{
    public partial class App : Application
    {
        public DatabaseService DatabaseService { get; private set; }
        public SyncService SyncService { get; private set; }

        public App(DatabaseService databaseService)
        {
            InitializeComponent();
            DatabaseService = databaseService;
            
            // Auto-start sync on app launch
            if (DatabaseService.Database != null)
            {
                SyncService = new SyncService(DatabaseService.Database);
                
                // Start sync automatically with hardcoded credentials
                SyncService.StartSync(
                    "wss://chczsofr8gfp8ihw.apps.cloud.couchbase.com:4984/edge-platform-endpoint",
                    "syncer",
                    ",)aQ9ca7VtZ,,gD"
                );
                
                Console.WriteLine("Auto-sync started on app launch");
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}