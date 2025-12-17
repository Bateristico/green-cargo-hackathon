using Microsoft.Extensions.DependencyInjection;

namespace CouchbaseHackathonApp
{
    public partial class App : Application
    {
        public DatabaseService DatabaseService { get; private set; }

        public App(DatabaseService databaseService)
        {
            InitializeComponent();
            DatabaseService = databaseService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}