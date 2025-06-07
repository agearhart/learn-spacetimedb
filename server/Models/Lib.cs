using System.ComponentModel.DataAnnotations;
using SpacetimeDB;

public static partial class Module
{
    /// <summary>
    /// Users Table
    /// </summary>
    [Table(Name = "users", Public = true)]
    public partial class User
    {
        [PrimaryKey]
        public Identity Identity;
        public string? Name;
        public bool Online;
        public Timestamp LastSeen;
    }

    [Reducer]
    public static void SetUserName(ReducerContext ctx, string name)
    {
        var user = ctx.Db.users.Identity.Find(ctx.Sender);
        if (user is null)
        {
            Log.Warn($"Warning: User {ctx.Sender} tried to set their name to {name}, but no user record was found.");
            return;
        }

        Log.Info($"User {ctx.Sender} has changed their name to {name}.");
        user.Name = name;
        ctx.Db.users.Identity.Update(user);
    }

    [Reducer(ReducerKind.ClientConnected)]
    public static void ClientConnected(ReducerContext ctx)
    {
        var user = ctx.Db.users.Identity.Find(ctx.Sender);

        if (user is null)
        {
            Log.Info($"New user {ctx.Sender} has signed in.");
            ctx.Db.users.Insert(
                new User
                {
                    Name = null,
                    Identity = ctx.Sender,
                }
            );

            user = ctx.Db.users.Identity.Find(ctx.Sender);
        }

        Log.Info($"User {ctx.Sender} has signed in.");
        user.Online = true;
        user.LastSeen = new Timestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        ctx.Db.users.Identity.Update(user);
    }

    [Reducer(ReducerKind.ClientDisconnected)]
    public static void ClientDisconnected(ReducerContext ctx)
    {
        var user = ctx.Db.users.Identity.Find(ctx.Sender);

        if (user is null)
        {
            Log.Warn("Warning: No user found for disconnected client.");
            return;
        }

        Log.Info($"User {ctx.Sender} has signed out.");
        user.Online = false;
        user.LastSeen = new Timestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        ctx.Db.users.Identity.Update(user);
    }

    public partial class TankPart
    {
        [PrimaryKey]
        [AutoInc]
        public int Id;

        /// <summary>
        /// Human readable name for the Turret
        /// TODO: should be a localization table.
        /// </summary>
        [Required]
        public string? Name;

        /// <summary>
        /// How many armor points it costs to equip
        /// </summary>
        public int ArmorPointsCost;
    }

    /// <summary>
    /// Supported Turrets Table, used for attacking
    /// </summary>
    [Table(Name = "turrets", Public = true)]
    public partial class Turret : TankPart
    {
        /// <summary>
        /// Percent chance to hit target at clost range
        /// </summary>
        public int AttackPercentage;
    }

    /// <summary>
    /// Body of the tank where everything attaches
    /// </summary>
    [Table(Name = "chassis", Public = true)]
    public partial class Chassis : TankPart
    {
        /// <summary>
        /// How many armor points can this chassis support
        /// </summary>
        public int ArmorPointsAllowed;
    }

    /// <summary>
    /// How fast the tank can move
    /// </summary>
    [Table(Name = "engines", Public = true)]
    public partial class Engine : TankPart
    {
        public int Speed;
    }

    [Table(Name = "tanks", Public = true)]
    public partial class Tank
    {
        public Identity OwningPlayer;

        public int ChassisId;
        public int TurrentId;
        public int EngineId;

        /// <summary>
        /// Chassis APs - Sum of APCosts of parts
        /// Remaining APs are the health of the tank in the game
        /// </summary>
        public int RemainingArmorPoints;

        public Timestamp Created;
        public Timestamp LastUpdated;
    }


}
