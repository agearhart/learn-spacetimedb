using SpacetimeDB;
using SpacetimeDB.Types;
using System.Collections.Concurrent;

// our local client SpacetimeDB identity
Identity? local_identity = null;

// declare a thread safe queue to store commands
var input_queue = new ConcurrentQueue<(string Command, string Args)>();

void Main()
{
    // Initialize the `AuthToken` module
    AuthToken.Init(".spacetime_csharp_quickstart");
    // Builds and connects to the database
    DbConnection? conn = null;
    conn = ConnectToDB();
    // Registers to run in response to database events.
    RegisterCallbacks(conn);
    // Declare a threadsafe cancel token to cancel the process loop
    var cancellationTokenSource = new CancellationTokenSource();
    // Spawn a thread to call process updates and process commands
    var thread = new Thread(() => ProcessThread(conn, cancellationTokenSource.Token));
    thread.Start();
    // Handles CLI input
    InputLoop();
    // This signals the ProcessThread to stop
    cancellationTokenSource.Cancel();
    thread.Join();
}

/// The URI of the SpacetimeDB instance hosting our chat database and module.
// The URI of the SpacetimeDB instance hosting our chat database and module.
// Loaded from dotnet user-secrets with secret name SPACETIMEDB_HOST.
string HOST = Environment.GetEnvironmentVariable("SPACETIMEDB_HOST") ?? "";

// The database name we chose when we published our module.
string DB_NAME = Environment.GetEnvironmentVariable("SPACETIMEDB_DBNAME") ?? "";

/// Load credentials from a file and connect to the database.
DbConnection ConnectToDB()
{
    DbConnection? conn = null;
    conn = DbConnection.Builder()
        .WithUri(HOST)
        .WithModuleName(DB_NAME)
        .WithToken(AuthToken.Token)
        .OnConnect(OnConnected)
        .OnConnectError(OnConnectError)
        .OnDisconnect(OnDisconnected)
        .Build();
    return conn;
}

/// Our `OnConnected` callback: save our credentials to a file.
/// Our `OnConnect` callback: save our credentials to a file.
void OnConnected(DbConnection conn, Identity identity, string authToken)
{
    local_identity = identity;
    AuthToken.SaveToken(authToken);

    conn.SubscriptionBuilder()
        .OnApplied(OnSubscriptionApplied)
        .SubscribeToAllTables();
}

/// Our `OnConnectError` callback: print the error, then exit the process.
void OnConnectError(Exception e)
{
    Console.Write($"Error while connecting: {e}");
}

/// Our `OnDisconnect` callback: print a note, then exit the process.
void OnDisconnected(DbConnection conn, Exception? e)
{
    if (e != null)
    {
        Console.Write($"Disconnected abnormally: {e}");
    }
    else
    {
        Console.Write($"Disconnected normally.");
    }
}

/// Register all the callbacks our app will use to respond to database events.
void RegisterCallbacks(DbConnection conn)
{
    conn.Db.Users.OnInsert += User_OnInsert;
    conn.Db.Users.OnUpdate += User_OnUpdate;

    conn.Reducers.OnSetUserName += Reducer_OnSetNameEvent;
}

/// Our `OnSetNameEvent` callback: print a warning if the reducer failed.
void Reducer_OnSetNameEvent(ReducerEventContext ctx, string name)
{
    var e = ctx.Event;
    if (e.CallerIdentity == local_identity && e.Status is Status.Failed(var error))
    {
        Console.Write($"Failed to change name to {name}: {error}");
    }
}

/// If the user has no set name, use the first 8 characters from their identity.
string UserNameOrIdentity(User user) => user.Name ?? user.Identity.ToString()[..8];

/// Our `User.OnInsert` callback: if the user is online, print a notification.
void User_OnInsert(EventContext ctx, User insertedValue)
{
    if (insertedValue.Online)
    {
        Console.WriteLine($"{UserNameOrIdentity(insertedValue)} is online");
    }
}

/// Our `User.OnUpdate` callback:
/// print a notification about name and status changes.
void User_OnUpdate(EventContext ctx, User oldValue, User newValue)
{
    if (oldValue.Name != newValue.Name)
    {
        Console.WriteLine($"{UserNameOrIdentity(oldValue)} renamed to {newValue.Name}");
    }

    if (oldValue.Online != newValue.Online)
    {
        if (newValue.Online)
        {
            Console.WriteLine($"{UserNameOrIdentity(newValue)} connected.");
        }
        else
        {
            Console.WriteLine($"{UserNameOrIdentity(newValue)} disconnected.");
        }
    }
}

/// Our `OnSubscriptionApplied` callback:
void OnSubscriptionApplied(SubscriptionEventContext ctx)
{
    Console.WriteLine("Connected");
    foreach (var user in ctx.Db.Users.Iter())
    {
        Console.WriteLine($"{user} updated!");
    }
}


/// Our separate thread from main, where we can call process updates and process commands without blocking the main thread. 
void ProcessThread(DbConnection conn, CancellationToken ct)
{
    try
    {
        // loop until cancellation token
        while (!ct.IsCancellationRequested)
        {
            conn.FrameTick();

            ProcessCommands(conn.Reducers);

            Thread.Sleep(100);
        }
    }
    finally
    {
        conn.Disconnect();
    }
}

/// Read each line of standard input, and either set our name or send a message as appropriate.
void InputLoop()
{
    while (true)
    {
        var input = Console.ReadLine();
        if (input == null)
        {
            break;
        }

        if (input.StartsWith("/name "))
        {
            input_queue.Enqueue(("name", input[6..]));
            continue;
        }

    }
}

void ProcessCommands(RemoteReducers reducers)
{
    // process input queue commands
    while (input_queue.TryDequeue(out var command))
    {
        switch (command.Command)
        {
            case "name":
                reducers.SetUserName(command.Args);
                break;
            default:
                Console.WriteLine($"Unknown command: {command.Command}");
                break;
        }
    }
}

Main();