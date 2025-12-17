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
                _database = new Database("trainyardapp");
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
        
        var doc = new MutableDocument();
        doc.SetString("type", type);
        doc.SetString("createdAt", DateTime.UtcNow.ToString("o"));

        foreach (var kvp in data)
        {
            doc.SetValue(kvp.Key, kvp.Value);
        }

        var collection = Database.GetDefaultCollection();
        collection.Save(doc);
        Console.WriteLine($"Document saved: {doc.Id}");

        return doc.Id;
    }

    public List<Dictionary<string, object?>> GetDocuments(string type)
    {
        var results = new List<Dictionary<string, object?>>();

        using var query = QueryBuilder
            .Select(SelectResult.All())
            .From(DataSource.Collection(Database.GetDefaultCollection()))
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
        using var query = QueryBuilder
            .Select(SelectResult.Expression(Function.Count(Expression.All())))
            .From(DataSource.Collection(Database.GetDefaultCollection()));

        var result = query.Execute().FirstOrDefault();
        return result?.GetInt(0) ?? 0;
    }

    public async Task ClearAllDataAsync()
    {
        await Task.Run(() =>
        {
            Database.Delete();
            _database = new Database("trainyardapp");
            Console.WriteLine("Database cleared");
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
        using var query = QueryBuilder
            .Select(
                SelectResult.Expression(Meta.ID),
                SelectResult.All())
            .From(DataSource.Collection(Database.GetDefaultCollection()))
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
        var collection = Database.GetDefaultCollection();
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
        var collection = Database.GetDefaultCollection();
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
        using var query = QueryBuilder
            .Select(
                SelectResult.Expression(Meta.ID),
                SelectResult.All())
            .From(DataSource.Collection(Database.GetDefaultCollection()))
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
        var collection = Database.GetDefaultCollection();
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
        var collection = Database.GetDefaultCollection();
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