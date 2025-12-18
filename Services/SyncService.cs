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
            Console.WriteLine("--->>> StartSync called");
            
            if (_replicator != null)
            {
                Console.WriteLine("--->>> Stopping existing replicator");
                StopSync();
            }

            // Initialize the replicator endpoint
            Console.WriteLine($"--->>> Creating endpoint: {syncGatewayUrl}");
            var targetEndpoint = new URLEndpoint(new Uri(syncGatewayUrl));
            
            // Get the specific collection from Sync Gateway (scope.collection = registry.cargo)
            Console.WriteLine("--->>> Getting collection 'cargo' from scope 'registry'");
            var collection = _database.GetCollection("cargo", "registry");
            if (collection == null)
            {
                Console.WriteLine("--->>> Collection not found, creating registry.cargo...");
                collection = _database.CreateCollection("cargo", "registry");
                Console.WriteLine($"--->>> Created collection: registry.cargo");
            }
            else
            {
                Console.WriteLine($"--->>> Found existing collection: {collection.Scope.Name}.{collection.Name}");
            }
            
            Console.WriteLine($"--->>> Using collection for sync: {collection.Scope.Name}.{collection.Name}");
            
            // Configure replication using Couchbase Lite 4.0 Collections API
            Console.WriteLine("--->>> Configuring collection with ConflictResolver.Default");
            var collectionConfig = new CollectionConfiguration(collection)
            {
                ConflictResolver = ConflictResolver.Default
            };
            
            // Initialize the replicator configuration - PUSH ONLY!
            Console.WriteLine("--->>> ========== PUSH ONLY MODE ENABLED ==========");
            Console.WriteLine("--->>> ReplicatorType.Push - NO DOWNLOAD FROM SERVER");
            var config = new ReplicatorConfiguration(new[] { collectionConfig }, targetEndpoint)
            {
                Continuous = true,
                ReplicatorType = ReplicatorType.Push, // Тільки відправка на сервер, без завантаження
                Authenticator = new BasicAuthenticator(username, password)
            };
            Console.WriteLine($"--->>> Config created: Type={config.ReplicatorType}, Continuous={config.Continuous}");
            
            Console.WriteLine($"--->>> Replicator configured: Continuous sync, Auth: {username}");

            // Initialize replicator with configuration data
            Console.WriteLine("--->>> Creating Replicator instance");
            _replicator = new Replicator(config);
            
            // Add document replication listener to debug incoming data
            Console.WriteLine("--->>> Adding document replication listener");
            _replicator.AddDocumentReplicationListener((sender, args) =>
            {
                Console.WriteLine($"--->>> Document replication event: {args.Documents.Count} documents");
                
                foreach (var doc in args.Documents)
                {
                    if (doc.Error != null)
                    {
                        Console.WriteLine($"--->>> ❌ Document replication ERROR: {doc.Id}");
                        Console.WriteLine($"--->>>    Error message: {doc.Error.Message}");
                    }
                    else
                    {
                        Console.WriteLine($"--->>> ✓ Document synced successfully: {doc.Id}");
                        
                        // Try to read the document to see what came from backend
                        try
                        {
                            var savedDoc = collection.GetDocument(doc.Id);
                            if (savedDoc != null)
                            {
                                var docType = savedDoc.GetString("type");
                                Console.WriteLine($"--->>>    Document type: {docType}");
                                
                                if (docType == "wagon")
                                {
                                    var wagonNumber = savedDoc.GetString("wagonNumber");
                                    Console.WriteLine($"--->>>    Wagon from backend: {wagonNumber}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"--->>>    Error reading synced doc: {ex.Message}");
                        }
                    }
                }
            });

            // Optionally add a change listener
            Console.WriteLine("--->>> Adding status change listener");
            _replicatorListenerToken = _replicator.AddChangeListener((sender, args) =>
            {
                var status = args.Status;
                
                Console.WriteLine($"--->>> Replicator status changed: Activity={status.Activity}");
                
                if (status.Error != null)
                {
                    SyncStatus = $"Error: {status.Error.Message}";
                    IsSyncing = false;
                    Console.WriteLine($"--->>> ❌ Sync ERROR: {status.Error.Message}");
                    Console.WriteLine($"--->>>    Error type: {status.Error.GetType().Name}");
                }
                else
                {
                    switch (status.Activity)
                    {
                        case ReplicatorActivityLevel.Busy:
                            SyncStatus = "Syncing...";
                            IsSyncing = true;
                            Console.WriteLine("--->>> 🔄 Replication BUSY - actively syncing data");
                            Console.WriteLine($"--->>>    Progress: {status.Progress.Completed}/{status.Progress.Total} documents");
                            break;
                        case ReplicatorActivityLevel.Idle:
                            SyncStatus = "Connected (Idle)";
                            IsSyncing = true;
                            Console.WriteLine("--->>> ✓ Replication IDLE - connected and waiting for changes");
                            Console.WriteLine($"--->>>    Progress: {status.Progress.Completed}/{status.Progress.Total} documents");
                            break;
                        case ReplicatorActivityLevel.Offline:
                            SyncStatus = "Offline";
                            IsSyncing = false;
                            Console.WriteLine("--->>> ⚠ Replication OFFLINE - no network connection");
                            break;
                        case ReplicatorActivityLevel.Stopped:
                            SyncStatus = "Stopped";
                            IsSyncing = false;
                            Console.WriteLine("--->>> ⏹ Replication STOPPED");
                            break;
                        case ReplicatorActivityLevel.Connecting:
                            SyncStatus = "Connecting...";
                            IsSyncing = false;
                            Console.WriteLine("--->>> 🔌 Replication CONNECTING to Sync Gateway");
                            break;
                    }
                    
                    Console.WriteLine($"--->>> Sync status: {SyncStatus}");
                }

                StatusChanged?.Invoke(this, SyncStatus);
            });

            // Start replicator
            Console.WriteLine("--->>> Starting replicator...");
            _replicator.Start();
            Console.WriteLine($"--->>> ✓ Replicator started successfully to: {syncGatewayUrl}");
        }
        catch (Exception ex)
        {
            SyncStatus = $"Failed to start: {ex.Message}";
            IsSyncing = false;
            StatusChanged?.Invoke(this, SyncStatus);
            Console.WriteLine($"--->>> ❌ CRITICAL ERROR starting sync: {ex.Message}");
            Console.WriteLine($"--->>>    Stack trace: {ex.StackTrace}");
        }
    }

    public void StopSync()
    {
        Console.WriteLine("--->>> StopSync called");
        if (_replicator != null)
        {
            Console.WriteLine("--->>> Removing listener token");
            _replicatorListenerToken?.Remove();
            Console.WriteLine("--->>> Stopping replicator");
            _replicator.Stop();
            Console.WriteLine("--->>> Disposing replicator");
            _replicator.Dispose();
            _replicator = null;
            IsSyncing = false;
            SyncStatus = "Stopped";
            StatusChanged?.Invoke(this, SyncStatus);
            Console.WriteLine("--->>> ✓ Sync stopped successfully");
        }
        else
        {
            Console.WriteLine("--->>> No active replicator to stop");
        }
    }
}
