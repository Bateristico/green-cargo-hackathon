using Couchbase.Lite;
using Couchbase.Lite.Sync;

namespace CouchbaseHackathonApp.Services;

public class SyncService
{
    private Replicator? _replicator;
    private readonly Database _database;
    private ListenerToken? _replicatorListenerToken;

    public bool IsSyncing { get; private set; }
    public string SyncStatus { get; private set; } = "Not Connected";

    public event EventHandler<string>? StatusChanged;

    public SyncService(Database database)
    {
        _database = database;
    }

    public void StartSync(string syncGatewayUrl, string username, string password)
    {
        try
        {
            if (_replicator != null)
            {
                StopSync();
            }

            var targetEndpoint = new URLEndpoint(new Uri(syncGatewayUrl));
            var collection = _database.GetDefaultCollection();
            
            var collectionConfig = new CollectionConfiguration(collection);
            
            var config = new ReplicatorConfiguration(new[] { collectionConfig }, targetEndpoint)
            {
                Continuous = true,
                Authenticator = new BasicAuthenticator(username, password)
            };

            _replicator = new Replicator(config);

            _replicatorListenerToken = _replicator.AddChangeListener((sender, args) =>
            {
                var status = args.Status;
                
                if (status.Error != null)
                {
                    SyncStatus = $"Error: {status.Error.Message}";
                    IsSyncing = false;
                    Console.WriteLine($"Sync error: {status.Error.Message}");
                }
                else
                {
                    switch (status.Activity)
                    {
                        case ReplicatorActivityLevel.Busy:
                            SyncStatus = "Syncing...";
                            IsSyncing = true;
                            break;
                        case ReplicatorActivityLevel.Idle:
                            SyncStatus = "Connected (Idle)";
                            IsSyncing = true;
                            break;
                        case ReplicatorActivityLevel.Offline:
                            SyncStatus = "Offline";
                            IsSyncing = false;
                            break;
                        case ReplicatorActivityLevel.Stopped:
                            SyncStatus = "Stopped";
                            IsSyncing = false;
                            break;
                        case ReplicatorActivityLevel.Connecting:
                            SyncStatus = "Connecting...";
                            IsSyncing = false;
                            break;
                    }
                    
                    Console.WriteLine($"Sync status: {SyncStatus} - Progress: {status.Progress.Completed}/{status.Progress.Total}");
                }

                StatusChanged?.Invoke(this, SyncStatus);
            });

            _replicator.Start();
            Console.WriteLine($"Sync started to: {syncGatewayUrl}");
        }
        catch (Exception ex)
        {
            SyncStatus = $"Failed to start: {ex.Message}";
            IsSyncing = false;
            StatusChanged?.Invoke(this, SyncStatus);
            Console.WriteLine($"Sync start error: {ex.Message}");
        }
    }

    public void StopSync()
    {
        if (_replicator != null)
        {
            _replicatorListenerToken?.Remove();
            _replicator.Stop();
            _replicator.Dispose();
            _replicator = null;
            IsSyncing = false;
            SyncStatus = "Stopped";
            StatusChanged?.Invoke(this, SyncStatus);
            Console.WriteLine("Sync stopped");
        }
    }
}
