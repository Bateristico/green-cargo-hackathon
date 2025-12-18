using Couchbase.Lite;
using Couchbase.Lite.Query;
using CouchbaseHackathonApp.Models;

namespace CouchbaseHackathonApp;

public class DatabaseService
{
    private Database? _database;

    public Database? Database
    {
        get
        {
            if (_database == null)
            {
                InitializeDatabaseAsync().Wait();
            }
            return _database;
        }
    }

    private Collection GetSyncedCollection()
    {
        if (Database == null)
        {
            Console.WriteLine("ERROR: Database is NULL!");
            throw new InvalidOperationException("Database not initialized");
        }

        Console.WriteLine($"Getting collection 'cargo' from scope 'registry'...");
        
        // Get or create the registry.cargo collection used by sync
        var collection = Database.GetCollection("cargo", "registry");
        if (collection == null)
        {
            Console.WriteLine("Collection not found, creating registry.cargo...");
            collection = Database.CreateCollection("cargo", "registry");
            Console.WriteLine($"✓ Created collection: {collection.Scope.Name}.{collection.Name}");
        }
        else
        {
            Console.WriteLine($"✓ Using existing collection: {collection.Scope.Name}.{collection.Name}");
        }
        
        return collection;
    }

    public async Task InitializeDatabaseAsync()
    {
        if (_database != null) return;
        
        await Task.Run(() =>
        {
            try
            {
#if ANDROID
                Couchbase.Lite.Support.Droid.Activate(Android.App.Application.Context);
#endif
                _database = new Database("edge-platform-v2");
                Console.WriteLine($"Database created: {_database.Path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        });
    }

    public string CreateDocument(string type, Dictionary<string, object> data)
    {
        if (Database == null) throw new InvalidOperationException("Database not initialized");
        
        var collection = GetSyncedCollection();
        var doc = new MutableDocument();
        doc.SetString("type", type);
        doc.SetString("createdAt", DateTime.UtcNow.ToString("o"));

        foreach (var kvp in data)
        {
            doc.SetValue(kvp.Key, kvp.Value);
        }
        
        collection.Save(doc);
        Console.WriteLine($"Document saved to {collection.Scope.Name}.{collection.Name}: {doc.Id}");

        return doc.Id;
    }

    public List<Dictionary<string, object?>> GetDocuments(string type)
    {
        var results = new List<Dictionary<string, object?>>();
        var collection = GetSyncedCollection();

        using var query = QueryBuilder
            .Select(SelectResult.All())
            .From(DataSource.Collection(collection))
            .Where(Expression.Property("type").EqualTo(Expression.String(type)));

        foreach (var result in query.Execute())
        {
            var dict = result.ToDictionary();
            results.Add(dict);
        }

        Console.WriteLine($"Found {results.Count} documents of type '{type}'");
        return results;
    }

    public int GetTotalCount()
    {
        var collection = GetSyncedCollection();
        using var query = QueryBuilder
            .Select(SelectResult.Expression(Function.Count(Expression.All())))
            .From(DataSource.Collection(collection));

        var result = query.Execute().FirstOrDefault();
        return result?.GetInt(0) ?? 0;
    }

    public async Task<List<Views.DocumentInfo>> GetAllDocumentsRaw()
    {
        return await Task.Run(() =>
        {
            var results = new List<Views.DocumentInfo>();
            var collection = GetSyncedCollection();

            Console.WriteLine($"📊 Querying ALL documents from {collection.Scope.Name}.{collection.Name}...");

            using var query = QueryBuilder
                .Select(
                    SelectResult.Expression(Meta.ID),
                    SelectResult.Expression(Meta.Sequence),
                    SelectResult.All()
                )
                .From(DataSource.Collection(collection));

            var allResults = query.Execute().ToList();
            Console.WriteLine($"📄 Query returned {allResults.Count} documents");

            foreach (var result in allResults)
            {
                try
                {
                    var docId = result.GetString("id") ?? "unknown";
                    var sequence = result.GetLong("sequence");
                    var allData = result.GetDictionary(collection.Name);
                    
                    var docType = allData?.GetString("type") ?? "unknown";
                    var dataJson = allData != null ? System.Text.Json.JsonSerializer.Serialize(allData.ToDictionary(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true }) : "{}";

                    results.Add(new Views.DocumentInfo
                    {
                        Id = docId,
                        Type = $"Type: {docType}",
                        DataJson = dataJson,
                        Sequence = $"Sequence: #{sequence}"
                    });

                    Console.WriteLine($"  📄 Doc: {docId} (seq #{sequence}) - type: {docType}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ❌ Error reading document: {ex.Message}");
                }
            }

            Console.WriteLine($"✅ Loaded {results.Count} documents successfully");
            return results;
        });
    }

    public async Task ClearAllDataAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                Console.WriteLine("--->>> ⚠ NUCLEAR OPTION: Deleting entire database file");
                
                // Close and delete the physical database file
                if (_database != null)
                {
                    var dbPath = _database.Path;
                    Console.WriteLine($"--->>> Database path: {dbPath}");
                    
                    _database.Close();
                    _database.Dispose();
                    _database = null;
                    
                    // Delete the database file completely
                    Database.Delete("edge-platform-v2", null);
                    Console.WriteLine("--->>> ✓ Database file deleted from disk");
                }
                
                // Recreate fresh database
                _database = new Database("edge-platform-v2");
                Console.WriteLine($"--->>> ✓ Fresh database created at: {_database.Path}");
                
                // Recreate the collection
                var collection = _database.CreateCollection("cargo", "registry");
                Console.WriteLine($"--->>> ✓ Created fresh collection: {collection.Scope.Name}.{collection.Name}");
                
                Console.WriteLine("--->>> ✓ Database completely cleared and recreated");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--->>> ❌ Error clearing database: {ex.Message}");
                Console.WriteLine($"--->>>    Stack trace: {ex.StackTrace}");
            }
        });
    }
    // CRUD operations for Tasks
public string CreateTask(TaskItem task)
{
    var data = new Dictionary<string, object>
    {
        { "title", task.Title },
        { "description", task.Description },
        { "isCompleted", task.IsCompleted },
        { "createdAt", task.CreatedAt.ToString("o") }
    };
    
    return CreateDocument("task", data);
}

public List<TaskItem> GetAllTasks()
{
    var tasks = new List<TaskItem>();
    
    try
    {
        var collection = GetSyncedCollection();
        using var query = QueryBuilder
            .Select(
                SelectResult.Expression(Meta.ID),
                SelectResult.All())
            .From(DataSource.Collection(collection))
            .Where(Expression.Property("type").EqualTo(Expression.String("task")))
            .OrderBy(Ordering.Property("createdAt").Descending());
        
        foreach (var result in query.Execute())
        {
            var docId = result.GetString(0);
            var dict = result.GetDictionary(1);
            
            if (dict != null && docId != null)
            {
                tasks.Add(new TaskItem
                {
                    Id = docId,
                    Title = dict.GetString("title") ?? "",
                    Description = dict.GetString("description") ?? "",
                    IsCompleted = dict.GetBoolean("isCompleted"),
                    CreatedAt = dict.Contains("createdAt") 
                        ? DateTime.Parse(dict.GetString("createdAt") ?? DateTime.UtcNow.ToString("o")) 
                        : DateTime.UtcNow
                });
            }
        }
        
        Console.WriteLine($"Loaded {tasks.Count} tasks");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error getting tasks: {ex.Message}");
    }
    
    return tasks;
}

public void UpdateTask(TaskItem task)
{
    try
    {
        var collection = GetSyncedCollection();
        var doc = collection.GetDocument(task.Id);
        
        if (doc != null)
        {
            var mutableDoc = doc.ToMutable();
            mutableDoc.SetString("title", task.Title);
            mutableDoc.SetString("description", task.Description);
            mutableDoc.SetBoolean("isCompleted", task.IsCompleted);
            
            collection.Save(mutableDoc);
            Console.WriteLine($"Task updated: {task.Id}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error updating task: {ex.Message}");
    }
}

public void DeleteTask(string taskId)
{
    try
    {
        var collection = GetSyncedCollection();
        var doc = collection.GetDocument(taskId);
        
        if (doc != null)
        {
            collection.Delete(doc);
            Console.WriteLine($"Task deleted: {taskId}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error deleting task: {ex.Message}");
    }
}

// CRUD operations for Wagons
public string CreateWagon(Wagon wagon)
{
    var data = new Dictionary<string, object>
    {
        { "wagonNumber", wagon.WagonNumber },
        { "wagonType", wagon.WagonType },
        { "status", wagon.Status },
        { "currentLocation", wagon.CurrentLocation },
        { "destination", wagon.Destination },
        { "requiresLegalCheck", wagon.RequiresLegalCheck },
        { "lastInspection", wagon.LastInspection.ToString("o") },
        { "notes", wagon.Notes },
        { "createdAt", wagon.CreatedAt.ToString("o") }
    };
    
    return CreateDocument("wagon", data);
}

public List<Wagon> GetAllWagons()
{
    var wagons = new List<Wagon>();
    
    try
    {
        var collection = GetSyncedCollection();
        using var query = QueryBuilder
            .Select(
                SelectResult.Expression(Meta.ID),
                SelectResult.All())
            .From(DataSource.Collection(collection))
            .Where(Expression.Property("type").EqualTo(Expression.String("wagon")))
            .OrderBy(Ordering.Property("wagonNumber").Ascending());
        
        foreach (var result in query.Execute())
        {
            var docId = result.GetString(0);
            var dict = result.GetDictionary(1);
            
            if (dict != null && docId != null)
            {
                wagons.Add(new Wagon
                {
                    Id = docId,
                    WagonNumber = dict.GetString("wagonNumber") ?? "",
                    WagonType = dict.GetString("wagonType") ?? "",
                    Status = dict.GetString("status") ?? "Available",
                    CurrentLocation = dict.GetString("currentLocation") ?? "",
                    Destination = dict.GetString("destination") ?? "",
                    RequiresLegalCheck = dict.GetBoolean("requiresLegalCheck"),
                    LastInspection = dict.Contains("lastInspection") 
                        ? DateTime.Parse(dict.GetString("lastInspection") ?? DateTime.UtcNow.ToString("o")) 
                        : DateTime.UtcNow,
                    Notes = dict.GetString("notes") ?? "",
                    CreatedAt = dict.Contains("createdAt") 
                        ? DateTime.Parse(dict.GetString("createdAt") ?? DateTime.UtcNow.ToString("o")) 
                        : DateTime.UtcNow
                });
            }
        }
        
        Console.WriteLine($"Loaded {wagons.Count} wagons");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error getting wagons: {ex.Message}");
    }
    
    return wagons;
}

public void UpdateWagon(Wagon wagon)
{
    try
    {
        var collection = GetSyncedCollection();
        var doc = collection.GetDocument(wagon.Id);
        
        if (doc != null)
        {
            var mutableDoc = doc.ToMutable();
            mutableDoc.SetString("wagonNumber", wagon.WagonNumber);
            mutableDoc.SetString("wagonType", wagon.WagonType);
            mutableDoc.SetString("status", wagon.Status);
            mutableDoc.SetString("currentLocation", wagon.CurrentLocation);
            mutableDoc.SetString("destination", wagon.Destination);
            mutableDoc.SetBoolean("requiresLegalCheck", wagon.RequiresLegalCheck);
            mutableDoc.SetString("lastInspection", wagon.LastInspection.ToString("o"));
            mutableDoc.SetString("notes", wagon.Notes);
            
            collection.Save(mutableDoc);
            Console.WriteLine($"Wagon updated: {wagon.Id}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error updating wagon: {ex.Message}");
    }
}

public void DeleteWagon(string wagonId)
{
    try
    {
        var collection = GetSyncedCollection();
        var doc = collection.GetDocument(wagonId);
        
        if (doc != null)
        {
            collection.Delete(doc);
            Console.WriteLine($"Wagon deleted: {wagonId}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error deleting wagon: {ex.Message}");
    }
}
}